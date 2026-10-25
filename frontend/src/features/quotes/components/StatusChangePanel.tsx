import { useState } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';
import { CheckIcon, SendIcon, XIcon } from 'lucide-react';
import { Controller, useForm } from 'react-hook-form';
import { toast } from 'sonner';
import { ApiError } from '@/api/http';
import type { Quote, QuoteStatus } from '@/api/types';
import { ErrorAlert } from '@/components/ErrorAlert';
import { TraceId } from '@/components/TraceId';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Field, FieldDescription, FieldError, FieldLabel } from '@/components/ui/field';
import { Spinner } from '@/components/ui/spinner';
import { Textarea } from '@/components/ui/textarea';
import { useAdviser } from '@/features/adviser/AdviserContext';
import { messages } from '@/lib/messages';
import { useChangeQuoteStatus } from '../queries';
import { buildStatusChangeSchema, FIELD_LENGTHS, type StatusChangeFormValues } from '../quoteForm';
import { STATUS_ACTIONS, STATUS_LABELS, isReasonRequired } from '../status';

const DECLINE_REASON_REQUIRED = 'Quote.DeclineReasonRequired';

const ACTION_ICONS: Partial<Record<QuoteStatus, typeof SendIcon>> = {
  SentToClient: SendIcon,
  Accepted: CheckIcon,
  Declined: XIcon,
};

export function StatusChangePanel({ quote }: { quote: Quote }) {
  const [target, setTarget] = useState<QuoteStatus | null>(null);
  const [lastTraceId, setLastTraceId] = useState<string | null>(null);
  const isFinal = quote.allowedNextStatuses.length === 0;

  return (
    <Card>
      <CardHeader>
        <CardTitle>
          <h2>{messages.statusChange.title}</h2>
        </CardTitle>
        <CardDescription>
          {isFinal ? messages.statusChange.finalDescription(STATUS_LABELS[quote.status]) : messages.statusChange.openDescription}
        </CardDescription>
      </CardHeader>
      {(!isFinal || lastTraceId !== null) && (
        <CardContent className="flex flex-col gap-3">
          {!isFinal && (
            <div className="flex flex-wrap gap-2">
              {quote.allowedNextStatuses.map((status) => {
                const Icon = ACTION_ICONS[status];

                return (
                  <Button key={status} variant={status === 'Declined' ? 'destructive' : 'outline'} onClick={() => setTarget(status)}>
                    {Icon !== undefined && <Icon data-icon="inline-start" />}
                    {STATUS_ACTIONS[status]}
                  </Button>
                );
              })}
            </div>
          )}
          {lastTraceId !== null && <TraceId traceId={lastTraceId} />}
        </CardContent>
      )}

      <Dialog open={target !== null} onOpenChange={(open) => !open && setTarget(null)}>
        {target !== null && (
          // Keyed so each opening starts with a fresh form and mutation state.
          <StatusChangeDialogContent
            key={target}
            quote={quote}
            target={target}
            onDone={(traceId) => {
              setLastTraceId(traceId);
              setTarget(null);
            }}
          />
        )}
      </Dialog>
    </Card>
  );
}

interface StatusChangeDialogContentProps {
  quote: Quote;
  target: QuoteStatus;
  onDone: (traceId: string) => void;
}

function StatusChangeDialogContent({ quote, target, onDone }: StatusChangeDialogContentProps) {
  const { adviserId } = useAdviser();
  const changeStatus = useChangeQuoteStatus(quote.id);
  const reasonRequired = isReasonRequired(target);
  // After a conflict the quote is reloaded; the chosen status may no longer be on offer.
  const stillAllowed = quote.allowedNextStatuses.includes(target);
  const form = useForm<StatusChangeFormValues>({
    resolver: zodResolver(buildStatusChangeSchema(reasonRequired)),
    defaultValues: { reason: '' },
  });

  const onSubmit = form.handleSubmit(({ reason }) => {
    if (changeStatus.isPending) {
      return;
    }

    const trimmed = reason.trim();

    changeStatus.mutate(
      { newStatus: target, reason: trimmed === '' ? undefined : trimmed, changedBy: adviserId.trim() },
      {
        onSuccess: (result) => {
          toast.success(messages.statusChange.movedTo(STATUS_LABELS[result.quote.status]), {
            description: messages.statusChange.movedToDescription(quote.externalReference, result.traceId),
            duration: 8000,
          });
          onDone(result.traceId);
        },
        onError: (error) => {
          if (error instanceof ApiError) {
            const message = error.code === DECLINE_REASON_REQUIRED ? error.message : error.fieldErrors.reason?.[0];

            if (message !== undefined) {
              form.setError('reason', { type: 'server', message }, { shouldFocus: true });
            }
          }
        },
      },
    );
  });

  const showRequestError = changeStatus.error !== null && form.formState.errors.reason?.type !== 'server';

  return (
    <DialogContent>
      <form onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
        <DialogHeader>
          <DialogTitle>{messages.statusChange.dialogTitle(STATUS_ACTIONS[target], quote.externalReference)}</DialogTitle>
          <DialogDescription>
            {messages.statusChange.dialogDescription(quote.client.fullName, STATUS_LABELS[quote.status], STATUS_LABELS[target], adviserId)}
          </DialogDescription>
        </DialogHeader>

        <Controller
          name="reason"
          control={form.control}
          render={({ field, fieldState }) => (
            <Field data-invalid={fieldState.invalid}>
              <FieldLabel htmlFor="status-reason">
                {messages.statusChange.reason}{' '}
                {!reasonRequired && <span className="font-normal text-muted-foreground">{messages.common.optional}</span>}
              </FieldLabel>
              <Textarea
                {...field}
                id="status-reason"
                rows={3}
                maxLength={FIELD_LENGTHS.statusReason}
                aria-invalid={fieldState.invalid}
                aria-describedby={fieldState.invalid ? 'status-reason-description status-reason-error' : 'status-reason-description'}
              />
              <FieldDescription id="status-reason-description">
                {reasonRequired && messages.statusChange.reasonRequiredHint}
                {messages.statusChange.reasonKeptHint}
              </FieldDescription>
              {fieldState.invalid && <FieldError id="status-reason-error" errors={[fieldState.error]} />}
            </Field>
          )}
        />

        {showRequestError && <ErrorAlert error={changeStatus.error} title={messages.statusChange.notChanged} />}

        <DialogFooter>
          <DialogClose asChild>
            <Button type="button" variant="outline" disabled={changeStatus.isPending}>
              {messages.common.cancel}
            </Button>
          </DialogClose>
          <Button
            type="submit"
            variant={target === 'Declined' ? 'destructive' : 'default'}
            disabled={changeStatus.isPending || !stillAllowed}
          >
            {changeStatus.isPending && <Spinner data-icon="inline-start" />}
            {STATUS_ACTIONS[target]}
          </Button>
        </DialogFooter>
      </form>
    </DialogContent>
  );
}

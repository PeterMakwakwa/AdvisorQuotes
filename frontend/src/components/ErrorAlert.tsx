import { CircleAlertIcon, RotateCwIcon } from 'lucide-react';
import { ApiError } from '@/api/http';
import { Alert, AlertAction, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { messages } from '@/lib/messages';
import { TraceId } from './TraceId';

interface ErrorAlertProps {
  error: unknown;
  title?: string;
  onRetry?: () => void;
}

export function ErrorAlert({ error, title = messages.common.somethingWentWrong, onRetry }: ErrorAlertProps) {
  const message = error instanceof Error ? error.message : messages.common.unexpectedError;

  return (
    <Alert variant="destructive" role="alert">
      <CircleAlertIcon />
      <AlertTitle>{title}</AlertTitle>
      <AlertDescription className="flex flex-col gap-1.5">
        <p>{message}</p>
        {error instanceof ApiError && <TraceId traceId={error.traceId} />}
      </AlertDescription>
      {onRetry !== undefined && (
        <AlertAction>
          <Button type="button" variant="outline" size="sm" onClick={onRetry}>
            <RotateCwIcon data-icon="inline-start" />
            {messages.common.retry}
          </Button>
        </AlertAction>
      )}
    </Alert>
  );
}

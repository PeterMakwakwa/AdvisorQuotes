import { useState } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';
import { Controller, useForm, useWatch, type ControllerFieldState } from 'react-hook-form';
import { ApiError } from '@/api/http';
import type { CreateQuoteResult } from '@/api/quotesApi';
import type { QuoteSummary } from '@/api/types';
import { ErrorAlert } from '@/components/ErrorAlert';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import {
  Field,
  FieldContent,
  FieldDescription,
  FieldError,
  FieldGroup,
  FieldLabel,
  FieldLegend,
  FieldSeparator,
  FieldSet,
} from '@/components/ui/field';
import { Input } from '@/components/ui/input';
import { InputGroup, InputGroupAddon, InputGroupInput, InputGroupText } from '@/components/ui/input-group';
import { NativeSelect, NativeSelectOption } from '@/components/ui/native-select';
import { Spinner } from '@/components/ui/spinner';
import { Textarea } from '@/components/ui/textarea';
import { useAdviser } from '@/features/adviser/AdviserContext';
import { todayIsoDate } from '@/lib/format';
import { clientProductKey, findOpenQuotesForClient } from '../openQuotes';
import { PRODUCTS } from '../products';
import { useCreateQuote } from '../queries';
import {
  EMPTY_QUOTE_FORM,
  FIELD_LENGTHS,
  mapServerErrors,
  quoteFormSchema,
  toCreateQuoteRequest,
  type QuoteFormField,
  type QuoteFormInput,
  type QuoteFormOutput,
} from '../quoteForm';
import { useIdempotencyKey } from '../useIdempotencyKey';
import { OpenQuotesWarning } from './OpenQuotesWarning';

/** Links an input to its description and error so screen readers announce them. */
function describedBy(name: QuoteFormField, fieldState: ControllerFieldState, hasDescription = false): string | undefined {
  const ids = [hasDescription ? `${name}-description` : null, fieldState.invalid ? `${name}-error` : null].filter(Boolean);

  return ids.length === 0 ? undefined : ids.join(' ');
}

function FieldMessage({ name, fieldState }: { name: QuoteFormField; fieldState: ControllerFieldState }) {
  return fieldState.invalid ? <FieldError id={`${name}-error`} errors={[fieldState.error]} /> : null;
}

interface CreateQuoteFormProps {
  onCreated: (result: CreateQuoteResult) => void;
}

export function CreateQuoteForm({ onCreated }: CreateQuoteFormProps) {
  const { adviserId } = useAdviser();
  const [alertError, setAlertError] = useState<unknown>(null);
  const idempotencyKeyFor = useIdempotencyKey();
  const createQuote = useCreateQuote();
  const form = useForm<QuoteFormInput, unknown, QuoteFormOutput>({
    resolver: zodResolver(quoteFormSchema),
    defaultValues: EMPTY_QUOTE_FORM,
    mode: 'onTouched',
  });
  // The open-quote check runs once per client and product; a second submit for the same ones captures anyway.
  const [openQuotes, setOpenQuotes] = useState<{ key: string; quotes: QuoteSummary[] } | null>(null);
  const [isCheckingClient, setIsCheckingClient] = useState(false);
  const [clientFullName, dateOfBirth, productCode] = useWatch({
    control: form.control,
    name: ['clientFullName', 'dateOfBirth', 'productCode'],
  });
  const currentKey = clientProductKey(clientFullName, dateOfBirth, productCode);
  const warningQuotes = openQuotes?.key === currentKey ? openQuotes.quotes : [];

  const onSubmit = form.handleSubmit(async (values) => {
    if (createQuote.isPending || isCheckingClient) {
      return;
    }

    setAlertError(null);

    if (adviserId.trim() === '') {
      setAlertError(new Error('Enter your adviser ID at the top of the page before capturing a quote.'));
      return;
    }

    const request = toCreateQuoteRequest(values, adviserId);
    const key = clientProductKey(request.client.fullName, request.client.dateOfBirth, request.product.productCode);

    if (openQuotes?.key !== key) {
      setIsCheckingClient(true);
      const quotes = await findOpenQuotesForClient(request);
      setIsCheckingClient(false);
      setOpenQuotes({ key, quotes });

      if (quotes.length > 0) {
        return;
      }
    }

    createQuote.mutate(
      { request, idempotencyKey: idempotencyKeyFor(request) },
      {
        onSuccess: onCreated,
        onError: (error) => {
          if (!(error instanceof ApiError)) {
            setAlertError(error);
            return;
          }

          const mapped = mapServerErrors(error);
          const fields = Object.entries(mapped.fieldErrors) as [QuoteFormField, string][];
          fields.forEach(([field, message], index) => form.setError(field, { type: 'server', message }, { shouldFocus: index === 0 }));
          setAlertError(mapped.fullyMapped ? null : error);
        },
      },
    );
  });

  return (
    <form onSubmit={onSubmit} noValidate className="flex max-w-3xl flex-col gap-6">
      {alertError !== null && <ErrorAlert error={alertError} title="The quote was not created" />}

      <Card>
        <CardHeader>
          <CardTitle>
            <h2>Quote details</h2>
          </CardTitle>
          <CardDescription>The premium is calculated by the server when the quote is captured.</CardDescription>
        </CardHeader>
        <CardContent>
          <FieldGroup>
            <FieldSet>
              <FieldLegend>Client</FieldLegend>
              <div className="grid gap-4 sm:grid-cols-2">
                <Controller
                  name="clientFullName"
                  control={form.control}
                  render={({ field, fieldState }) => (
                    <Field data-invalid={fieldState.invalid}>
                      <FieldLabel htmlFor="clientFullName">Full name</FieldLabel>
                      <Input
                        {...field}
                        id="clientFullName"
                        autoComplete="off"
                        maxLength={FIELD_LENGTHS.clientFullName}
                        aria-invalid={fieldState.invalid}
                        aria-describedby={describedBy(field.name, fieldState)}
                      />
                      <FieldMessage name={field.name} fieldState={fieldState} />
                    </Field>
                  )}
                />
                <Controller
                  name="dateOfBirth"
                  control={form.control}
                  render={({ field, fieldState }) => (
                    <Field data-invalid={fieldState.invalid}>
                      <FieldLabel htmlFor="dateOfBirth">Date of birth</FieldLabel>
                      {/* A native date input: typing a birth year beats paging back decades in a calendar popover. */}
                      <Input
                        {...field}
                        id="dateOfBirth"
                        type="date"
                        max={todayIsoDate()}
                        aria-invalid={fieldState.invalid}
                        aria-describedby={describedBy(field.name, fieldState)}
                      />
                      <FieldMessage name={field.name} fieldState={fieldState} />
                    </Field>
                  )}
                />
              </div>
              <Controller
                name="isSmoker"
                control={form.control}
                render={({ field }) => (
                  <Field orientation="horizontal">
                    <Checkbox
                      id="isSmoker"
                      name={field.name}
                      ref={field.ref}
                      checked={field.value}
                      onCheckedChange={(checked) => field.onChange(checked === true)}
                      onBlur={field.onBlur}
                    />
                    <FieldContent>
                      <FieldLabel htmlFor="isSmoker">The client smokes</FieldLabel>
                      <FieldDescription>Smokers pay a loading on the premium.</FieldDescription>
                    </FieldContent>
                  </Field>
                )}
              />
            </FieldSet>

            <FieldSeparator />

            <FieldSet>
              <FieldLegend>Cover</FieldLegend>
              <div className="grid gap-4 sm:grid-cols-3">
                <Controller
                  name="productCode"
                  control={form.control}
                  render={({ field, fieldState }) => (
                    <Field data-invalid={fieldState.invalid}>
                      <FieldLabel htmlFor="productCode">Product</FieldLabel>
                      <NativeSelect
                        {...field}
                        id="productCode"
                        className="w-full"
                        aria-invalid={fieldState.invalid}
                        aria-describedby={describedBy(field.name, fieldState)}
                      >
                        {PRODUCTS.map((product) => (
                          <NativeSelectOption key={product.code} value={product.code}>
                            {product.label}
                          </NativeSelectOption>
                        ))}
                      </NativeSelect>
                      <FieldMessage name={field.name} fieldState={fieldState} />
                    </Field>
                  )}
                />
                <Controller
                  name="coverAmount"
                  control={form.control}
                  render={({ field, fieldState }) => (
                    <Field data-invalid={fieldState.invalid}>
                      <FieldLabel htmlFor="coverAmount">Cover amount</FieldLabel>
                      <InputGroup>
                        <InputGroupAddon>
                          <InputGroupText>R</InputGroupText>
                        </InputGroupAddon>
                        <InputGroupInput
                          {...field}
                          id="coverAmount"
                          inputMode="decimal"
                          aria-invalid={fieldState.invalid}
                          aria-describedby={describedBy(field.name, fieldState)}
                        />
                      </InputGroup>
                      <FieldMessage name={field.name} fieldState={fieldState} />
                    </Field>
                  )}
                />
                <Controller
                  name="termYears"
                  control={form.control}
                  render={({ field, fieldState }) => (
                    <Field data-invalid={fieldState.invalid}>
                      <FieldLabel htmlFor="termYears">Term</FieldLabel>
                      <InputGroup>
                        <InputGroupInput
                          {...field}
                          id="termYears"
                          inputMode="numeric"
                          aria-invalid={fieldState.invalid}
                          aria-describedby={describedBy(field.name, fieldState)}
                        />
                        <InputGroupAddon align="inline-end">
                          <InputGroupText>years</InputGroupText>
                        </InputGroupAddon>
                      </InputGroup>
                      <FieldMessage name={field.name} fieldState={fieldState} />
                    </Field>
                  )}
                />
              </div>
            </FieldSet>

            <FieldSeparator />

            <FieldSet>
              <FieldLegend>Reference and notes</FieldLegend>
              <Controller
                name="adviserReference"
                control={form.control}
                render={({ field, fieldState }) => (
                  <Field data-invalid={fieldState.invalid}>
                    <FieldLabel htmlFor="adviserReference">
                      Adviser reference <span className="font-normal text-muted-foreground">(optional)</span>
                    </FieldLabel>
                    <Input
                      {...field}
                      id="adviserReference"
                      maxLength={FIELD_LENGTHS.adviserReference}
                      aria-invalid={fieldState.invalid}
                      aria-describedby={describedBy(field.name, fieldState, true)}
                    />
                    <FieldDescription id="adviserReference-description">
                      Your own reference, for example a CRM case number.
                    </FieldDescription>
                    <FieldMessage name={field.name} fieldState={fieldState} />
                  </Field>
                )}
              />
              <Controller
                name="notes"
                control={form.control}
                render={({ field, fieldState }) => (
                  <Field data-invalid={fieldState.invalid}>
                    <FieldLabel htmlFor="notes">
                      Notes <span className="font-normal text-muted-foreground">(optional)</span>
                    </FieldLabel>
                    <Textarea
                      {...field}
                      id="notes"
                      rows={4}
                      maxLength={FIELD_LENGTHS.notes}
                      aria-invalid={fieldState.invalid}
                      aria-describedby={describedBy(field.name, fieldState)}
                    />
                    <FieldMessage name={field.name} fieldState={fieldState} />
                  </Field>
                )}
              />
            </FieldSet>
          </FieldGroup>
          {warningQuotes.length > 0 && (
            <div className="mt-6">
              <OpenQuotesWarning quotes={warningQuotes} />
            </div>
          )}
        </CardContent>
        <CardFooter className="flex flex-wrap items-center justify-between gap-3 border-t">
          <p className="text-sm text-muted-foreground">
            Capturing as <span className="font-medium text-foreground">{adviserId.trim() === '' ? 'no adviser set' : adviserId}</span>
          </p>
          <Button type="submit" disabled={createQuote.isPending || isCheckingClient}>
            {(createQuote.isPending || isCheckingClient) && <Spinner data-icon="inline-start" />}
            {createQuote.isPending ? 'Capturing quote…' : warningQuotes.length > 0 ? 'Capture anyway' : 'Capture quote'}
          </Button>
        </CardFooter>
      </Card>
    </form>
  );
}

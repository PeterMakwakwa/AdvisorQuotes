import { z } from 'zod';
import type { ApiError } from '@/api/http';
import type { CreateQuoteRequest } from '@/api/types';
import { todayIsoDate } from '@/lib/format';
import { messages } from '@/lib/messages';
import { PRODUCTS } from './products';

// Zod probes `new Function` to speed up parsing. Our CSP forbids eval, so the probe would log a security-policy
// violation on every page load; jitless mode skips it (the forms are far too small for the speed-up to matter).
z.config({ jitless: true });

// Matches Quotes.Application/Constants/FieldLengths.cs.
export const FIELD_LENGTHS = {
  adviserReference: 50,
  clientFullName: 200,
  notes: 2000,
  statusReason: 500,
} as const;

const MONEY_PATTERN = /^\d+(\.\d{1,2})?$/;

const WHOLE_NUMBER_PATTERN = /^\d+$/;

/**
 * Quick checks so the adviser gets feedback before a round trip. The API stays the authority: it re-validates
 * everything and applies the product limits, and those errors are mapped back onto the same fields.
 * Numbers stay as text in the inputs so a half-typed value is not lost; they are converted on submit.
 */
export function buildQuoteFormSchema(today: () => string = todayIsoDate) {
  return z.object({
    clientFullName: z
      .string()
      .trim()
      .min(1, messages.quoteValidation.fullNameRequired)
      .max(FIELD_LENGTHS.clientFullName, messages.quoteValidation.tooLong(FIELD_LENGTHS.clientFullName)),
    dateOfBirth: z.iso
      .date(messages.quoteValidation.dateOfBirthRequired)
      .refine((dateOfBirth) => dateOfBirth < today(), messages.quoteValidation.dateOfBirthNotInPast),
    isSmoker: z.boolean(),
    productCode: z.string().min(1, messages.quoteValidation.productRequired),
    coverAmount: z
      .string()
      .trim()
      .min(1, messages.quoteValidation.coverAmountRequired)
      .regex(MONEY_PATTERN, messages.quoteValidation.coverAmountInvalid)
      .refine((value) => Number(value) > 0, messages.quoteValidation.coverAmountInvalid),
    termYears: z
      .string()
      .trim()
      .min(1, messages.quoteValidation.termRequired)
      .regex(WHOLE_NUMBER_PATTERN, messages.quoteValidation.termInvalid)
      .refine((value) => Number(value) > 0, messages.quoteValidation.termInvalid),
    adviserReference: z
      .string()
      .trim()
      .max(FIELD_LENGTHS.adviserReference, messages.quoteValidation.tooLong(FIELD_LENGTHS.adviserReference)),
    notes: z.string().trim().max(FIELD_LENGTHS.notes, messages.quoteValidation.tooLong(FIELD_LENGTHS.notes)),
  });
}

export const quoteFormSchema = buildQuoteFormSchema();

/** What the inputs hold. */
export type QuoteFormInput = z.input<typeof quoteFormSchema>;

/** What a valid form produces (text trimmed). */
export type QuoteFormOutput = z.output<typeof quoteFormSchema>;

export type QuoteFormField = keyof QuoteFormInput;

export const EMPTY_QUOTE_FORM: QuoteFormInput = {
  clientFullName: '',
  dateOfBirth: '',
  isSmoker: false,
  productCode: PRODUCTS[0]?.code ?? '',
  coverAmount: '',
  termYears: '',
  adviserReference: '',
  notes: '',
};

function optional(value: string): string | undefined {
  return value === '' ? undefined : value;
}

export function toCreateQuoteRequest(values: QuoteFormOutput, adviserId: string): CreateQuoteRequest {
  return {
    adviserId: adviserId.trim(),
    adviserReference: optional(values.adviserReference),
    client: {
      fullName: values.clientFullName,
      dateOfBirth: values.dateOfBirth,
      isSmoker: values.isSmoker,
    },
    product: {
      productCode: values.productCode,
      coverAmount: Number(values.coverAmount),
      termYears: Number(values.termYears),
    },
    notes: optional(values.notes),
  };
}

/** A reason is optional, except when declining (mirrors the domain rule). */
export function buildStatusChangeSchema(reasonRequired: boolean) {
  return z.object({
    reason: z
      .string()
      .trim()
      .max(FIELD_LENGTHS.statusReason, messages.quoteValidation.tooLong(FIELD_LENGTHS.statusReason))
      .refine((reason) => !reasonRequired || reason !== '', messages.quoteValidation.declineReasonRequired),
  });
}

export type StatusChangeFormValues = z.input<ReturnType<typeof buildStatusChangeSchema>>;

// Validation errors arrive keyed by the request's camelCase JSON path.
const FIELD_BY_REQUEST_PATH: Record<string, QuoteFormField> = {
  adviserReference: 'adviserReference',
  'client.fullName': 'clientFullName',
  'client.dateOfBirth': 'dateOfBirth',
  'product.productCode': 'productCode',
  'product.coverAmount': 'coverAmount',
  'product.termYears': 'termYears',
  notes: 'notes',
};

// Pricing rules come back as a single error code; each one belongs to the field the adviser should change.
const FIELD_BY_ERROR_CODE: Record<string, QuoteFormField> = {
  'Pricing.UnknownProduct': 'productCode',
  'Pricing.CoverOutOfRange': 'coverAmount',
  'Pricing.TermOutOfRange': 'termYears',
  'Pricing.TermNotRated': 'termYears',
  'Pricing.AgeNotInsurable': 'dateOfBirth',
  'Pricing.AgeAtExpiryExceeded': 'termYears',
};

export type QuoteFormErrors = Partial<Record<QuoteFormField, string>>;

export interface MappedServerErrors {
  fieldErrors: QuoteFormErrors;
  /** True when every problem in the response was placed on a field, so no general error alert is needed. */
  fullyMapped: boolean;
}

export function mapServerErrors(error: ApiError): MappedServerErrors {
  const fieldErrors: QuoteFormErrors = {};
  let unmapped = 0;

  for (const [path, messages] of Object.entries(error.fieldErrors)) {
    const field = FIELD_BY_REQUEST_PATH[path];

    if (field !== undefined && messages[0] !== undefined) {
      fieldErrors[field] = messages[0];
    } else {
      unmapped += 1;
    }
  }

  const codeField = error.code === undefined ? undefined : FIELD_BY_ERROR_CODE[error.code];

  if (codeField !== undefined) {
    fieldErrors[codeField] = error.message;
  }

  const hasFieldErrors = Object.keys(fieldErrors).length > 0;

  return { fieldErrors, fullyMapped: hasFieldErrors && unmapped === 0 };
}

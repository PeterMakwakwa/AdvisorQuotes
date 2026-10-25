import { describe, expect, it } from 'vitest';
import { ApiError } from '@/api/http';
import {
  buildQuoteFormSchema,
  buildStatusChangeSchema,
  EMPTY_QUOTE_FORM,
  mapServerErrors,
  toCreateQuoteRequest,
  type QuoteFormInput,
} from './quoteForm';

const schema = buildQuoteFormSchema(() => '2026-10-24');

const validForm: QuoteFormInput = {
  ...EMPTY_QUOTE_FORM,
  clientFullName: '  Thandi Nkosi ',
  dateOfBirth: '1990-05-01',
  productCode: 'TERM_LIFE',
  coverAmount: '1000000',
  termYears: '20',
};

function errorFields(values: QuoteFormInput): string[] {
  const result = schema.safeParse(values);

  return result.success ? [] : [...new Set(result.error.issues.map((issue) => String(issue.path[0])))].sort();
}

describe('quote form schema', () => {
  it('accepts a complete form', () => {
    expect(errorFields(validForm)).toEqual([]);
  });

  it('requires the client, product and cover fields', () => {
    expect(errorFields({ ...EMPTY_QUOTE_FORM, productCode: '' })).toEqual([
      'clientFullName',
      'coverAmount',
      'dateOfBirth',
      'productCode',
      'termYears',
    ]);
  });

  it('rejects a date of birth that is today or later', () => {
    expect(errorFields({ ...validForm, dateOfBirth: '2026-10-24' })).toEqual(['dateOfBirth']);
  });

  it.each(['0', '-5', 'abc', '100.123'])('rejects cover amount %s', (coverAmount) => {
    expect(errorFields({ ...validForm, coverAmount })).toEqual(['coverAmount']);
  });

  it.each(['0', '2.5', 'ten'])('rejects term %s', (termYears) => {
    expect(errorFields({ ...validForm, termYears })).toEqual(['termYears']);
  });
});

describe('toCreateQuoteRequest', () => {
  it('uses the trimmed values, converts numbers and drops blank optional fields', () => {
    const values = schema.parse({ ...validForm, adviserReference: '   ', notes: '' });

    expect(toCreateQuoteRequest(values, ' ADV-1 ')).toEqual({
      adviserId: 'ADV-1',
      adviserReference: undefined,
      client: { fullName: 'Thandi Nkosi', dateOfBirth: '1990-05-01', isSmoker: false },
      product: { productCode: 'TERM_LIFE', coverAmount: 1_000_000, termYears: 20 },
      notes: undefined,
    });
  });
});

describe('status change schema', () => {
  it('requires a reason only when declining', () => {
    expect(buildStatusChangeSchema(true).safeParse({ reason: '  ' }).success).toBe(false);
    expect(buildStatusChangeSchema(false).safeParse({ reason: '' }).success).toBe(true);
  });
});

describe('mapServerErrors', () => {
  it('maps validation errors from request paths onto form fields', () => {
    const error = new ApiError(400, 'One or more validation errors occurred.', 'trace', {
      code: 'Validation.Failed',
      errors: { 'client.fullName': ["'Full Name' must not be empty."], 'product.termYears': ['Too short.'] },
    });

    expect(mapServerErrors(error)).toEqual({
      fieldErrors: { clientFullName: "'Full Name' must not be empty.", termYears: 'Too short.' },
      fullyMapped: true,
    });
  });

  it('maps a pricing rule code to the field the adviser should change', () => {
    const error = new ApiError(400, 'Cover for TERM_LIFE must be between 100,000 and 10,000,000.', 'trace', {
      code: 'Pricing.CoverOutOfRange',
    });

    expect(mapServerErrors(error).fieldErrors).toEqual({ coverAmount: 'Cover for TERM_LIFE must be between 100,000 and 10,000,000.' });
  });

  it('reports errors it cannot place so the form shows an alert', () => {
    const error = new ApiError(400, 'Bad', 'trace', { errors: { adviserId: ['Too long.'] } });

    expect(mapServerErrors(error)).toEqual({ fieldErrors: {}, fullyMapped: false });
  });
});

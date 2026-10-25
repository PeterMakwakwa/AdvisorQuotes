import { request } from './http';
import type {
  ChangeQuoteStatusRequest,
  CreateQuoteRequest,
  PagedResponse,
  Quote,
  QuoteStatus,
  QuoteSummary,
} from './types';

const IDEMPOTENCY_KEY_HEADER = 'Idempotency-Key';

const IDEMPOTENT_REPLAYED_HEADER = 'Idempotent-Replayed';

export interface ListQuotesParams {
  adviserId?: string;
  adviserReference?: string;
  status?: QuoteStatus;
  clientName?: string;
  /** yyyy-MM-dd */
  clientDateOfBirth?: string;
  productCode?: string;
  /** Only quotes that can still change status (Draft or SentToClient). */
  openOnly?: boolean;
  cursor?: string;
  pageSize?: number;
}

export interface QuoteMutationResult {
  quote: Quote;
  traceId: string;
}

export interface CreateQuoteResult extends QuoteMutationResult {
  /** True when the API recognised a repeated submission and returned the original quote. */
  replayed: boolean;
}

export async function listQuotes(params: ListQuotesParams, signal?: AbortSignal): Promise<PagedResponse<QuoteSummary>> {
  const query = new URLSearchParams();

  for (const [name, value] of Object.entries(params)) {
    if (value !== undefined && value !== '') {
      query.set(name, String(value));
    }
  }

  const response = await request<PagedResponse<QuoteSummary>>(`/quotes?${query.toString()}`, { signal });

  return response.data;
}

export async function getQuote(quoteId: string, signal?: AbortSignal): Promise<Quote> {
  const response = await request<Quote>(`/quotes/${encodeURIComponent(quoteId)}`, { signal });

  return response.data;
}

export async function createQuote(body: CreateQuoteRequest, idempotencyKey: string): Promise<CreateQuoteResult> {
  const response = await request<Quote>('/quotes', {
    method: 'POST',
    headers: { [IDEMPOTENCY_KEY_HEADER]: idempotencyKey },
    body: JSON.stringify(body),
  });

  return {
    quote: response.data,
    traceId: response.traceId,
    replayed: response.headers.get(IDEMPOTENT_REPLAYED_HEADER) === 'true',
  };
}

export async function changeQuoteStatus(quoteId: string, body: ChangeQuoteStatusRequest): Promise<QuoteMutationResult> {
  const response = await request<Quote>(`/quotes/${encodeURIComponent(quoteId)}/status-changes`, {
    method: 'POST',
    body: JSON.stringify(body),
  });

  return { quote: response.data, traceId: response.traceId };
}

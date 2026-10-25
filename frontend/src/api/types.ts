// Mirrors the Quotes API v1 HTTP contract: backend/docs/api.md, or the OpenAPI document at /openapi/v1.json.

export const QUOTE_STATUSES = ['Draft', 'SentToClient', 'Accepted', 'Declined'] as const;

export type QuoteStatus = (typeof QUOTE_STATUSES)[number];

export interface ClientRequest {
  fullName: string;
  /** yyyy-MM-dd */
  dateOfBirth: string;
  isSmoker: boolean;
}

export interface ProductRequest {
  productCode: string;
  coverAmount: number;
  termYears: number;
}

export interface CreateQuoteRequest {
  adviserId: string;
  adviserReference?: string;
  client: ClientRequest;
  product: ProductRequest;
  notes?: string;
}

export interface ChangeQuoteStatusRequest {
  newStatus: QuoteStatus;
  reason?: string;
  changedBy: string;
}

export interface Premium {
  monthlyAmount: number;
  currency: string;
  pricingVersion: string;
}

export interface StatusChange {
  fromStatus: QuoteStatus | null;
  toStatus: QuoteStatus;
  reason: string | null;
  changedBy: string;
  changedAt: string;
}

export interface Quote {
  id: string;
  externalReference: string;
  adviserId: string;
  adviserReference: string | null;
  client: ClientRequest;
  product: ProductRequest;
  premium: Premium;
  status: QuoteStatus;
  allowedNextStatuses: QuoteStatus[];
  notes: string | null;
  createdAt: string;
  updatedAt: string;
  statusHistory: StatusChange[];
}

export interface QuoteSummary {
  id: string;
  externalReference: string;
  adviserId: string;
  adviserReference: string | null;
  clientName: string;
  productCode: string;
  coverAmount: number;
  monthlyPremium: number;
  currency: string;
  status: QuoteStatus;
  createdAt: string;
  updatedAt: string;
}

export interface PagedResponse<TItem> {
  items: TItem[];
  pageSize: number;
  /** Only on the first page (no cursor); null on later pages. */
  totalCount: number | null;
  nextCursor: string | null;
}

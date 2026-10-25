import { listQuotes } from '@/api/quotesApi';
import type { CreateQuoteRequest, QuoteSummary } from '@/api/types';

const MAX_SHOWN = 5;

/** "The same client and product", matched the way the API matches it: the name ignoring case and spaces. */
export function clientProductKey(fullName: string, dateOfBirth: string, productCode: string): string {
  return [fullName.trim().toLowerCase(), dateOfBirth, productCode.toUpperCase()].join('|');
}

/**
 * The adviser's open quotes (Draft or Sent to client) for the same client and product. A failed check returns none:
 * the warning helps the adviser, but it never stops a quote being captured.
 */
export async function findOpenQuotesForClient(request: CreateQuoteRequest): Promise<QuoteSummary[]> {
  try {
    const page = await listQuotes({
      adviserId: request.adviserId,
      clientName: request.client.fullName,
      clientDateOfBirth: request.client.dateOfBirth,
      productCode: request.product.productCode,
      openOnly: true,
      pageSize: MAX_SHOWN,
    });

    return page.items;
  } catch {
    return [];
  }
}

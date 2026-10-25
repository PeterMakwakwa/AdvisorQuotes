import type { QuoteStatus } from '@/api/types';
import { messages } from '@/lib/messages';

export const STATUS_LABELS: Record<QuoteStatus, string> = messages.quoteStatus.labels;

/** The button text for moving a quote to each status. */
export const STATUS_ACTIONS: Record<QuoteStatus, string> = messages.quoteStatus.actions;

/** Mirrors the domain rule: a reason is required when a quote is declined. */
export function isReasonRequired(status: QuoteStatus): boolean {
  return status === 'Declined';
}

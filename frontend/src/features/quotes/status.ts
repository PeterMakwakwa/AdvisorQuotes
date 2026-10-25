import type { QuoteStatus } from '@/api/types';

export const STATUS_LABELS: Record<QuoteStatus, string> = {
  Draft: 'Draft',
  SentToClient: 'Sent to client',
  Accepted: 'Accepted',
  Declined: 'Declined',
};

/** The button text for moving a quote to each status. */
export const STATUS_ACTIONS: Record<QuoteStatus, string> = {
  Draft: 'Move to draft',
  SentToClient: 'Send to client',
  Accepted: 'Mark accepted',
  Declined: 'Decline',
};

/** Mirrors the domain rule: a reason is required when a quote is declined. */
export function isReasonRequired(status: QuoteStatus): boolean {
  return status === 'Declined';
}

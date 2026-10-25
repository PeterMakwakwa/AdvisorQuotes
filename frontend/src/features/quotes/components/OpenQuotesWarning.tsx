import { TriangleAlertIcon } from 'lucide-react';
import { Link } from 'react-router';
import type { QuoteSummary } from '@/api/types';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { formatDateTime, formatMoney } from '@/lib/format';
import { messages } from '@/lib/messages';
import { StatusBadge } from './StatusBadge';

/** Warns, without blocking, that the client already has an open quote for the same product. */
export function OpenQuotesWarning({ quotes }: { quotes: QuoteSummary[] }) {
  return (
    <Alert>
      <TriangleAlertIcon />
      <AlertTitle>{messages.openQuotes.title}</AlertTitle>
      <AlertDescription className="flex flex-col gap-1.5">
        <p>{messages.openQuotes.description}</p>
        <ul className="flex flex-col gap-1">
          {quotes.map((quote) => (
            <li key={quote.id} className="flex flex-wrap items-center gap-2">
              <Link to={`/quotes/${quote.id}`} className="font-medium">
                {quote.externalReference}
              </Link>
              <StatusBadge status={quote.status} />
              <span>{messages.openQuotes.summary(formatMoney(quote.monthlyPremium, quote.currency), formatDateTime(quote.createdAt))}</span>
            </li>
          ))}
        </ul>
      </AlertDescription>
    </Alert>
  );
}

import { Link, useNavigate } from 'react-router';
import type { QuoteSummary } from '@/api/types';
import { Skeleton } from '@/components/ui/skeleton';
import { Table, TableBody, TableCaption, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { formatAmount, formatDateTime, formatMoney } from '@/lib/format';
import { messages } from '@/lib/messages';
import { productLabel } from '../products';
import { StatusBadge } from './StatusBadge';

const { columns } = messages.quoteTable;

const COLUMNS = [
  columns.reference,
  columns.client,
  columns.product,
  columns.cover,
  columns.monthlyPremium,
  columns.status,
  columns.adviser,
  columns.created,
] as const;

const NUMERIC_COLUMNS = new Set<string>([columns.cover, columns.monthlyPremium]);

function QuoteTableHeader() {
  return (
    <TableHeader>
      <TableRow>
        {COLUMNS.map((column) => (
          <TableHead key={column} className={NUMERIC_COLUMNS.has(column) ? 'text-right' : undefined}>
            {column}
          </TableHead>
        ))}
      </TableRow>
    </TableHeader>
  );
}

export function QuoteTable({ quotes }: { quotes: QuoteSummary[] }) {
  const navigate = useNavigate();

  return (
    <Table>
      <TableCaption className="sr-only">{messages.quoteTable.caption}</TableCaption>
      <QuoteTableHeader />
      <TableBody>
        {quotes.map((quote) => (
          // The whole row is clickable for pointer users; keyboard and screen-reader users get the reference link.
          <TableRow key={quote.id} className="cursor-pointer" onClick={() => navigate(`/quotes/${quote.id}`)}>
            <TableCell>
              <Link
                to={`/quotes/${quote.id}`}
                className="font-medium text-primary underline-offset-4 hover:underline"
                onClick={(event) => event.stopPropagation()}
              >
                {quote.externalReference}
              </Link>
              {quote.adviserReference !== null && <div className="text-xs text-muted-foreground">{quote.adviserReference}</div>}
            </TableCell>
            <TableCell className="font-medium">{quote.clientName}</TableCell>
            <TableCell>{productLabel(quote.productCode)}</TableCell>
            <TableCell className="text-right tabular-nums">{formatAmount(quote.coverAmount, quote.currency)}</TableCell>
            <TableCell className="text-right font-medium tabular-nums">{formatMoney(quote.monthlyPremium, quote.currency)}</TableCell>
            <TableCell>
              <StatusBadge status={quote.status} />
            </TableCell>
            <TableCell className="text-muted-foreground">{quote.adviserId}</TableCell>
            <TableCell className="text-muted-foreground">{formatDateTime(quote.createdAt)}</TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}

export function QuoteTableSkeleton({ rows = 5 }: { rows?: number }) {
  return (
    <Table aria-busy="true">
      <TableCaption className="sr-only">{messages.quoteTable.loadingCaption}</TableCaption>
      <QuoteTableHeader />
      <TableBody>
        {Array.from({ length: rows }, (_, row) => (
          <TableRow key={row}>
            {COLUMNS.map((column) => (
              <TableCell key={column}>
                <Skeleton className="h-4 w-full max-w-28" />
              </TableCell>
            ))}
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}

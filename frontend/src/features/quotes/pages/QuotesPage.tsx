import { FileTextIcon, PlusIcon } from 'lucide-react';
import { Link, useSearchParams } from 'react-router';
import { QUOTE_STATUSES, type QuoteStatus } from '@/api/types';
import { ErrorAlert } from '@/components/ErrorAlert';
import { PageHeader } from '@/components/PageHeader';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader } from '@/components/ui/card';
import { Empty, EmptyContent, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from '@/components/ui/empty';
import { Spinner } from '@/components/ui/spinner';
import { messages } from '@/lib/messages';
import { QuoteFiltersBar } from '../components/QuoteFiltersBar';
import { QuoteTable, QuoteTableSkeleton } from '../components/QuoteTable';
import { useQuoteList, type QuoteFilters } from '../queries';

// Filters live in the URL so a filtered list can be bookmarked or shared, and Back restores it.
function readFilters(params: URLSearchParams): QuoteFilters {
  const status = params.get('status');

  return {
    status: QUOTE_STATUSES.includes(status as QuoteStatus) ? (status as QuoteStatus) : undefined,
    adviserId: params.get('adviserId') ?? undefined,
    adviserReference: params.get('adviserReference') ?? undefined,
  };
}

function writeFilters(filters: QuoteFilters): URLSearchParams {
  const params = new URLSearchParams();

  for (const [name, value] of Object.entries(filters)) {
    if (value !== undefined) {
      params.set(name, value);
    }
  }

  return params;
}

export function QuotesPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const filters = readFilters(searchParams);
  const quotes = useQuoteList(filters);
  const items = quotes.data?.pages.flatMap((page) => page.items) ?? [];
  const totalCount = quotes.data?.pages[0]?.totalCount ?? 0;

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title={messages.quoteList.title}
        description={
          quotes.isSuccess ? messages.quoteList.summary(totalCount, items.length) : messages.quoteList.description
        }
        actions={
          <Button asChild>
            <Link to="/quotes/new">
              <PlusIcon data-icon="inline-start" />
              {messages.common.newQuote}
            </Link>
          </Button>
        }
      />

      {quotes.isError && <ErrorAlert error={quotes.error} title={messages.quoteList.loadFailed} onRetry={() => void quotes.refetch()} />}

      <Card>
        <CardHeader className="border-b">
          {/* Re-keyed on the URL so Back/Forward resets the inputs to the filters actually applied. */}
          <QuoteFiltersBar key={searchParams.toString()} filters={filters} onChange={(next) => setSearchParams(writeFilters(next))} />
        </CardHeader>
        <CardContent className="px-0">
          {quotes.isPending && <QuoteTableSkeleton />}

          {quotes.isSuccess && items.length === 0 && (
            <Empty>
              <EmptyHeader>
                <EmptyMedia variant="icon">
                  <FileTextIcon />
                </EmptyMedia>
                <EmptyTitle>{messages.quoteList.emptyTitle}</EmptyTitle>
                <EmptyDescription>{messages.quoteList.emptyDescription}</EmptyDescription>
              </EmptyHeader>
              <EmptyContent>
                <Button variant="outline" asChild>
                  <Link to="/quotes/new">{messages.quoteList.captureQuote}</Link>
                </Button>
              </EmptyContent>
            </Empty>
          )}

          {items.length > 0 && <QuoteTable quotes={items} />}
        </CardContent>
      </Card>

      {quotes.hasNextPage && (
        <div className="flex justify-center">
          <Button variant="outline" onClick={() => void quotes.fetchNextPage()} disabled={quotes.isFetchingNextPage}>
            {quotes.isFetchingNextPage && <Spinner data-icon="inline-start" />}
            {quotes.isFetchingNextPage ? messages.quoteList.loading : messages.quoteList.loadMore}
          </Button>
        </div>
      )}
    </div>
  );
}

import type { ReactNode } from 'react';
import { ArrowLeftIcon, CircleCheckIcon, InfoIcon, SearchXIcon } from 'lucide-react';
import { Link, useLocation, useParams } from 'react-router';
import { ApiError } from '@/api/http';
import type { Quote } from '@/api/types';
import { ErrorAlert } from '@/components/ErrorAlert';
import { PageHeader } from '@/components/PageHeader';
import { TraceId } from '@/components/TraceId';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Empty, EmptyContent, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from '@/components/ui/empty';
import { Separator } from '@/components/ui/separator';
import { Skeleton } from '@/components/ui/skeleton';
import { formatAmount, formatDate, formatDateTime, formatMoney } from '@/lib/format';
import { messages } from '@/lib/messages';
import { productLabel } from '../products';
import { useQuote } from '../queries';
import { StatusBadge } from '../components/StatusBadge';
import { StatusChangePanel } from '../components/StatusChangePanel';
import { StatusHistory } from '../components/StatusHistory';

/** Passed by the create page so the detail page can confirm what just happened. */
export interface QuoteDetailLocationState {
  created?: { replayed: boolean; traceId: string };
}

export function QuoteDetailPage() {
  const { quoteId = '' } = useParams();
  const location = useLocation();
  const created = (location.state as QuoteDetailLocationState | null)?.created;
  const quote = useQuote(quoteId);

  return (
    <div className="flex flex-col gap-6">
      <Button variant="ghost" size="sm" className="w-fit" asChild>
        <Link to="/quotes">
          <ArrowLeftIcon data-icon="inline-start" />
          {messages.common.quotes}
        </Link>
      </Button>

      {quote.isPending && <QuoteDetailSkeleton />}

      {quote.isError &&
        (quote.error instanceof ApiError && quote.error.status === 404 ? (
          <Empty className="border">
            <EmptyHeader>
              <EmptyMedia variant="icon">
                <SearchXIcon />
              </EmptyMedia>
              <EmptyTitle>{messages.quoteDetail.notFoundTitle}</EmptyTitle>
              <EmptyDescription>{messages.quoteDetail.notFoundDescription}</EmptyDescription>
            </EmptyHeader>
            <EmptyContent>
              <Button variant="outline" asChild>
                <Link to="/quotes">{messages.common.backToQuotes}</Link>
              </Button>
            </EmptyContent>
          </Empty>
        ) : (
          <ErrorAlert error={quote.error} title={messages.quoteDetail.loadFailed} onRetry={() => void quote.refetch()} />
        ))}

      {quote.isSuccess && (
        <>
          {created !== undefined && <CreatedAlert replayed={created.replayed} traceId={created.traceId} />}
          <QuoteDetail quote={quote.data} />
        </>
      )}
    </div>
  );
}

function CreatedAlert({ replayed, traceId }: { replayed: boolean; traceId: string }) {
  return (
    <Alert role="status" className={replayed ? 'border-info/40' : 'border-success/40'}>
      {replayed ? <InfoIcon className="text-info" /> : <CircleCheckIcon className="text-success" />}
      <AlertTitle>{replayed ? messages.quoteDetail.replayedTitle : messages.quoteDetail.createdTitle}</AlertTitle>
      <AlertDescription className="flex flex-col gap-1.5">
        <p>{replayed ? messages.quoteDetail.replayedDescription : messages.quoteDetail.createdDescription}</p>
        <TraceId traceId={traceId} />
      </AlertDescription>
    </Alert>
  );
}

function DetailRow({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="flex flex-col gap-0.5">
      <dt className="text-xs text-muted-foreground">{label}</dt>
      <dd className="text-sm font-medium">{children}</dd>
    </div>
  );
}

function QuoteDetail({ quote }: { quote: Quote }) {
  return (
    <>
      <PageHeader
        title={
          <>
            {quote.client.fullName} <StatusBadge status={quote.status} />
          </>
        }
        description={
          <>
            {quote.externalReference}
            {quote.adviserReference !== null && ` · ${quote.adviserReference}`} · {messages.quoteDetail.adviser(quote.adviserId)}
          </>
        }
      />

      <div className="grid items-start gap-6 lg:grid-cols-3">
        <div className="flex min-w-0 flex-col gap-6 lg:col-span-2">
          <Card>
            <CardHeader>
              <CardDescription>
                <h2>{messages.quoteDetail.monthlyPremium}</h2>
              </CardDescription>
              <CardTitle className="text-4xl font-semibold tracking-tight tabular-nums">
                {formatMoney(quote.premium.monthlyAmount, quote.premium.currency)}
              </CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-xs text-muted-foreground">{messages.quoteDetail.pricingVersion(quote.premium.pricingVersion)}</p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>
                <h2>{messages.quoteDetail.details}</h2>
              </CardTitle>
            </CardHeader>
            <CardContent className="flex flex-col gap-5">
              <dl className="grid grid-cols-2 gap-x-6 gap-y-4 sm:grid-cols-3">
                <DetailRow label={messages.quoteDetail.product}>{productLabel(quote.product.productCode)}</DetailRow>
                <DetailRow label={messages.quoteDetail.cover}>{formatAmount(quote.product.coverAmount, quote.premium.currency)}</DetailRow>
                <DetailRow label={messages.quoteDetail.term}>{messages.quoteDetail.termYears(quote.product.termYears)}</DetailRow>
                <DetailRow label={messages.quoteDetail.dateOfBirth}>{formatDate(quote.client.dateOfBirth)}</DetailRow>
                <DetailRow label={messages.quoteDetail.smoker}>{quote.client.isSmoker ? messages.common.yes : messages.common.no}</DetailRow>
                <DetailRow label={messages.quoteDetail.captured}>{formatDateTime(quote.createdAt)}</DetailRow>
                <DetailRow label={messages.quoteDetail.lastUpdated}>{formatDateTime(quote.updatedAt)}</DetailRow>
              </dl>
              {quote.notes !== null && (
                <>
                  <Separator />
                  <div className="flex flex-col gap-1">
                    <h3 className="text-xs text-muted-foreground">{messages.quoteDetail.notes}</h3>
                    <p className="text-sm whitespace-pre-wrap break-words">{quote.notes}</p>
                  </div>
                </>
              )}
            </CardContent>
          </Card>
        </div>

        <div className="flex min-w-0 flex-col gap-6">
          <StatusChangePanel quote={quote} />
          <StatusHistory history={quote.statusHistory} />
        </div>
      </div>
    </>
  );
}

function QuoteDetailSkeleton() {
  return (
    <div className="flex flex-col gap-6" aria-busy="true" aria-label={messages.quoteDetail.loading}>
      <div className="flex flex-col gap-2">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-4 w-80" />
      </div>
      <div className="grid gap-6 lg:grid-cols-3">
        <div className="flex flex-col gap-6 lg:col-span-2">
          <Skeleton className="h-36 rounded-xl" />
          <Skeleton className="h-56 rounded-xl" />
        </div>
        <div className="flex flex-col gap-6">
          <Skeleton className="h-36 rounded-xl" />
          <Skeleton className="h-56 rounded-xl" />
        </div>
      </div>
    </div>
  );
}

import { useMemo } from 'react';
import { RotateCwIcon, TriangleAlertIcon } from 'lucide-react';
import { isRouteErrorResponse, Link, useRouteError } from 'react-router';
import { newTraceparent } from '@/api/tracing';
import { TraceId } from '@/components/TraceId';
import { Button } from '@/components/ui/button';
import { Empty, EmptyContent, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from '@/components/ui/empty';
import { messages } from '@/lib/messages';

function isChunkLoadError(error: unknown): boolean {
  // After a deploy, an open tab can ask for a lazy page chunk that no longer exists; a reload fetches the new build.
  return error instanceof TypeError && /dynamically imported module|Failed to fetch|Importing a module script failed/i.test(error.message);
}

/**
 * Shown when a page throws while rendering or loading. It sits inside the layout, so the header and navigation still
 * work. Advisers see a plain message and a reference to quote to support, never a stack trace.
 */
export function RouteError() {
  const error = useRouteError();
  // An id to tie the adviser's report to the console log below, in the same format as API trace ids.
  const reference = useMemo(() => newTraceparent().traceId, []);
  const newVersion = isChunkLoadError(error);

  console.error(messages.routeError.consoleLog(reference), error);

  if (isRouteErrorResponse(error) && error.status === 404) {
    return (
      <Empty className="border">
        <EmptyHeader>
          <EmptyTitle>{messages.routeError.notFoundTitle}</EmptyTitle>
          <EmptyDescription>{messages.routeError.notFoundDescription}</EmptyDescription>
        </EmptyHeader>
        <EmptyContent>
          <Button variant="outline" asChild>
            <Link to="/quotes">{messages.common.goToQuotes}</Link>
          </Button>
        </EmptyContent>
      </Empty>
    );
  }

  return (
    <Empty className="border" role="alert">
      <EmptyHeader>
        <EmptyMedia variant="icon">
          <TriangleAlertIcon />
        </EmptyMedia>
        <EmptyTitle>{newVersion ? messages.routeError.newVersionTitle : messages.routeError.failedTitle}</EmptyTitle>
        <EmptyDescription>
          {newVersion ? messages.routeError.newVersionDescription : messages.routeError.failedDescription}
        </EmptyDescription>
      </EmptyHeader>
      <EmptyContent className="items-center">
        <div className="flex flex-wrap justify-center gap-2">
          <Button onClick={() => window.location.reload()}>
            <RotateCwIcon data-icon="inline-start" />
            {messages.common.reload}
          </Button>
          <Button variant="outline" asChild>
            <Link to="/quotes">{messages.common.goToQuotes}</Link>
          </Button>
        </div>
        {!newVersion && <TraceId traceId={reference} />}
      </EmptyContent>
    </Empty>
  );
}

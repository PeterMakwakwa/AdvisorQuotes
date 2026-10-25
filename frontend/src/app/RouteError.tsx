import { useMemo } from 'react';
import { RotateCwIcon, TriangleAlertIcon } from 'lucide-react';
import { isRouteErrorResponse, Link, useRouteError } from 'react-router';
import { newTraceparent } from '@/api/tracing';
import { TraceId } from '@/components/TraceId';
import { Button } from '@/components/ui/button';
import { Empty, EmptyContent, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from '@/components/ui/empty';

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

  console.error(`Unhandled UI error (reference ${reference})`, error);

  if (isRouteErrorResponse(error) && error.status === 404) {
    return (
      <Empty className="border">
        <EmptyHeader>
          <EmptyTitle>Page not found</EmptyTitle>
          <EmptyDescription>The page you asked for does not exist.</EmptyDescription>
        </EmptyHeader>
        <EmptyContent>
          <Button variant="outline" asChild>
            <Link to="/quotes">Go to quotes</Link>
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
        <EmptyTitle>{newVersion ? 'A new version is available' : 'This page could not be shown'}</EmptyTitle>
        <EmptyDescription>
          {newVersion
            ? 'The app was updated while you had it open. Reload to continue.'
            : 'Something went wrong on our side. Reload the page, and if it keeps happening contact support with the reference below.'}
        </EmptyDescription>
      </EmptyHeader>
      <EmptyContent className="items-center">
        <div className="flex flex-wrap justify-center gap-2">
          <Button onClick={() => window.location.reload()}>
            <RotateCwIcon data-icon="inline-start" />
            Reload
          </Button>
          <Button variant="outline" asChild>
            <Link to="/quotes">Go to quotes</Link>
          </Button>
        </div>
        {!newVersion && <TraceId traceId={reference} />}
      </EmptyContent>
    </Empty>
  );
}

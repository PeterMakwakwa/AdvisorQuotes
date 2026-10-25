import { CompassIcon } from 'lucide-react';
import { Link, Navigate, type RouteObject } from 'react-router';
import { Button } from '@/components/ui/button';
import { Empty, EmptyContent, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from '@/components/ui/empty';
import { Spinner } from '@/components/ui/spinner';
import { AppLayout } from './AppLayout';
import { RouteError } from './RouteError';

function NotFoundPage() {
  return (
    <Empty className="border">
      <EmptyHeader>
        <EmptyMedia variant="icon">
          <CompassIcon />
        </EmptyMedia>
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

export const routes: RouteObject[] = [
  {
    element: <AppLayout />,
    // Shown for the moment the first lazy page is downloading on a cold load.
    hydrateFallbackElement: (
      <div className="flex min-h-svh items-center justify-center">
        <Spinner className="size-6 text-muted-foreground" />
      </div>
    ),
    children: [
      {
        // A pathless route that holds the boundary, so an error keeps the layout and navigation around it.
        errorElement: <RouteError />,
        children: [
          { index: true, element: <Navigate to="/quotes" replace /> },
          // Each page loads on first visit, so the initial bundle carries only the shell and the page being opened.
          {
            path: 'quotes',
            lazy: async () => ({ Component: (await import('@/features/quotes/pages/QuotesPage')).QuotesPage }),
          },
          {
            path: 'quotes/new',
            lazy: async () => ({ Component: (await import('@/features/quotes/pages/NewQuotePage')).NewQuotePage }),
          },
          {
            path: 'quotes/:quoteId',
            lazy: async () => ({ Component: (await import('@/features/quotes/pages/QuoteDetailPage')).QuoteDetailPage }),
          },
          { path: '*', element: <NotFoundPage /> },
        ],
      },
    ],
  },
];

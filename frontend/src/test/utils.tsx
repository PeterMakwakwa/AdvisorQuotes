import { QueryClient } from '@tanstack/react-query';
import { render } from '@testing-library/react';
import { createMemoryRouter, RouterProvider, type RouteObject } from 'react-router';
import { vi } from 'vitest';
import { AppProviders } from '@/app/AppProviders';
import type { Quote } from '@/api/types';

export function renderRoutes(routes: RouteObject[], initialPath: string) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  const router = createMemoryRouter(routes, { initialEntries: [initialPath] });

  const result = render(
    <AppProviders queryClient={queryClient} initialAdviserId="ADV-TEST">
      <RouterProvider router={router} />
    </AppProviders>,
  );

  return { ...result, router };
}

export function jsonResponse(body: unknown, status = 200, headers: Record<string, string> = {}): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json', 'X-Trace-Id': 'trace-from-api', ...headers },
  });
}

/** Stubs fetch with a handler and returns the mock so tests can inspect the requests. */
export function stubFetch(handler: (url: string, init: RequestInit) => Response | Promise<Response>) {
  const fetchMock = vi.fn((input: RequestInfo | URL, init: RequestInit = {}) => Promise.resolve(handler(String(input), init)));
  vi.stubGlobal('fetch', fetchMock);

  return fetchMock;
}

export function buildQuote(overrides: Partial<Quote> = {}): Quote {
  return {
    id: '01a16ee7-f04b-7d15-b9e1-b1642ead52af',
    externalReference: 'Q-2026-000001',
    adviserId: 'ADV-TEST',
    adviserReference: null,
    client: { fullName: 'Thandi Nkosi', dateOfBirth: '1990-05-01', isSmoker: false },
    product: { productCode: 'TERM_LIFE', coverAmount: 1_000_000, termYears: 20 },
    premium: { monthlyAmount: 300, currency: 'ZAR', pricingVersion: '2026.1' },
    status: 'Draft',
    allowedNextStatuses: ['SentToClient', 'Declined'],
    notes: null,
    createdAt: '2026-10-24T10:00:00+00:00',
    updatedAt: '2026-10-24T10:00:00+00:00',
    statusHistory: [
      {
        fromStatus: null,
        toStatus: 'Draft',
        reason: 'Quote created',
        changedBy: 'ADV-TEST',
        changedAt: '2026-10-24T10:00:00+00:00',
      },
    ],
    ...overrides,
  };
}

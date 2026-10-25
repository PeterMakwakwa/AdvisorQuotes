import axe from 'axe-core';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { AppLayout } from '@/app/AppLayout';
import { RouteError } from '@/app/RouteError';
import { routes } from '@/app/routes';
import { buildQuote, jsonResponse, renderRoutes, stubFetch } from './utils';

/** Runs the WCAG 2.2 A/AA rules. Colour contrast needs real layout, so it is checked in the browser, not jsdom. */
async function expectNoAxeViolations() {
  const results = await axe.run(document.body, {
    runOnly: { type: 'tag', values: ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'] },
    rules: { 'color-contrast': { enabled: false } },
  });
  const summary = results.violations.map((violation) => `${violation.id}: ${violation.nodes.map((node) => node.target.join(' ')).join(', ')}`);

  expect(summary).toEqual([]);
}

const listResponse = {
  items: [
    {
      id: 'q1',
      externalReference: 'Q-2026-000007',
      adviserId: 'ADV-001',
      adviserReference: 'CRM-1',
      clientName: 'Sipho Dlamini',
      productCode: 'TERM_LIFE',
      coverAmount: 500000,
      monthlyPremium: 150,
      currency: 'ZAR',
      status: 'SentToClient',
      createdAt: '2026-10-24T10:00:00+00:00',
      updatedAt: '2026-10-24T10:00:00+00:00',
    },
  ],
  pageSize: 20,
  totalCount: 1,
  nextCursor: null,
};

describe('accessibility', () => {
  it('quote list', async () => {
    stubFetch(() => jsonResponse(listResponse));
    renderRoutes(routes, '/quotes');
    await screen.findByRole('link', { name: 'Q-2026-000007' });

    await expectNoAxeViolations();
  });

  it('new quote form, including its error state', async () => {
    stubFetch(() => jsonResponse({}));
    const user = userEvent.setup();
    renderRoutes(routes, '/quotes/new');
    await user.click(await screen.findByRole('button', { name: 'Capture quote' }));
    await screen.findByText("Enter the client's full name.");

    await expectNoAxeViolations();
  });

  it('quote detail and the status dialog', async () => {
    stubFetch(() => jsonResponse(buildQuote({ notes: 'Prefers email.' })));
    const user = userEvent.setup();
    renderRoutes(routes, '/quotes/01a16ee7-f04b-7d15-b9e1-b1642ead52af');
    await expectNoAxeViolationsAfter(() => screen.findByText('Monthly premium'));

    await user.click(screen.getByRole('button', { name: 'Decline' }));
    await screen.findByRole('dialog');
    await expectNoAxeViolations();
  });
});

async function expectNoAxeViolationsAfter(ready: () => Promise<unknown>) {
  await ready();
  await expectNoAxeViolations();
}

describe('route errors', () => {
  it('keep the layout and offer a reload with a support reference', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => {});

    function Broken(): never {
      throw new Error('render failed');
    }

    renderRoutes(
      [{ element: <AppLayout />, children: [{ errorElement: <RouteError />, children: [{ path: 'broken', Component: Broken }] }] }],
      '/broken',
    );

    expect(await screen.findByText('This page could not be shown')).toBeInTheDocument();
    expect(screen.getByRole('navigation', { name: 'Main' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Reload' })).toBeInTheDocument();
    expect(screen.getByText('Support ref')).toBeInTheDocument();
    expect(screen.queryByText('render failed')).not.toBeInTheDocument();
  });
});

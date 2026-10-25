import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import type { QuoteSummary } from '@/api/types';
import { routes } from '@/app/routes';
import { buildQuote, jsonResponse, renderRoutes, stubFetch } from '@/test/utils';

const QUOTE_PATH = '/quotes/01a16ee7-f04b-7d15-b9e1-b1642ead52af';

const OPEN_QUOTES_CHECK =
  '/api/v1/quotes?adviserId=ADV-TEST&clientName=Thandi+Nkosi&clientDateOfBirth=1990-05-01&productCode=TERM_LIFE&openOnly=true&pageSize=5';

const OPEN_QUOTE_WARNING = 'This client already has an open quote for this product';

const OPEN_QUOTE: QuoteSummary = {
  id: 'q-open',
  externalReference: 'Q-2026-000001',
  adviserId: 'ADV-TEST',
  adviserReference: null,
  clientName: 'Thandi Nkosi',
  productCode: 'TERM_LIFE',
  coverAmount: 1_000_000,
  monthlyPremium: 300,
  currency: 'ZAR',
  status: 'Draft',
  createdAt: '2026-10-24T10:00:00+00:00',
  updatedAt: '2026-10-24T10:00:00+00:00',
};

/** The form checks for the client's open quotes (a list request) before it posts; `create` answers the post. */
function stubCapture(create: () => Response, openQuotes: QuoteSummary[] = []) {
  return stubFetch((url, init) => {
    if (init.method === 'POST') {
      return create();
    }

    if (url.startsWith('/api/v1/quotes?')) {
      return jsonResponse({ items: openQuotes, pageSize: 5, totalCount: openQuotes.length, nextCursor: null });
    }

    return jsonResponse(buildQuote());
  });
}

function postCalls(fetchMock: ReturnType<typeof stubFetch>) {
  return fetchMock.mock.calls.filter(([, init]) => init?.method === 'POST');
}

async function fillValidQuote(user: ReturnType<typeof userEvent.setup>) {
  // Pages are lazy routes, so wait for the form before typing.
  await user.type(await screen.findByLabelText('Full name'), 'Thandi Nkosi');
  await user.type(screen.getByLabelText('Date of birth'), '1990-05-01');
  await user.type(screen.getByLabelText('Cover amount'), '1000000');
  await user.type(screen.getByLabelText('Term'), '20');
}

describe('capturing a quote', () => {
  it('shows field errors and focuses the first one without calling the API', async () => {
    const fetchMock = stubFetch(() => jsonResponse({}));
    const user = userEvent.setup();
    renderRoutes(routes, '/quotes/new');

    await user.click(await screen.findByRole('button', { name: 'Capture quote' }));

    const fullName = screen.getByLabelText('Full name');
    expect(await screen.findByText("Enter the client's full name.")).toBeInTheDocument();
    expect(fullName).toHaveAccessibleDescription("Enter the client's full name.");
    expect(fullName).toHaveFocus();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('checks for open quotes, sends an idempotency key, then opens the new quote', async () => {
    const quote = buildQuote();
    const fetchMock = stubCapture(() => jsonResponse(quote, 201));
    const user = userEvent.setup();
    renderRoutes(routes, '/quotes/new');

    await fillValidQuote(user);
    await user.click(screen.getByLabelText('The client smokes'));
    await user.click(screen.getByRole('button', { name: 'Capture quote' }));

    expect(await screen.findByText('Quote captured')).toBeInTheDocument();
    expect(screen.getByText('R 300,00')).toBeInTheDocument();
    expect(String(fetchMock.mock.calls[0]?.[0])).toBe(OPEN_QUOTES_CHECK);
    const [url, init] = postCalls(fetchMock)[0] ?? [];
    expect(url).toBe('/api/v1/quotes');
    expect(new Headers(init?.headers).get('Idempotency-Key')).toMatch(/^[0-9a-f-]{36}$/);
    expect(JSON.parse(String(init?.body))).toMatchObject({
      adviserId: 'ADV-TEST',
      client: { fullName: 'Thandi Nkosi', isSmoker: true },
      product: { productCode: 'TERM_LIFE', coverAmount: 1_000_000, termYears: 20 },
    });
  });

  it('warns when the client already has an open quote, and captures only after Capture anyway', async () => {
    const fetchMock = stubCapture(() => jsonResponse(buildQuote(), 201), [OPEN_QUOTE]);
    const user = userEvent.setup();
    renderRoutes(routes, '/quotes/new');

    await fillValidQuote(user);
    await user.click(screen.getByRole('button', { name: 'Capture quote' }));

    expect(await screen.findByText(OPEN_QUOTE_WARNING)).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Q-2026-000001' })).toHaveAttribute('href', '/quotes/q-open');
    expect(postCalls(fetchMock)).toHaveLength(0);

    await user.click(screen.getByRole('button', { name: 'Capture anyway' }));

    expect(await screen.findByText('Quote captured')).toBeInTheDocument();
    expect(postCalls(fetchMock)).toHaveLength(1);
  });

  it('checks again when the client changes after a warning', async () => {
    const fetchMock = stubCapture(() => jsonResponse(buildQuote(), 201), [OPEN_QUOTE]);
    const user = userEvent.setup();
    renderRoutes(routes, '/quotes/new');

    await fillValidQuote(user);
    await user.click(screen.getByRole('button', { name: 'Capture quote' }));
    await screen.findByText(OPEN_QUOTE_WARNING);
    await user.type(screen.getByLabelText('Full name'), '-Mokoena');

    expect(screen.queryByText(OPEN_QUOTE_WARNING)).not.toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Capture quote' }));

    expect(await screen.findByText(OPEN_QUOTE_WARNING)).toBeInTheDocument();
    expect(postCalls(fetchMock)).toHaveLength(0);
  });

  it('still captures when the open-quote check fails', async () => {
    const fetchMock = stubFetch((url, init) => {
      if (init.method === 'POST') {
        return jsonResponse(buildQuote(), 201);
      }

      if (url.startsWith('/api/v1/quotes?')) {
        return jsonResponse({ status: 500, detail: 'Unexpected error.' }, 500);
      }

      return jsonResponse(buildQuote());
    });
    const user = userEvent.setup();
    renderRoutes(routes, '/quotes/new');

    await fillValidQuote(user);
    await user.click(screen.getByRole('button', { name: 'Capture quote' }));

    expect(await screen.findByText('Quote captured')).toBeInTheDocument();
    expect(postCalls(fetchMock)).toHaveLength(1);
  });

  it('tells the adviser when the API replayed an earlier submission', async () => {
    stubCapture(() => jsonResponse(buildQuote(), 201, { 'Idempotent-Replayed': 'true' }));
    const user = userEvent.setup();
    renderRoutes(routes, '/quotes/new');

    await fillValidQuote(user);
    await user.click(screen.getByRole('button', { name: 'Capture quote' }));

    expect(await screen.findByText('Already captured')).toBeInTheDocument();
  });

  it('puts a pricing rule error on the field it belongs to', async () => {
    stubCapture(() =>
      jsonResponse({ status: 400, detail: 'Cover for TERM_LIFE must be between 100,000 and 10,000,000.', code: 'Pricing.CoverOutOfRange' }, 400),
    );
    const user = userEvent.setup();
    renderRoutes(routes, '/quotes/new');

    await fillValidQuote(user);
    await user.click(screen.getByRole('button', { name: 'Capture quote' }));

    expect(await screen.findByText('Cover for TERM_LIFE must be between 100,000 and 10,000,000.')).toBeInTheDocument();
    expect(screen.getByLabelText('Cover amount')).toHaveAttribute('aria-invalid', 'true');
    expect(screen.queryByText('The quote was not created')).not.toBeInTheDocument();
  });
});

describe('changing a quote status', () => {
  it('requires a reason before declining', async () => {
    const fetchMock = stubFetch(() => jsonResponse(buildQuote()));
    const user = userEvent.setup();
    renderRoutes(routes, QUOTE_PATH);

    await user.click(await screen.findByRole('button', { name: 'Decline' }));
    const dialog = await screen.findByRole('dialog');
    await user.click(within(dialog).getByRole('button', { name: 'Decline' }));

    expect(await within(dialog).findByText('Give a reason for declining the quote.')).toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it('sends the change, closes the dialog and confirms with the trace id', async () => {
    const sent = buildQuote({
      status: 'SentToClient',
      allowedNextStatuses: ['Accepted', 'Declined'],
      statusHistory: [
        ...buildQuote().statusHistory,
        { fromStatus: 'Draft', toStatus: 'SentToClient', reason: null, changedBy: 'ADV-TEST', changedAt: '2026-10-24T11:00:00+00:00' },
      ],
    });
    const fetchMock = stubFetch((_url, init) => (init.method === 'POST' ? jsonResponse(sent) : jsonResponse(buildQuote())));
    const user = userEvent.setup();
    renderRoutes(routes, QUOTE_PATH);

    await user.click(await screen.findByRole('button', { name: 'Send to client' }));
    const dialog = await screen.findByRole('dialog');
    await user.click(within(dialog).getByRole('button', { name: 'Send to client' }));

    expect(await screen.findByText('Moved to Sent to client')).toBeInTheDocument();
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(screen.getByText('trace-from-api')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Mark accepted' })).toBeInTheDocument();
    expect(JSON.parse(String(fetchMock.mock.calls[1]?.[1]?.body))).toEqual({ newStatus: 'SentToClient', changedBy: 'ADV-TEST' });
  });

  it('shows the conflict and reloads the quote when someone else changed it first', async () => {
    let gets = 0;
    stubFetch((_url, init) => {
      if (init.method === 'POST') {
        return jsonResponse({ status: 409, detail: 'A quote cannot move from Declined to SentToClient.', code: 'Quote.InvalidStatusTransition' }, 409);
      }

      gets += 1;

      return jsonResponse(gets === 1 ? buildQuote() : buildQuote({ status: 'Declined', allowedNextStatuses: [] }));
    });
    const user = userEvent.setup();
    renderRoutes(routes, QUOTE_PATH);

    await user.click(await screen.findByRole('button', { name: 'Send to client' }));
    const dialog = await screen.findByRole('dialog');
    await user.click(within(dialog).getByRole('button', { name: 'Send to client' }));

    expect(await within(dialog).findByText('A quote cannot move from Declined to SentToClient.')).toBeInTheDocument();
    expect(await screen.findByText(/No further status changes are possible/)).toBeInTheDocument();
    expect(within(dialog).getByRole('button', { name: 'Send to client' })).toBeDisabled();
    expect(gets).toBe(2);
  });
});

describe('listing quotes', () => {
  it('passes filters from the URL to the API and lists the results', async () => {
    const fetchMock = stubFetch(() =>
      jsonResponse({
        items: [
          {
            id: 'q1',
            externalReference: 'Q-2026-000007',
            adviserId: 'ADV-001',
            adviserReference: null,
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
      }),
    );
    renderRoutes(routes, '/quotes?status=SentToClient');

    expect(await screen.findByRole('link', { name: 'Q-2026-000007' })).toHaveAttribute('href', '/quotes/q1');
    expect(screen.getByLabelText('Status')).toHaveValue('SentToClient');
    expect(String(fetchMock.mock.calls[0]?.[0])).toBe('/api/v1/quotes?status=SentToClient&pageSize=20');
    expect(screen.queryByRole('button', { name: 'Load more' })).not.toBeInTheDocument();
  });

  it('shows an empty state when nothing matches', async () => {
    stubFetch(() => jsonResponse({ items: [], pageSize: 20, totalCount: 0, nextCursor: null }));
    renderRoutes(routes, '/quotes');

    expect(await screen.findByText('No quotes found')).toBeInTheDocument();
  });
});

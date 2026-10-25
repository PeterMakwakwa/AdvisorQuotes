import { describe, expect, it } from 'vitest';
import { jsonResponse, stubFetch } from '@/test/utils';
import { ApiError, request } from './http';

describe('request', () => {
  it('sends a W3C traceparent and returns the trace id the API echoes', async () => {
    const fetchMock = stubFetch(() => jsonResponse({ ok: true }));

    const response = await request<{ ok: boolean }>('/quotes');

    const headers = new Headers(fetchMock.mock.calls[0]?.[1]?.headers);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/quotes');
    expect(headers.get('traceparent')).toMatch(/^00-[0-9a-f]{32}-[0-9a-f]{16}-01$/);
    expect(response).toMatchObject({ data: { ok: true }, traceId: 'trace-from-api' });
  });

  it('turns a problem response into an ApiError with its code and field errors', async () => {
    stubFetch(() =>
      jsonResponse(
        { status: 409, detail: 'A quote cannot move from Accepted to Declined.', code: 'Quote.InvalidStatusTransition' },
        409,
      ),
    );

    const error = await request('/quotes/1/status-changes', { method: 'POST', body: '{}' }).catch((caught: unknown) => caught);

    expect(error).toBeInstanceOf(ApiError);
    expect(error).toMatchObject({
      status: 409,
      code: 'Quote.InvalidStatusTransition',
      message: 'A quote cannot move from Accepted to Declined.',
      traceId: 'trace-from-api',
    });
  });

  it('reports an unreachable API as status 0 with the trace id it started', async () => {
    stubFetch(() => {
      throw new TypeError('Failed to fetch');
    });

    const error = await request('/quotes').catch((caught: unknown) => caught);

    expect(error).toBeInstanceOf(ApiError);
    expect((error as ApiError).isNetworkError).toBe(true);
    expect((error as ApiError).traceId).toMatch(/^[0-9a-f]{32}$/);
  });

  it('reports a proxy error (no X-Trace-Id from the API) as an unreachable API', async () => {
    stubFetch(() => new Response('<html>502 Bad Gateway</html>', { status: 502, headers: { 'Content-Type': 'text/html' } }));

    const error = await request('/quotes').catch((caught: unknown) => caught);

    expect((error as ApiError).isNetworkError).toBe(true);
    expect((error as ApiError).message).toBe('Could not reach the Quotes API. Check that it is running.');
  });

  it('does not leak server error details for a 500', async () => {
    stubFetch(() => jsonResponse({ detail: 'NpgsqlException: connection refused' }, 500));

    const error = await request('/quotes').catch((caught: unknown) => caught);

    expect((error as ApiError).message).not.toContain('Npgsql');
  });
});

import { newTraceparent } from './tracing';

const API_BASE_PATH = '/api/v1';

const TRACE_ID_HEADER = 'X-Trace-Id';

const UNREACHABLE_MESSAGE = 'Could not reach the Quotes API. Check that it is running.';

/** An RFC 9457 problem response, plus the API's stable error `code` and field-level `errors`. */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  code?: string;
  errors?: Record<string, string[]>;
}

export interface ApiResponse<T> {
  data: T;
  headers: Headers;
  traceId: string;
}

/** Any failed API call. `status` is 0 when the API could not be reached. */
export class ApiError extends Error {
  readonly status: number;

  readonly code: string | undefined;

  readonly fieldErrors: Record<string, string[]>;

  readonly traceId: string;

  constructor(status: number, message: string, traceId: string, problem?: ProblemDetails) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.code = problem?.code;
    this.fieldErrors = problem?.errors ?? {};
    this.traceId = traceId;
  }

  get isNetworkError(): boolean {
    return this.status === 0;
  }

  get isClientError(): boolean {
    return this.status >= 400 && this.status < 500;
  }
}

function messageFor(status: number, problem: ProblemDetails | undefined): string {
  if (status === 429) {
    return 'Too many requests. Wait a moment and try again.';
  }

  if (status >= 500) {
    return 'Something went wrong on the server. Try again, and quote the trace id if it keeps happening.';
  }

  return problem?.detail ?? problem?.title ?? `The request failed (HTTP ${status}).`;
}

async function readJson(response: Response): Promise<unknown> {
  const text = await response.text();

  if (text.length === 0) {
    return undefined;
  }

  try {
    return JSON.parse(text);
  } catch {
    return undefined;
  }
}

export async function request<T>(path: string, init: RequestInit = {}): Promise<ApiResponse<T>> {
  const traceparent = newTraceparent();
  const headers = new Headers(init.headers);
  headers.set('Accept', 'application/json');
  headers.set('traceparent', traceparent.header);

  if (init.body !== undefined) {
    headers.set('Content-Type', 'application/json');
  }

  let response: Response;

  try {
    response = await fetch(`${API_BASE_PATH}${path}`, { ...init, headers });
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') {
      throw error;
    }

    throw new ApiError(0, UNREACHABLE_MESSAGE, traceparent.traceId);
  }

  const apiTraceId = response.headers.get(TRACE_ID_HEADER);

  // The API sends X-Trace-Id on every response, so a 5xx without it came from the proxy in front (nginx's 502, or the
  // dev server's 500 when the API isn't running). Treat it like a network failure.
  if (response.status >= 500 && apiTraceId === null) {
    throw new ApiError(0, UNREACHABLE_MESSAGE, traceparent.traceId);
  }

  const traceId = apiTraceId ?? traceparent.traceId;
  const body = await readJson(response);

  if (!response.ok) {
    const problem = body as ProblemDetails | undefined;
    throw new ApiError(response.status, messageFor(response.status, problem), traceId, problem);
  }

  return { data: body as T, headers: response.headers, traceId };
}

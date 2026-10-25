// Each API call starts a W3C trace in the browser. The API continues it and echoes the id in X-Trace-Id, so one
// trace id follows a user action from the click through the API, PostgreSQL and the outbox.

export interface Traceparent {
  traceId: string;
  header: string;
}

function randomHex(byteCount: number): string {
  const bytes = new Uint8Array(byteCount);
  crypto.getRandomValues(bytes);

  return Array.from(bytes, (byte) => byte.toString(16).padStart(2, '0')).join('');
}

export function newTraceparent(): Traceparent {
  const traceId = randomHex(16);
  const spanId = randomHex(8);

  return { traceId, header: `00-${traceId}-${spanId}-01` };
}

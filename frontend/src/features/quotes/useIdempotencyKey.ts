import { useCallback, useRef } from 'react';

/**
 * Returns a key for a submission: the same key while the payload is unchanged, a new one when it changes.
 *
 * A double-click or a retry after a lost response therefore reuses the key, and the API replays the original quote
 * instead of creating a duplicate. Editing the form and submitting again is a new submission and gets a new key,
 * which avoids the API rejecting a reused key with a different body.
 */
export function useIdempotencyKey(): (payload: unknown) => string {
  const last = useRef<{ payload: string; key: string } | null>(null);

  return useCallback((payload: unknown) => {
    const serialized = JSON.stringify(payload);

    if (last.current === null || last.current.payload !== serialized) {
      last.current = { payload: serialized, key: crypto.randomUUID() };
    }

    return last.current.key;
  }, []);
}

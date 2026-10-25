import { renderHook } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { useIdempotencyKey } from './useIdempotencyKey';

describe('useIdempotencyKey', () => {
  it('reuses the key while the payload is unchanged and issues a new one when it changes', () => {
    const { result } = renderHook(() => useIdempotencyKey());

    const first = result.current({ cover: 1000 });
    const repeat = result.current({ cover: 1000 });
    const changed = result.current({ cover: 2000 });

    expect(repeat).toBe(first);
    expect(changed).not.toBe(first);
  });
});

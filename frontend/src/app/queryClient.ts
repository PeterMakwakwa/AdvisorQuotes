import { QueryClient } from '@tanstack/react-query';
import { ApiError } from '@/api/http';

export function createQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 10_000,
        // A 4xx will not fix itself on retry (not found, bad filter); network errors and 5xx get two more tries.
        retry: (failureCount, error) => !(error instanceof ApiError && error.isClientError) && failureCount < 2,
      },
    },
  });
}

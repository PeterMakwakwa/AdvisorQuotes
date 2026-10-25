import { useInfiniteQuery, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ApiError } from '@/api/http';
import { changeQuoteStatus, createQuote, getQuote, listQuotes } from '@/api/quotesApi';
import type { ChangeQuoteStatusRequest, CreateQuoteRequest, QuoteStatus } from '@/api/types';

export const PAGE_SIZE = 20;

export interface QuoteFilters {
  status?: QuoteStatus;
  adviserId?: string;
  adviserReference?: string;
}

export const quoteKeys = {
  all: ['quotes'] as const,
  lists: () => [...quoteKeys.all, 'list'] as const,
  list: (filters: QuoteFilters) => [...quoteKeys.lists(), filters] as const,
  detail: (quoteId: string) => [...quoteKeys.all, 'detail', quoteId] as const,
};

export function useQuoteList(filters: QuoteFilters) {
  return useInfiniteQuery({
    queryKey: quoteKeys.list(filters),
    queryFn: ({ pageParam, signal }) => listQuotes({ ...filters, cursor: pageParam, pageSize: PAGE_SIZE }, signal),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) => lastPage.nextCursor ?? undefined,
  });
}

export function useQuote(quoteId: string) {
  return useQuery({
    queryKey: quoteKeys.detail(quoteId),
    queryFn: ({ signal }) => getQuote(quoteId, signal),
  });
}

export function useCreateQuote() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ request, idempotencyKey }: { request: CreateQuoteRequest; idempotencyKey: string }) =>
      createQuote(request, idempotencyKey),
    // Safe only because the idempotency key is reused: if the response was lost, the retry replays the same quote.
    retry: (failureCount, error) => error instanceof ApiError && error.isNetworkError && failureCount < 2,
    onSuccess: async (result) => {
      queryClient.setQueryData(quoteKeys.detail(result.quote.id), result.quote);
      await queryClient.invalidateQueries({ queryKey: quoteKeys.lists() });
    },
  });
}

export function useChangeQuoteStatus(quoteId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: ChangeQuoteStatusRequest) => changeQuoteStatus(quoteId, request),
    onSuccess: async (result) => {
      queryClient.setQueryData(quoteKeys.detail(quoteId), result.quote);
      await queryClient.invalidateQueries({ queryKey: quoteKeys.lists() });
    },
    onError: async (error) => {
      // A 409 means someone else moved the quote first; reload it so the adviser sees the current status and options.
      if (error instanceof ApiError && error.status === 409) {
        await queryClient.invalidateQueries({ queryKey: quoteKeys.detail(quoteId) });
      }
    },
  });
}

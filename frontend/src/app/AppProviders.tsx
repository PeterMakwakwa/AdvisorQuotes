import type { ReactNode } from 'react';
import { QueryClientProvider, type QueryClient } from '@tanstack/react-query';
import { ThemeProvider } from 'next-themes';
import { TooltipProvider } from '@/components/ui/tooltip';
import { AdviserProvider } from '@/features/adviser/AdviserContext';

interface AppProvidersProps {
  queryClient: QueryClient;
  initialAdviserId?: string;
  children: ReactNode;
}

/** Everything the app needs around the router. Tests render the same providers. */
export function AppProviders({ queryClient, initialAdviserId, children }: AppProvidersProps) {
  return (
    <ThemeProvider attribute="class" defaultTheme="system" enableSystem disableTransitionOnChange>
      <QueryClientProvider client={queryClient}>
        <TooltipProvider>
          <AdviserProvider initialAdviserId={initialAdviserId}>{children}</AdviserProvider>
        </TooltipProvider>
      </QueryClientProvider>
    </ThemeProvider>
  );
}

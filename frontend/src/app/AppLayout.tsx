import { FileStackIcon } from 'lucide-react';
import { NavLink, Outlet, useLocation } from 'react-router';
import { ThemeToggle } from '@/components/ThemeToggle';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Toaster } from '@/components/ui/sonner';
import { useAdviser } from '@/features/adviser/AdviserContext';
import { cn } from '@/lib/utils';

function navLinkClass({ isActive }: { isActive: boolean }): string {
  return cn(
    'rounded-md px-3 py-1.5 text-sm font-medium transition-colors hover:bg-muted hover:text-foreground',
    isActive ? 'bg-muted text-foreground' : 'text-muted-foreground',
  );
}

export function AppLayout() {
  const { adviserId, setAdviserId } = useAdviser();
  const { pathname } = useLocation();
  // A quote's detail page belongs to the Quotes section, so keep that tab highlighted there; New quote has its own tab.
  const inQuotesSection = pathname.startsWith('/quotes') && pathname !== '/quotes/new';

  return (
    <div className="min-h-svh">
      <a
        href="#main"
        className="sr-only focus:not-sr-only focus:fixed focus:top-3 focus:left-3 focus:z-50 focus:rounded-md focus:bg-background focus:px-3 focus:py-2 focus:shadow"
      >
        Skip to content
      </a>
      <header className="sticky top-0 z-40 border-b bg-background/85 backdrop-blur supports-[backdrop-filter]:bg-background/70">
        <div className="mx-auto flex max-w-6xl flex-wrap items-center gap-x-6 gap-y-2 px-4 py-2.5">
          <NavLink to="/quotes" end className="flex items-center gap-2 font-semibold">
            <span className="flex size-7 items-center justify-center rounded-md bg-primary text-primary-foreground">
              <FileStackIcon className="size-4" />
            </span>
            Advisor Quotes
          </NavLink>
          <nav aria-label="Main" className="flex items-center gap-1">
            <NavLink to="/quotes" end className={({ isActive }) => navLinkClass({ isActive: isActive || inQuotesSection })}>
              Quotes
            </NavLink>
            <NavLink to="/quotes/new" className={navLinkClass}>
              New quote
            </NavLink>
          </nav>
          <div className="ml-auto flex items-center gap-2">
            <Label htmlFor="adviser-id" className="text-xs text-muted-foreground">
              Adviser ID
            </Label>
            <Input
              id="adviser-id"
              className="h-8 w-32"
              value={adviserId}
              onChange={(event) => setAdviserId(event.target.value)}
              maxLength={50}
              spellCheck={false}
            />
            <ThemeToggle />
          </div>
        </div>
      </header>
      <main id="main" className="mx-auto max-w-6xl px-4 py-8">
        <Outlet />
      </main>
      <Toaster position="bottom-right" />
    </div>
  );
}

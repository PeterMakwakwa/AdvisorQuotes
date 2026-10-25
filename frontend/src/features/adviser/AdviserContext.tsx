import { createContext, useContext, useMemo, useState, type ReactNode } from 'react';
import { messages } from '@/lib/messages';

// Authentication is out of scope, so the adviser says who they are. It fills AdviserId on new quotes and ChangedBy on
// status changes. With sign-in this would come from the identity token instead.

const STORAGE_KEY = 'advisor-quotes.adviser-id';

const DEFAULT_ADVISER_ID = 'ADV-001';

interface AdviserContextValue {
  adviserId: string;
  setAdviserId: (adviserId: string) => void;
}

const AdviserContext = createContext<AdviserContextValue | null>(null);

function readStoredAdviserId(): string {
  try {
    return localStorage.getItem(STORAGE_KEY) ?? DEFAULT_ADVISER_ID;
  } catch {
    return DEFAULT_ADVISER_ID;
  }
}

export function AdviserProvider({ children, initialAdviserId }: { children: ReactNode; initialAdviserId?: string }) {
  const [adviserId, setAdviserIdState] = useState<string>(() => initialAdviserId ?? readStoredAdviserId());

  const value = useMemo<AdviserContextValue>(
    () => ({
      adviserId,
      setAdviserId: (next: string) => {
        setAdviserIdState(next);

        try {
          localStorage.setItem(STORAGE_KEY, next);
        } catch {
          // Storage can be blocked; the id still works for this session.
        }
      },
    }),
    [adviserId],
  );

  return <AdviserContext.Provider value={value}>{children}</AdviserContext.Provider>;
}

export function useAdviser(): AdviserContextValue {
  const context = useContext(AdviserContext);

  if (context === null) {
    throw new Error(messages.developer.adviserProviderMissing);
  }

  return context;
}

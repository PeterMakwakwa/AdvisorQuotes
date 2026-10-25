import { ArrowLeftIcon } from 'lucide-react';
import { Link, useNavigate } from 'react-router';
import { PageHeader } from '@/components/PageHeader';
import { Button } from '@/components/ui/button';
import { messages } from '@/lib/messages';
import { CreateQuoteForm } from '../components/CreateQuoteForm';
import type { QuoteDetailLocationState } from './QuoteDetailPage';

export function NewQuotePage() {
  const navigate = useNavigate();

  return (
    <div className="flex flex-col gap-6">
      <Button variant="ghost" size="sm" className="w-fit" asChild>
        <Link to="/quotes">
          <ArrowLeftIcon data-icon="inline-start" />
          {messages.common.quotes}
        </Link>
      </Button>
      <PageHeader title={messages.newQuote.title} description={messages.newQuote.description} />
      <CreateQuoteForm
        onCreated={(result) => {
          const state: QuoteDetailLocationState = { created: { replayed: result.replayed, traceId: result.traceId } };
          navigate(`/quotes/${result.quote.id}`, { state });
        }}
      />
    </div>
  );
}

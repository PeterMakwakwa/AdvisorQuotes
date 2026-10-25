import { useState, type FormEvent } from 'react';
import { SearchIcon, UserIcon, XIcon } from 'lucide-react';
import { QUOTE_STATUSES, type QuoteStatus } from '@/api/types';
import { Button } from '@/components/ui/button';
import { Field, FieldLabel } from '@/components/ui/field';
import { Input } from '@/components/ui/input';
import { NativeSelect, NativeSelectOption } from '@/components/ui/native-select';
import { useAdviser } from '@/features/adviser/AdviserContext';
import { messages } from '@/lib/messages';
import type { QuoteFilters } from '../queries';
import { STATUS_LABELS } from '../status';

interface QuoteFiltersBarProps {
  filters: QuoteFilters;
  onChange: (filters: QuoteFilters) => void;
}

function blankToUndefined(value: string): string | undefined {
  const trimmed = value.trim();

  return trimmed === '' ? undefined : trimmed;
}

/** Text filters apply on submit rather than per keystroke; the status filter applies as soon as it changes. */
export function QuoteFiltersBar({ filters, onChange }: QuoteFiltersBarProps) {
  const { adviserId: currentAdviserId } = useAdviser();
  const [adviserId, setAdviserId] = useState(filters.adviserId ?? '');
  const [adviserReference, setAdviserReference] = useState(filters.adviserReference ?? '');
  const hasFilters = filters.status !== undefined || filters.adviserId !== undefined || filters.adviserReference !== undefined;

  function apply(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    onChange({ ...filters, adviserId: blankToUndefined(adviserId), adviserReference: blankToUndefined(adviserReference) });
  }

  return (
    <form
      role="search"
      aria-label={messages.quoteFilters.label}
      onSubmit={apply}
      className="grid gap-3 sm:grid-cols-2 lg:grid-cols-[repeat(3,minmax(0,1fr))_auto] lg:items-end"
    >
      <Field>
        <FieldLabel htmlFor="filter-status">{messages.quoteFilters.status}</FieldLabel>
        <NativeSelect
          id="filter-status"
          className="w-full"
          value={filters.status ?? ''}
          onChange={(event) => onChange({ ...filters, status: (event.target.value || undefined) as QuoteStatus | undefined })}
        >
          <NativeSelectOption value="">{messages.quoteFilters.allStatuses}</NativeSelectOption>
          {QUOTE_STATUSES.map((status) => (
            <NativeSelectOption key={status} value={status}>
              {STATUS_LABELS[status]}
            </NativeSelectOption>
          ))}
        </NativeSelect>
      </Field>
      <Field>
        <FieldLabel htmlFor="filter-adviser">{messages.quoteFilters.adviserId}</FieldLabel>
        <Input id="filter-adviser" value={adviserId} onChange={(event) => setAdviserId(event.target.value)} placeholder={messages.quoteFilters.anyAdviser} />
      </Field>
      <Field>
        <FieldLabel htmlFor="filter-reference">{messages.quoteFilters.adviserReference}</FieldLabel>
        <Input
          id="filter-reference"
          value={adviserReference}
          onChange={(event) => setAdviserReference(event.target.value)}
          placeholder={messages.quoteFilters.adviserReferencePlaceholder}
        />
      </Field>
      <div className="flex flex-wrap gap-2 sm:col-span-2 lg:col-span-1">
        <Button type="submit" variant="secondary">
          <SearchIcon data-icon="inline-start" />
          {messages.quoteFilters.search}
        </Button>
        <Button
          type="button"
          variant="ghost"
          onClick={() => onChange({ ...filters, adviserId: currentAdviserId })}
          disabled={currentAdviserId.trim() === ''}
        >
          <UserIcon data-icon="inline-start" />
          {messages.quoteFilters.myQuotes}
        </Button>
        {hasFilters && (
          <Button type="button" variant="ghost" onClick={() => onChange({})}>
            <XIcon data-icon="inline-start" />
            {messages.quoteFilters.clear}
          </Button>
        )}
      </div>
    </form>
  );
}

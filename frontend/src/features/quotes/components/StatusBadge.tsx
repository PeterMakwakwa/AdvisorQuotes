import type { QuoteStatus } from '@/api/types';
import { Badge } from '@/components/ui/badge';
import { cn } from '@/lib/utils';
import { STATUS_LABELS } from '../status';

const STATUS_STYLES: Record<QuoteStatus, string> = {
  Draft: 'bg-secondary text-secondary-foreground',
  SentToClient: 'bg-info/10 text-info',
  Accepted: 'bg-success/10 text-success',
  Declined: 'bg-destructive/10 text-destructive',
};

export function StatusBadge({ status, className }: { status: QuoteStatus; className?: string }) {
  return (
    <Badge variant="outline" className={cn('border-transparent', STATUS_STYLES[status], className)}>
      <span aria-hidden="true" className="size-1.5 rounded-full bg-current" />
      {STATUS_LABELS[status]}
    </Badge>
  );
}

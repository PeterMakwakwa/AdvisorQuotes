import type { StatusChange } from '@/api/types';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { formatDateTime } from '@/lib/format';
import { messages } from '@/lib/messages';
import { STATUS_LABELS } from '../status';
import { StatusBadge } from './StatusBadge';

/** The API returns history oldest first; show the latest change at the top. */
export function StatusHistory({ history }: { history: StatusChange[] }) {
  const newestFirst = [...history].reverse();

  return (
    <Card>
      <CardHeader>
        <CardTitle>
          <h2>{messages.statusHistory.title}</h2>
        </CardTitle>
      </CardHeader>
      <CardContent>
        <ol className="flex flex-col">
          {newestFirst.map((change, index) => (
            <li
              key={`${change.changedAt}-${change.toStatus}`}
              className="relative flex flex-col gap-1 border-l pb-5 pl-5 last:border-transparent last:pb-0"
            >
              <span
                aria-hidden="true"
                className={`absolute top-1.5 -left-[5px] size-2.5 rounded-full ring-4 ring-card ${index === 0 ? 'bg-primary' : 'bg-muted-foreground/40'}`}
              />
              <div className="flex flex-wrap items-center gap-2">
                <StatusBadge status={change.toStatus} />
                {change.fromStatus !== null && <span className="text-xs text-muted-foreground">{messages.statusHistory.from(STATUS_LABELS[change.fromStatus])}</span>}
              </div>
              {change.reason !== null && <p className="text-sm break-words">{change.reason}</p>}
              <p className="text-xs text-muted-foreground">
                {change.changedBy} · <time dateTime={change.changedAt}>{formatDateTime(change.changedAt)}</time>
              </p>
            </li>
          ))}
        </ol>
      </CardContent>
    </Card>
  );
}

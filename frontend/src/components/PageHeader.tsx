import type { ReactNode } from 'react';

interface PageHeaderProps {
  title: ReactNode;
  description?: ReactNode;
  actions?: ReactNode;
}

export function PageHeader({ title, description, actions }: PageHeaderProps) {
  return (
    <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
      <div className="flex min-w-0 flex-col gap-1">
        <h1 className="flex flex-wrap items-center gap-2 font-heading text-2xl font-semibold tracking-tight">{title}</h1>
        {description !== undefined && <div className="text-sm text-muted-foreground">{description}</div>}
      </div>
      {actions !== undefined && <div className="flex shrink-0 flex-wrap gap-2">{actions}</div>}
    </div>
  );
}

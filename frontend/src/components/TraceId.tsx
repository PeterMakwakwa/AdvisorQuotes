import { useState } from 'react';
import { CheckIcon, CopyIcon, ExternalLinkIcon } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip';
import { messages } from '@/lib/messages';
import { cn } from '@/lib/utils';

// Set VITE_TRACE_VIEWER_URL (for example http://localhost:16686 for Jaeger) to link trace ids to the trace viewer.
const TRACE_VIEWER_URL: string | undefined = import.meta.env.VITE_TRACE_VIEWER_URL;

export function TraceId({ traceId, className }: { traceId: string; className?: string }) {
  const [copied, setCopied] = useState(false);

  async function copy() {
    try {
      await navigator.clipboard.writeText(traceId);
      setCopied(true);
      window.setTimeout(() => setCopied(false), 1500);
    } catch {
      // Clipboard access can be denied; the id is still visible to select by hand.
    }
  }

  return (
    <span className={cn('inline-flex min-w-0 flex-wrap items-center gap-1.5 text-xs text-muted-foreground', className)}>
      <span className="font-medium tracking-wide uppercase" title={messages.traceId.hint}>
        {messages.traceId.label}
      </span>
      <code className="rounded bg-muted px-1.5 py-0.5 font-mono break-all text-foreground">{traceId}</code>
      <Tooltip>
        <TooltipTrigger asChild>
          <Button type="button" variant="ghost" size="icon-xs" onClick={copy} aria-label={messages.traceId.copy}>
            {copied ? <CheckIcon /> : <CopyIcon />}
          </Button>
        </TooltipTrigger>
        <TooltipContent>{copied ? messages.traceId.copied : messages.traceId.copy}</TooltipContent>
      </Tooltip>
      {TRACE_VIEWER_URL !== undefined && (
        <Tooltip>
          <TooltipTrigger asChild>
            <Button variant="ghost" size="icon-xs" asChild>
              <a href={`${TRACE_VIEWER_URL}/trace/${traceId}`} target="_blank" rel="noreferrer" aria-label={messages.traceId.openInViewer}>
                <ExternalLinkIcon />
              </a>
            </Button>
          </TooltipTrigger>
          <TooltipContent>{messages.traceId.openInViewer}</TooltipContent>
        </Tooltip>
      )}
    </span>
  );
}

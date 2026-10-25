import { CircleAlertIcon, RotateCwIcon } from 'lucide-react';
import { ApiError } from '@/api/http';
import { Alert, AlertAction, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { TraceId } from './TraceId';

interface ErrorAlertProps {
  error: unknown;
  title?: string;
  onRetry?: () => void;
}

export function ErrorAlert({ error, title = 'Something went wrong', onRetry }: ErrorAlertProps) {
  const message = error instanceof Error ? error.message : 'An unexpected error occurred.';

  return (
    <Alert variant="destructive" role="alert">
      <CircleAlertIcon />
      <AlertTitle>{title}</AlertTitle>
      <AlertDescription className="flex flex-col gap-1.5">
        <p>{message}</p>
        {error instanceof ApiError && <TraceId traceId={error.traceId} />}
      </AlertDescription>
      {onRetry !== undefined && (
        <AlertAction>
          <Button type="button" variant="outline" size="sm" onClick={onRetry}>
            <RotateCwIcon data-icon="inline-start" />
            Retry
          </Button>
        </AlertAction>
      )}
    </Alert>
  );
}

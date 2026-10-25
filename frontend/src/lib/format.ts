const LOCALE = 'en-ZA';

export function formatMoney(amount: number, currency: string): string {
  return new Intl.NumberFormat(LOCALE, { style: 'currency', currency }).format(amount);
}

/** Whole amounts such as cover, without cents. */
export function formatAmount(amount: number, currency: string): string {
  return new Intl.NumberFormat(LOCALE, { style: 'currency', currency, maximumFractionDigits: 0 }).format(amount);
}

export function formatDate(isoDate: string): string {
  // A yyyy-MM-dd date has no time zone; parse it as local midnight so it never shifts a day.
  const date = /^\d{4}-\d{2}-\d{2}$/.test(isoDate) ? new Date(`${isoDate}T00:00:00`) : new Date(isoDate);

  return new Intl.DateTimeFormat(LOCALE, { dateStyle: 'medium' }).format(date);
}

export function formatDateTime(isoDateTime: string): string {
  return new Intl.DateTimeFormat(LOCALE, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(isoDateTime));
}

/** Today's date as yyyy-MM-dd in the browser's time zone. */
export function todayIsoDate(now: Date = new Date()): string {
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');

  return `${now.getFullYear()}-${month}-${day}`;
}

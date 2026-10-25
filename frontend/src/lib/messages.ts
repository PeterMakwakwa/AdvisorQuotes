// Every piece of text the adviser sees, in one place. Grouped by the screen or component that shows it; text that
// includes values is a function. Keep the wording here and the components free of literals.

import type { QuoteStatus } from '@/api/types';

export const messages = {
  common: {
    appName: 'Advisor Quotes',
    quotes: 'Quotes',
    newQuote: 'New quote',
    goToQuotes: 'Go to quotes',
    backToQuotes: 'Back to quotes',
    reload: 'Reload',
    retry: 'Retry',
    cancel: 'Cancel',
    close: 'Close',
    optional: '(optional)',
    yes: 'Yes',
    no: 'No',
    years: 'years',
    currencySymbol: 'R',
    somethingWentWrong: 'Something went wrong',
    unexpectedError: 'An unexpected error occurred.',
  },

  layout: {
    skipToContent: 'Skip to content',
    mainNavigation: 'Main',
    adviserId: 'Adviser ID',
  },

  theme: {
    change: 'Change theme',
    light: 'Light',
    dark: 'Dark',
    system: 'System',
  },

  traceId: {
    label: 'Support ref',
    hint: 'Quote this to support. It is the trace id of the request in our logs and traces.',
    copy: 'Copy support reference',
    copied: 'Copied',
    openInViewer: 'Open in the trace viewer',
  },

  routeError: {
    notFoundTitle: 'Page not found',
    notFoundDescription: 'The page you asked for does not exist.',
    newVersionTitle: 'A new version is available',
    newVersionDescription: 'The app was updated while you had it open. Reload to continue.',
    failedTitle: 'This page could not be shown',
    failedDescription:
      'Something went wrong on our side. Reload the page, and if it keeps happening contact support with the reference below.',
    consoleLog: (reference: string) => `Unhandled UI error (reference ${reference})`,
  },

  api: {
    unreachable: 'Could not reach the Quotes API. Check that it is running.',
    tooManyRequests: 'Too many requests. Wait a moment and try again.',
    serverError: 'Something went wrong on the server. Try again, and quote the trace id if it keeps happening.',
    requestFailed: (status: number) => `The request failed (HTTP ${status}).`,
  },

  developer: {
    adviserProviderMissing: 'useAdviser must be used inside AdviserProvider.',
  },

  quoteStatus: {
    labels: {
      Draft: 'Draft',
      SentToClient: 'Sent to client',
      Accepted: 'Accepted',
      Declined: 'Declined',
    } satisfies Record<QuoteStatus, string>,
    /** The button text for moving a quote to each status. */
    actions: {
      Draft: 'Move to draft',
      SentToClient: 'Send to client',
      Accepted: 'Mark accepted',
      Declined: 'Decline',
    } satisfies Record<QuoteStatus, string>,
  },

  products: {
    TERM_LIFE: 'Term life',
    SERIOUS_ILLNESS: 'Serious illness',
  },

  quoteList: {
    title: 'Quotes',
    description: 'Every quote captured by advisers, newest first',
    summary: (totalCount: number, shownCount: number) =>
      `${totalCount === 1 ? '1 quote' : `${totalCount} quotes`}${shownCount < totalCount ? `, showing ${shownCount}` : ''}, newest first`,
    loadFailed: 'Quotes could not be loaded',
    emptyTitle: 'No quotes found',
    emptyDescription: 'No quotes match these filters. Clear them, or capture a new quote.',
    captureQuote: 'Capture a quote',
    loading: 'Loading…',
    loadMore: 'Load more',
  },

  quoteFilters: {
    label: 'Filter quotes',
    status: 'Status',
    allStatuses: 'All statuses',
    adviserId: 'Adviser ID',
    anyAdviser: 'Any adviser',
    adviserReference: 'Adviser reference',
    adviserReferencePlaceholder: 'e.g. CRM-2026-0042',
    search: 'Search',
    myQuotes: 'My quotes',
    clear: 'Clear',
  },

  quoteTable: {
    caption: 'Quotes, newest first',
    loadingCaption: 'Loading quotes',
    columns: {
      reference: 'Reference',
      client: 'Client',
      product: 'Product',
      cover: 'Cover',
      monthlyPremium: 'Monthly premium',
      status: 'Status',
      adviser: 'Adviser',
      created: 'Created',
    },
  },

  newQuote: {
    title: 'New quote',
    description: 'Capture a quote for a client. It is saved as a draft.',
  },

  createQuote: {
    notCreated: 'The quote was not created',
    adviserMissing: 'Enter your adviser ID at the top of the page before capturing a quote.',
    detailsTitle: 'Quote details',
    detailsDescription: 'The premium is calculated by the server when the quote is captured.',
    clientLegend: 'Client',
    fullName: 'Full name',
    dateOfBirth: 'Date of birth',
    smoker: 'The client smokes',
    smokerDescription: 'Smokers pay a loading on the premium.',
    coverLegend: 'Cover',
    product: 'Product',
    coverAmount: 'Cover amount',
    term: 'Term',
    referenceLegend: 'Reference and notes',
    adviserReference: 'Adviser reference',
    adviserReferenceDescription: 'Your own reference, for example a CRM case number.',
    notes: 'Notes',
    capturingAs: 'Capturing as',
    noAdviserSet: 'no adviser set',
    capturing: 'Capturing quote…',
    captureAnyway: 'Capture anyway',
    capture: 'Capture quote',
  },

  quoteValidation: {
    fullNameRequired: "Enter the client's full name.",
    dateOfBirthRequired: "Enter the client's date of birth.",
    dateOfBirthNotInPast: 'Date of birth must be in the past.',
    productRequired: 'Choose a product.',
    coverAmountRequired: 'Enter the cover amount.',
    coverAmountInvalid: 'Enter an amount greater than zero, with at most two decimals.',
    termRequired: 'Enter the term in years.',
    termInvalid: 'Enter a whole number of years.',
    declineReasonRequired: 'Give a reason for declining the quote.',
    tooLong: (maxLength: number) => `Use ${maxLength} characters or fewer.`,
  },

  openQuotes: {
    title: 'This client already has an open quote for this product',
    description: 'Open it to carry on, or choose Capture anyway to create another quote.',
    summary: (monthlyPremium: string, capturedAt: string) => `${monthlyPremium} a month, captured ${capturedAt}`,
  },

  quoteDetail: {
    loading: 'Loading quote',
    notFoundTitle: 'Quote not found',
    notFoundDescription: 'It may have been mistyped, or it does not exist.',
    loadFailed: 'The quote could not be loaded',
    replayedTitle: 'Already captured',
    replayedDescription: 'This submission had already been received, so no duplicate was created. This is the original quote.',
    createdTitle: 'Quote captured',
    createdDescription: 'The premium was calculated and the quote saved as a draft.',
    adviser: (adviserId: string) => `Adviser ${adviserId}`,
    monthlyPremium: 'Monthly premium',
    pricingVersion: (version: string) => `Calculated by the server using rate table ${version}.`,
    details: 'Details',
    product: 'Product',
    cover: 'Cover',
    term: 'Term',
    termYears: (years: number) => `${years} years`,
    dateOfBirth: 'Date of birth',
    smoker: 'Smoker',
    captured: 'Captured',
    lastUpdated: 'Last updated',
    notes: 'Notes',
  },

  statusChange: {
    title: 'Status',
    finalDescription: (statusLabel: string) => `This quote is ${statusLabel.toLowerCase()}. No further status changes are possible.`,
    openDescription: 'Move the quote on. Every change is kept in the history.',
    dialogTitle: (action: string, reference: string) => `${action}: ${reference}`,
    dialogDescription: (clientName: string, fromLabel: string, toLabel: string, adviserId: string) =>
      `${clientName}'s quote moves from ${fromLabel} to ${toLabel}, recorded as ${adviserId}.`,
    reason: 'Reason',
    reasonRequiredHint: 'Required when declining. ',
    reasonKeptHint: 'It is kept in the status history.',
    notChanged: 'The status was not changed',
    movedTo: (statusLabel: string) => `Moved to ${statusLabel}`,
    movedToDescription: (reference: string, traceId: string) => `${reference} · support ref ${traceId}`,
  },

  statusHistory: {
    title: 'History',
    from: (statusLabel: string) => `from ${statusLabel}`,
  },
} as const;

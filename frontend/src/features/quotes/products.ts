// The products the API's rate table prices (Pricing:Products in appsettings.json). Cover and term limits stay on the
// server, which reports them as field errors. A /products endpoint would let the UI load this list instead.

import { messages } from '@/lib/messages';

export interface ProductOption {
  code: string;
  label: string;
}

export const PRODUCTS: readonly ProductOption[] = [
  { code: 'TERM_LIFE', label: messages.products.TERM_LIFE },
  { code: 'SERIOUS_ILLNESS', label: messages.products.SERIOUS_ILLNESS },
];

export function productLabel(code: string): string {
  return PRODUCTS.find((product) => product.code === code)?.label ?? code;
}

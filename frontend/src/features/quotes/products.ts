// The products the API's rate table prices (Pricing:Products in appsettings.json). Cover and term limits stay on the
// server, which reports them as field errors. A /products endpoint would let the UI load this list instead.

export interface ProductOption {
  code: string;
  label: string;
}

export const PRODUCTS: readonly ProductOption[] = [
  { code: 'TERM_LIFE', label: 'Term life' },
  { code: 'SERIOUS_ILLNESS', label: 'Serious illness' },
];

export function productLabel(code: string): string {
  return PRODUCTS.find((product) => product.code === code)?.label ?? code;
}

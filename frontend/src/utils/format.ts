const CURRENCY_SYMBOLS: Record<string, string> = {
  EUR: '€',
  USD: '$',
  GBP: '£',
};

/** Formata como "# ### ##0.00 €" — milhares com espaço, decimais sempre com ponto. */
export function formatCurrency(amount: number, currency: string): string {
  const sign = amount < 0 ? '-' : '';
  const [intPart, decPart] = Math.abs(amount).toFixed(2).split('.');
  const grouped = intPart.replace(/\B(?=(\d{3})+(?!\d))/g, ' ');
  const symbol = CURRENCY_SYMBOLS[currency] ?? currency;
  return `${sign}${grouped}.${decPart} ${symbol}`;
}

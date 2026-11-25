const moneyFormatter = new Intl.NumberFormat('pl-PL', { style: 'currency', currency: 'PLN' })

export function toCents(value: number): number {
  return Math.round(value * 100)
}

export function fromCents(value: number): number {
  return value / 100
}

export function formatMoney(value: number): string {
  return moneyFormatter.format(value)
}

export function formatCents(value: number): string {
  return formatMoney(fromCents(value))
}

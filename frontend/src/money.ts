import { formatMoney as formatLocalizedMoney } from '@/i18n'

export function toCents(value: number): number {
  return Math.round(value * 100)
}

export function fromCents(value: number): number {
  return value / 100
}

export function formatMoney(value: number): string {
  return formatLocalizedMoney(value)
}

export function formatCents(value: number): string {
  return formatMoney(fromCents(value))
}

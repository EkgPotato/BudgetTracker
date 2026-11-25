import { localDateInputValue } from './date'

export type DateRangePreset = 'day' | 'month' | 'year' | 'custom'

export interface DateRange {
  from: string
  to: string
}

export function dateRangeForPreset(preset: Exclude<DateRangePreset, 'custom'>, today = localDateInputValue()): DateRange {
  const date = new Date(`${today}T12:00:00`)

  if (preset === 'day') return { from: today, to: today }
  if (preset === 'year') return { from: `${date.getFullYear()}-01-01`, to: `${date.getFullYear()}-12-31` }

  const lastDay = new Date(date.getFullYear(), date.getMonth() + 1, 0)

  return {
    from: `${today.slice(0, 7)}-01`,
    to: `${lastDay.getFullYear()}-${String(lastDay.getMonth() + 1).padStart(2, '0')}-${String(lastDay.getDate()).padStart(2, '0')}`,
  }
}

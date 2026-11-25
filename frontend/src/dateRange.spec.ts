import { describe, expect, it } from 'vitest'
import { dateRangeForPreset } from './dateRange'

describe('dateRangeForPreset', () => {
  it('creates a single-day range', () => {
    expect(dateRangeForPreset('day', '2026-11-25')).toEqual({ from: '2026-11-25', to: '2026-11-25' })
  })

  it('uses the actual last day of a month', () => {
    expect(dateRangeForPreset('month', '2024-02-12')).toEqual({ from: '2024-02-01', to: '2024-02-29' })
  })

  it('creates a whole-year range', () => {
    expect(dateRangeForPreset('year', '2026-06-15')).toEqual({ from: '2026-01-01', to: '2026-12-31' })
  })
})

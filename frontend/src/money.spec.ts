import { describe, expect, it } from 'vitest'
import { setLocale } from './i18n'
import { formatCents, fromCents, toCents } from './money'

describe('money helpers', () => {
  it('converts common decimal input into integer grosze', () => {
    expect(toCents(19.99)).toBe(1999)
    expect(toCents(0.1 + 0.2)).toBe(30)
  })

  it('formats integer grosze according to the selected locale', () => {
    setLocale('pl')
    expect(fromCents(1234)).toBe(12.34)
    expect(formatCents(1234)).toContain('12,34')
    expect(formatCents(1234)).toContain('zł')
    setLocale('en')
  })
})

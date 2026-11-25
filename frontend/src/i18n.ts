import { ref } from 'vue'
import { coreMessages } from './locales/core'
import { financeMessages } from './locales/finance'

export const supportedLocales = ['en', 'pl'] as const
export type Locale = typeof supportedLocales[number]
export type TranslationParameters = Record<string, string | number>
export type TranslationResource = Record<string, string>

const storageKey = 'grosik.locale'
const resources: Record<Locale, TranslationResource> = { en: {}, pl: {} }

export function registerTranslations(additions: Record<Locale, TranslationResource>): void {
  for (const selectedLocale of supportedLocales) Object.assign(resources[selectedLocale], additions[selectedLocale] ?? {})
}

registerTranslations(coreMessages)
registerTranslations(financeMessages)

function savedLocale(): Locale {
  if (typeof window === 'undefined') return 'en'
  const value = window.localStorage.getItem(storageKey)
  return value === 'pl' || value === 'en' ? value : 'en'
}

export const locale = ref<Locale>(savedLocale())

export function setLocale(value: Locale): void {
  locale.value = value
  if (typeof window !== 'undefined') {
    window.localStorage.setItem(storageKey, value)
    document.documentElement.lang = value
  }
}

export function t(key: string, parameters: TranslationParameters = {}): string {
  const template = resources[locale.value][key] ?? resources.en[key] ?? key
  return template.replace(/\{(\w+)\}/g, (_, name: string) => String(parameters[name] ?? `{${name}}`))
}

export function formatMoney(value: number, currency = 'PLN'): string {
  return new Intl.NumberFormat(locale.value === 'pl' ? 'pl-PL' : 'en-GB', { style: 'currency', currency }).format(value)
}

export function formatDate(value: Date | string, options: Intl.DateTimeFormatOptions = { day: '2-digit', month: 'short', year: 'numeric' }): string {
  return new Intl.DateTimeFormat(locale.value === 'pl' ? 'pl-PL' : 'en-GB', options).format(new Date(value))
}

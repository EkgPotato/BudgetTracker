export function localDateInputValue(date = new Date()): string {
  return new Date(date.getTime() - date.getTimezoneOffset() * 60_000).toISOString().slice(0, 10)
}

export function localMonthInputValue(date = new Date()): string { return localDateInputValue(date).slice(0, 7) }

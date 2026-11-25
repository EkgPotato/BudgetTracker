import { t } from './i18n'

let nextId = 0

function clearError(input: HTMLInputElement): void {
  const id = input.dataset.validationError
  if (!id) return
  document.getElementById(id)?.remove()
  const remaining = (input.getAttribute('aria-describedby') ?? '').split(/\s+/).filter(value => value && value !== id)
  if (remaining.length) input.setAttribute('aria-describedby', remaining.join(' '))
  else input.removeAttribute('aria-describedby')
  input.removeAttribute('aria-invalid')
  delete input.dataset.validationError
}

export function installValidationMessages(): void {
  document.addEventListener('invalid', event => {
    const input = event.target
    if (!(input instanceof HTMLInputElement) || !input.form) return
    event.preventDefault()
    clearError(input)
    const key = input.validity.valueMissing ? 'validation.required'
      : input.validity.typeMismatch ? 'validation.email'
        : input.validity.tooShort ? 'validation.tooShort' : 'validation.invalid'
    const message = document.createElement('span')
    message.className = 'field-error'
    message.id = `field-error-${++nextId}`
    message.textContent = t(key, { min: input.minLength })
    input.dataset.validationError = message.id
    input.setAttribute('aria-invalid', 'true')
    input.setAttribute('aria-describedby', [input.getAttribute('aria-describedby'), message.id].filter(Boolean).join(' '))
    const container = input.closest('label') ?? input.parentElement
    container?.appendChild(message)
  }, true)
  document.addEventListener('input', event => {
    if (event.target instanceof HTMLInputElement && event.target.validity.valid) clearError(event.target)
  })
}

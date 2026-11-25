import { ref } from 'vue'

export type Theme = 'light' | 'dark'
const stored = localStorage.getItem('grosik.theme')
export const theme = ref<Theme>(stored === 'dark' || stored === 'light' ? stored : (window.matchMedia?.('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'))

export function setTheme(value: Theme): void {
  theme.value = value
  document.documentElement.classList.toggle('dark', value === 'dark')
  localStorage.setItem('grosik.theme', value)
}

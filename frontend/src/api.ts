import { ref } from 'vue'
import { locale, t } from './i18n'
import type { Account, AccountType, BudgetSpace, Category, Dashboard, DateRangeReport, MonthlyReport, Transaction, TransactionKind } from './types'

const apiBaseUrl = '/api'
const accessTokenKey = 'grosik.access-token'
const userKey = 'grosik.user'

export interface AuthUser { id: string; email: string; isAdmin: boolean; displayName?: string; onboardingCompleted?: boolean }
export interface AuthResult { accessToken: string; user: AuthUser }
export interface CreateAccountRequest { budgetSpaceId: string; name: string; type: AccountType; openingBalance: number; openingBalanceDate?: string }
export interface CreateCategoryRequest { budgetSpaceId: string; name: string; appliesTo: TransactionKind; parentCategoryId: string | null; sortOrder: number }
export interface CreateTransactionRequest { accountId: string; categoryId: string; kind: TransactionKind; amount: number; occurredOn: string; description: string }
export interface CreateTransferRequest { sourceAccountId: string; destinationAccountId: string; amount: number; occurredOn: string; description: string }
export interface CompleteOnboardingRequest { displayName: string; budgetSpaceName: string; accountName: string; accountType: AccountType; openingBalance: number }
export interface ChangeEmailRequest { newEmail: string; currentPassword: string }
export interface ChangePasswordRequest { currentPassword: string; newPassword: string }

function isAuthUser(value: unknown): value is AuthUser {
  if (typeof value !== 'object' || value === null) return false
  const candidate = value as Record<string, unknown>
  return typeof candidate.id === 'string' && typeof candidate.email === 'string' && typeof candidate.isAdmin === 'boolean'
}

function readStoredUser(): AuthUser | null {
  const storedValue = localStorage.getItem(userKey)
  if (!storedValue) return null
  try { const parsedValue: unknown = JSON.parse(storedValue); if (isAuthUser(parsedValue)) return parsedValue } catch { /* discard untrusted browser data */ }
  localStorage.removeItem(userKey); localStorage.removeItem(accessTokenKey)
  return null
}

const currentUser = ref<AuthUser | null>(readStoredUser())

export const session = {
  get token(): string | null { return localStorage.getItem(accessTokenKey) },
  get user(): AuthUser | null { return currentUser.value },
  save(result: AuthResult): void { localStorage.setItem(accessTokenKey, result.accessToken); this.updateUser(result.user) },
  updateUser(user: AuthUser): void { currentUser.value = user; localStorage.setItem(userKey, JSON.stringify(user)) },
  clear(): void { currentUser.value = null; localStorage.removeItem(accessTokenKey); localStorage.removeItem(userKey) },
}

export class ApiError extends Error { constructor(public readonly status: number, message: string) { super(message); this.name = 'ApiError' } }

async function responseMessage(response: Response): Promise<string> {
  const body = await response.text()
  if (!body) return t('auth.operationFailed')
  try { const problem: unknown = JSON.parse(body); const title = typeof problem === 'object' && problem !== null ? (problem as Record<string, unknown>).title : null; if (typeof title === 'string') return title } catch { /* plain text is a supported fallback */ }
  return body
}

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  if (!(init.body instanceof FormData)) headers.set('Content-Type', 'application/json')
  if (session.token) headers.set('Authorization', `Bearer ${session.token}`)
  headers.set('Accept-Language', locale.value)
  const response = await fetch(`${apiBaseUrl}${path}`, { ...init, headers })
  if (!response.ok) { if (response.status === 401 || response.status === 403) session.clear(); throw new ApiError(response.status, await responseMessage(response)) }
  return response.status === 204 ? undefined as T : response.json() as Promise<T>
}

export const api = {
  dashboard: () => request<Dashboard>('/dashboard'), spaces: () => request<BudgetSpace[]>('/budget-spaces'), accounts: () => request<Account[]>('/accounts'), categories: () => request<Category[]>('/categories'), transactions: () => request<Transaction[]>('/transactions'),
  createAccount: (data: CreateAccountRequest) => request<Account>('/accounts', { method: 'POST', body: JSON.stringify(data) }),
  createCategory: (data: CreateCategoryRequest) => request<Category>('/categories', { method: 'POST', body: JSON.stringify(data) }),
  createTransaction: (data: CreateTransactionRequest) => request<Transaction>('/transactions', { method: 'POST', body: JSON.stringify(data) }),
  createTransfer: (data: CreateTransferRequest) => request<void>('/transfers', { method: 'POST', body: JSON.stringify(data) }),
  report: (value: string) => { const [year, month] = value.split('-'); return request<MonthlyReport>(`/reports/monthly?year=${year}&month=${Number(month)}`) },
  reportRange: (from: string, to: string) => request<DateRangeReport>(`/reports?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`),
  login: (email: string, password: string) => request<AuthResult>('/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) }),
  register: (email: string, password: string) => request<void>('/auth/register', { method: 'POST', body: JSON.stringify({ email, password }) }),
  confirmEmail: (userId: string, token: string) => request<void>('/auth/confirm-email', { method: 'POST', body: JSON.stringify({ userId, token }) }),
  completeOnboarding: (data: CompleteOnboardingRequest) => request<AuthUser>('/auth/onboarding', { method: 'POST', body: JSON.stringify(data) }),
  me: () => request<AuthUser>('/auth/me'),
  updateProfile: (displayName: string, onboardingCompleted: boolean) => request<AuthUser>('/auth/profile', { method: 'PUT', body: JSON.stringify({ displayName, onboardingCompleted }) }),
  changeEmail: (data: ChangeEmailRequest) => request<AuthUser>('/auth/email', { method: 'PUT', body: JSON.stringify(data) }),
  changePassword: (data: ChangePasswordRequest) => request<void>('/auth/password', { method: 'PUT', body: JSON.stringify(data) }),
}

export function apiErrorMessage(error: unknown, fallback: string): string { return error instanceof ApiError && error.message ? error.message : fallback }

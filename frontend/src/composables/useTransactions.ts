import { computed, ref, type ComputedRef, type Ref } from 'vue'
import { api } from '@/api'
import { dateRangeForPreset, type DateRangePreset } from '@/dateRange'
import { t } from '@/i18n'
import { toCents } from '@/money'
import type { Account, Category, Transaction, TransactionKind } from '@/types'

export interface TransactionFilters {
  kinds: TransactionKind[]
  accountIds: string[]
  categoryIds: string[]
  from: string
  to: string
}

export interface TransactionCategoryTotal {
  name: string
  cents: number
}

interface UseTransactionsReturn {
  items: Ref<Transaction[]>
  accounts: Ref<Account[]>
  categories: Ref<Category[]>
  filters: Ref<TransactionFilters>
  datePreset: Ref<DateRangePreset>
  filteredItems: ComputedRef<Transaction[]>
  incomeCents: ComputedRef<number>
  expenseCents: ComputedRef<number>
  balanceCents: ComputedRef<number>
  categoryBreakdown: ComputedRef<TransactionCategoryTotal[]>
  maxCategoryCents: ComputedRef<number>
  load: () => Promise<void>
  applyPreset: () => void
  resetFilters: () => void
}

const emptyFilters = (): TransactionFilters => ({ kinds: [], accountIds: [], categoryIds: [], from: '', to: '' })

export function useTransactions(): UseTransactionsReturn {
  const items = ref<Transaction[]>([])
  const accounts = ref<Account[]>([])
  const categories = ref<Category[]>([])
  const filters = ref<TransactionFilters>(emptyFilters())
  const datePreset = ref<DateRangePreset>('month')

  const filteredItems = computed(() => items.value.filter(transaction =>
    (!filters.value.kinds.length || filters.value.kinds.includes(transaction.kind)) &&
    (!filters.value.accountIds.length || filters.value.accountIds.includes(transaction.accountId)) &&
    (!filters.value.categoryIds.length || (transaction.categoryId != null && filters.value.categoryIds.includes(transaction.categoryId))) &&
    (!filters.value.from || transaction.occurredOn >= filters.value.from) &&
    (!filters.value.to || transaction.occurredOn <= filters.value.to)))

  const incomeCents = computed(() => filteredItems.value
    .filter(item => item.kind === 'income')
    .reduce((total, item) => total + toCents(item.amount), 0))
  const expenseCents = computed(() => filteredItems.value
    .filter(item => item.kind === 'expense')
    .reduce((total, item) => total + toCents(item.amount), 0))
  const balanceCents = computed(() => incomeCents.value - expenseCents.value)
  const categoryBreakdown = computed(() => {
    const totals = new Map<string, TransactionCategoryTotal>()
    for (const item of filteredItems.value.filter(entry => entry.kind === 'expense')) {
      const key = item.categoryId ?? 'other'
      const current = totals.get(key) ?? { name: item.categoryName || t('finance.transactions.noCategory'), cents: 0 }
      current.cents += toCents(item.amount)
      totals.set(key, current)
    }
    return [...totals.values()].sort((left, right) => right.cents - left.cents).slice(0, 4)
  })
  const maxCategoryCents = computed(() => Math.max(...categoryBreakdown.value.map(item => item.cents), 1))

  async function load(): Promise<void> {
    [items.value, accounts.value, categories.value] = await Promise.all([api.transactions(), api.accounts(), api.categories()])
  }

  function applyPreset(): void {
    if (datePreset.value === 'custom') return
    Object.assign(filters.value, dateRangeForPreset(datePreset.value))
  }

  function resetFilters(): void {
    filters.value = emptyFilters()
    datePreset.value = 'month'
    applyPreset()
  }

  return {
    items,
    accounts,
    categories,
    filters,
    datePreset,
    filteredItems,
    incomeCents,
    expenseCents,
    balanceCents,
    categoryBreakdown,
    maxCategoryCents,
    load,
    applyPreset,
    resetFilters,
  }
}

import { computed, ref, type ComputedRef, type Ref } from 'vue'
import { api } from '@/api'
import { dateRangeForPreset, type DateRangePreset } from '@/dateRange'
import { t } from '@/i18n'
import { toCents } from '@/money'
import type { Account, Category, Transaction, OperationKind } from '@/types'

export interface TransactionFilters {
  kinds: OperationKind[]
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
  total: Ref<number>
  page: Ref<number>
  pageSize: number
  load: () => Promise<void>
  loadPage: () => Promise<void>
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
  const total = ref(0)
  const page = ref(1)
  const pageSize = 25
  let requestVersion = 0

  const selectedCategories = computed(() => {
    const selected = new Set(filters.value.categoryIds)
    let changed = true
    while (changed) {
      changed = false
      for (const category of categories.value) {
        if (category.parentCategoryId && selected.has(category.parentCategoryId) && !selected.has(category.id)) {
          selected.add(category.id)
          changed = true
        }
      }
    }
    return selected
  })

  const filteredItems = computed(() => items.value.filter(transaction =>
    (!filters.value.kinds.length || filters.value.kinds.includes(transaction.kind)) &&
    (!filters.value.accountIds.length || filters.value.accountIds.includes(transaction.accountId)) &&
    (!filters.value.categoryIds.length || (transaction.categoryId != null && selectedCategories.value.has(transaction.categoryId))) &&
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
    ;[accounts.value, categories.value] = await Promise.all([api.accounts(), api.categories()])
    await loadPage()
  }

  async function loadPage(): Promise<void> {
    const version = ++requestVersion
    const result = await api.transactionPage({
      offset: (page.value - 1) * pageSize, limit: pageSize,
      from: filters.value.from, to: filters.value.to,
      kinds: filters.value.kinds, accountIds: filters.value.accountIds,
      categoryIds: [...selectedCategories.value],
    })
    if (version !== requestVersion) return
    items.value = result.items
    total.value = result.total
  }

  function applyPreset(): void {
    if (datePreset.value === 'custom') return
    Object.assign(filters.value, dateRangeForPreset(datePreset.value))
  }

  function resetFilters(): void {
    filters.value = emptyFilters()
    datePreset.value = 'custom'
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
    total,
    page,
    pageSize,
    load,
    loadPage,
    applyPreset,
    resetFilters,
  }
}

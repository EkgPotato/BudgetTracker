import { describe, expect, it } from 'vitest'
import { useTransactions } from './useTransactions'
import type { Category, Transaction } from '@/types'

describe('transaction filters', () => {
  it('includes descendants when a parent category is selected', () => {
    const state = useTransactions()
    state.categories.value = [
      { id: 'home', name: 'Home', parentCategoryId: null },
      { id: 'food', name: 'Food', parentCategoryId: 'home' },
      { id: 'other', name: 'Other', parentCategoryId: null },
    ] as Category[]
    state.items.value = [
      { id: '1', categoryId: 'food', kind: 'expense', occurredOn: '2026-01-01' },
      { id: '2', categoryId: 'other', kind: 'expense', occurredOn: '2026-01-01' },
    ] as Transaction[]
    state.filters.value.categoryIds = ['home']

    expect(state.filteredItems.value.map(item => item.id)).toEqual(['1'])
  })

  it('clears date limits as well as other filters', () => {
    const state = useTransactions()
    state.applyPreset()
    state.filters.value.kinds = ['expense']

    state.resetFilters()

    expect(state.filters.value).toEqual({ kinds: [], accountIds: [], categoryIds: [], from: '', to: '' })
    expect(state.datePreset.value).toBe('custom')
  })
})

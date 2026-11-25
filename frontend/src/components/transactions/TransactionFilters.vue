<script setup lang="ts">
import type { DateRangePreset } from '@/dateRange'
import type { TransactionFilters } from '@/composables/useTransactions'
import type { Account, Category } from '@/types'

defineProps<{
  accounts: Account[]
  categories: Category[]
  filters: TransactionFilters
  datePreset: DateRangePreset
}>()

const emit = defineEmits<{
  clear: []
  'update:datePreset': [value: DateRangePreset]
  presetChanged: []
}>()

function updatePreset(event: Event): void {
  emit('update:datePreset', (event.target as HTMLSelectElement).value as DateRangePreset)
  emit('presetChanged')
}
</script>

<template>
  <section class="filters-card" aria-label="Filtry transakcji">
    <div class="filter-heading">
      <div><p class="section-kicker">FILTRY</p><h3>Znajdź właściwe wpisy</h3></div>
      <button type="button" class="text-button" @click="emit('clear')">Wyczyść filtry</button>
    </div>
    <div class="filters-grid">
      <label>Zakres dat
        <select :value="datePreset" @change="updatePreset">
          <option value="day">Dzień</option><option value="month">Miesiąc</option><option value="year">Rok</option><option value="custom">Własny przedział</option>
        </select>
      </label>
      <label>Od<input v-model="filters.from" :disabled="datePreset !== 'custom'" type="date"></label>
      <label>Do<input v-model="filters.to" :disabled="datePreset !== 'custom'" type="date"></label>
      <label>Rodzaje
        <select v-model="filters.kinds" multiple size="3" aria-label="Rodzaje transakcji">
          <option value="expense">Wydatki</option><option value="income">Przychody</option>
        </select>
      </label>
      <label>Konta
        <select v-model="filters.accountIds" multiple size="3" aria-label="Konta">
          <option v-for="account in accounts" :key="account.id" :value="account.id">{{ account.name }}</option>
        </select>
      </label>
      <label class="category-filter">Kategorie
        <select v-model="filters.categoryIds" multiple size="5" aria-label="Kategorie">
          <option v-for="category in categories" :key="category.id" :value="category.id">{{ category.parentCategoryId ? '↳ ' : '' }}{{ category.name }}</option>
        </select>
      </label>
    </div>
  </section>
</template>

<style scoped>
.filters-card{padding:24px 26px;border:1px solid #e1e8fa;border-radius:20px;background:linear-gradient(135deg,#fff,#f5f8ff);box-shadow:0 12px 28px #1038ab0a}.filter-heading{display:flex;align-items:flex-start;justify-content:space-between;gap:16px}.section-kicker{margin:0 0 6px;color:#4d7cfe;font-size:11px;font-weight:800;letter-spacing:.12em}.filter-heading h3{margin:0;color:#172a4d}.text-button{padding:5px;border:0;background:transparent;color:#1038ab;font:700 13px inherit;cursor:pointer}.filters-grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:13px;margin-top:18px}.filters-grid label{display:grid;gap:7px;color:#50607e;font-size:12px;font-weight:700}.filters-grid select{width:100%}.filters-grid select[multiple]{height:112px;padding-right:10px;background-image:none}.category-filter{grid-column:span 2}@media(max-width:800px){.filters-grid{grid-template-columns:1fr 1fr}.category-filter{grid-column:span 2}}@media(max-width:440px){.filters-grid{grid-template-columns:1fr}.category-filter{grid-column:auto}.filter-heading{flex-direction:column}}
</style>

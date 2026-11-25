<script setup lang="ts">
import { computed, ref } from 'vue'
import type { DateRangePreset } from '@/dateRange'
import type { TransactionFilters } from '@/composables/useTransactions'
import { t } from '@/i18n'
import type { Account, Category } from '@/types'

const props = defineProps<{
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

function updatePreset(value: DateRangePreset): void {
  emit('update:datePreset', value)
  emit('presetChanged')
}

const dateOpen = ref(false)
const dateLabel = computed(() => {
  if (!props.filters.from && !props.filters.to) return t('finance.transactions.allDates')
  const keys = { day: 'finance.transactions.day', month: 'finance.transactions.month', year: 'finance.transactions.year', custom: 'finance.transactions.custom' }
  return `${t(keys[props.datePreset])} · ${props.filters.from} – ${props.filters.to}`
})
const customRange = computed({
  get: () => props.filters.from && props.filters.to ? [props.filters.from, props.filters.to] : null,
  set: (value: string[] | null) => {
    props.filters.from = value?.[0] ?? ''
    props.filters.to = value?.[1] ?? ''
    emit('update:datePreset', 'custom')
  },
})

const categoryTree = computed(() => {
  const nodes = props.categories.map(category => ({ value: category.id, label: category.name, children: [] as { value: string; label: string }[] }))
  const roots = nodes.filter(node => !props.categories.find(category => category.id === node.value)?.parentCategoryId)
  for (const category of props.categories) {
    if (!category.parentCategoryId) continue
    const parent = nodes.find(node => node.value === category.parentCategoryId)
    const node = nodes.find(item => item.value === category.id)
    if (parent && node) parent.children.push(node)
    else if (node) roots.push(node)
  }
  return roots
})
</script>

<template>
  <section class="filters-card" :aria-label="t('finance.transactions.filtersLabel')">
    <div class="filter-heading">
      <div><p class="section-kicker">{{ t('finance.transactions.filtersKicker') }}</p><h3>{{ t('finance.transactions.filtersTitle') }}</h3></div>
      <el-button @click="emit('clear')">{{ t('finance.transactions.clearFilters') }}</el-button>
    </div>
    <div class="filters-grid">
      <label>{{ t('finance.transactions.dateRange') }}
        <el-popover v-model:visible="dateOpen" trigger="click" placement="bottom-start" :width="340">
          <template #reference><el-button class="date-trigger">{{ dateLabel }}</el-button></template>
          <div class="date-options"><el-button v-for="preset in (['day', 'month', 'year'] as const)" :key="preset" :type="datePreset === preset ? 'primary' : 'default'" @click="updatePreset(preset); dateOpen = false">{{ t(`finance.transactions.${preset}`) }}</el-button></div>
          <div class="custom-date-label">{{ t('finance.transactions.custom') }}</div>
          <el-date-picker v-model="customRange" type="daterange" value-format="YYYY-MM-DD" format="DD/MM/YYYY" :start-placeholder="t('finance.transactions.from')" :end-placeholder="t('finance.transactions.to')" style="width:100%" />
        </el-popover>
      </label>
      <label>{{ t('finance.transactions.kinds') }}
        <el-select v-model="filters.kinds" multiple filterable clearable collapse-tags collapse-tags-tooltip :aria-label="t('finance.transactions.kindsAria')">
          <el-option value="expense" :label="t('finance.transactions.expense')" /><el-option value="income" :label="t('finance.transactions.income')" />
          <el-option value="transferDebit" :label="t('finance.transactions.transferDebit')" /><el-option value="transferCredit" :label="t('finance.transactions.transferCredit')" /><el-option value="openingBalance" :label="t('finance.transactions.openingBalance')" />
        </el-select>
      </label>
      <label>{{ t('finance.transactions.accounts') }}
        <el-select v-model="filters.accountIds" multiple filterable clearable collapse-tags collapse-tags-tooltip :aria-label="t('finance.transactions.accountsAria')">
          <el-option v-for="account in accounts" :key="account.id" :value="account.id" :label="account.name" />
        </el-select>
      </label>
      <label class="category-filter">{{ t('finance.transactions.categories') }}
        <el-tree-select v-model="filters.categoryIds" :data="categoryTree" multiple show-checkbox check-strictly filterable clearable collapse-tags collapse-tags-tooltip :aria-label="t('finance.transactions.categoriesAria')" />
      </label>
    </div>
  </section>
</template>

<style scoped>
.filters-card{padding:18px 20px;border:1px solid #e1e8fa;border-radius:10px;background:#fff;box-shadow:0 6px 18px #1038ab0a}.filter-heading{display:flex;align-items:center;justify-content:space-between;gap:16px}.section-kicker{margin:0 0 4px;color:#4d7cfe;font-size:11px;font-weight:800;letter-spacing:.12em}.filter-heading h3{margin:0;color:#172a4d}.filters-grid{display:grid;grid-template-columns:1.5fr repeat(3,minmax(0,1fr));gap:12px;margin-top:14px}.filters-grid label{display:grid;gap:6px;min-width:0;color:#50607e;font-size:12px;font-weight:700}.date-trigger{width:100%;height:44px;justify-content:flex-start;overflow:hidden;text-overflow:ellipsis}.date-options{display:flex;gap:6px;flex-wrap:wrap}.date-options .el-button{margin:0}.custom-date-label{margin:14px 0 7px;font-weight:600;color:#50607e}@media(max-width:1000px){.filters-grid{grid-template-columns:repeat(2,minmax(0,1fr))}}@media(max-width:440px){.filters-grid{grid-template-columns:1fr}.filter-heading{align-items:flex-start;flex-wrap:wrap}}
</style>

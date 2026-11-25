<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { api } from '@/api'
import { formatMoney, t } from '@/i18n'
import { accountTypeLabel, type Dashboard } from '@/types'
import { dateRangeForPreset } from '@/dateRange'
import { toCents } from '@/money'
import type { DateRangeReport } from '@/types'
import '@/locales/operationMessages'
import { isDebit, operationLabel } from '@/operations'
const data = ref<Dashboard>(); const error = ref('')
const report = ref<DateRangeReport>()
const categoryBreakdown = computed(() => (report.value?.categories ?? [])
  .filter(category => category.kind === 'expense')
  .map(category => ({ name: category.category || t('finance.transactions.noCategory'), cents: toCents(category.total) }))
  .sort((a, b) => b.cents - a.cents).slice(0, 4))
const maxCategoryCents = computed(() => Math.max(1, ...categoryBreakdown.value.map(item => item.cents)))
const analytics = { categoryBreakdown, maxCategoryCents }
const analyticsError = ref(false)
const analyticsLoading = ref(false)
async function loadAnalytics(): Promise<void> {
  analyticsError.value = false
  analyticsLoading.value = true
  try { const range = dateRangeForPreset('month'); report.value = await api.reportRange(range.from, range.to) } catch { analyticsError.value = true }
  finally { analyticsLoading.value = false }
}
onMounted(async () => {
  try { data.value = await api.dashboard() } catch { error.value = t('finance.dashboard.loadError'); return }
  await loadAnalytics()
})
</script>
<template><div v-if="error" class="notice">{{ error }}</div><template v-else>
  <section class="stats" v-if="data"><article><span>{{ t('finance.dashboard.totalBalance') }}</span><strong>{{ formatMoney(data.totalBalance) }}</strong><small>{{ t('finance.dashboard.activeAccounts') }}</small></article><article><span>{{ t('finance.dashboard.monthlyIncome') }}</span><strong class="positive">{{ formatMoney(data.incomeThisMonth) }}</strong><small>{{ t('finance.dashboard.actualIncome') }}</small></article><article><span>{{ t('finance.dashboard.monthlyExpenses') }}</span><strong class="negative">{{ formatMoney(data.expenseThisMonth) }}</strong><small>{{ t('finance.dashboard.actualExpenses') }}</small></article></section>
  <section class="panel overview-chart" v-if="data"><h2>{{ t('finance.transactions.largestExpenseCategories') }}</h2><p class="muted">{{ t('finance.dashboard.monthlyExpenses') }}</p><p v-if="analyticsLoading" class="muted" role="status">{{ t('operations.analyticsLoading') }}</p><div v-else-if="analyticsError" class="notice" role="alert">{{ t('operations.analyticsFailed') }} <el-button @click="loadAnalytics">{{ t('operations.retry') }}</el-button></div><div v-else-if="analytics.categoryBreakdown.value.length" class="chart-scroll"><div class="chart-columns" role="list" :aria-label="t('finance.transactions.largestExpenseCategories')"><div v-for="entry in analytics.categoryBreakdown.value" :key="entry.name" class="chart-column" role="listitem"><strong>{{ formatMoney(entry.cents / 100) }}</strong><div class="chart-column-area"><span :style="{ height: `${entry.cents / analytics.maxCategoryCents.value * 100}%` }"></span></div><span class="chart-label">{{ entry.name }}</span></div></div></div><p v-else class="empty">{{ t('finance.transactions.noExpenses') }}</p></section>
  <section class="grid two" v-if="data"><article class="panel"><div class="panel-title"><h2>{{ t('finance.dashboard.yourAccounts') }}</h2><RouterLink to="/konta">{{ t('finance.dashboard.viewAll') }}</RouterLink></div><div class="account-row" v-for="account in data.accounts" :key="account.id"><span class="account-icon">▣</span><span>{{ account.name }}<small>{{ accountTypeLabel(account.type) }}</small></span><strong>{{ formatMoney(account.balance) }}</strong></div></article><article class="panel"><div class="panel-title"><h2>{{ t('finance.dashboard.recentTransactions') }}</h2><RouterLink to="/transakcje">{{ t('finance.dashboard.fullHistory') }}</RouterLink></div><div class="transaction-row" v-for="transaction in data.recentTransactions" :key="transaction.id"><span class="round-icon" :class="{ expense: isDebit(transaction.kind) }">{{ isDebit(transaction.kind) ? '−' : '+' }}</span><span>{{ transaction.description || transaction.categoryName || operationLabel(transaction.kind) }}<small>{{ operationLabel(transaction.kind) }} · {{ transaction.accountName }} · {{ transaction.occurredOn }}</small></span><strong :class="isDebit(transaction.kind) ? 'negative' : 'positive'">{{ isDebit(transaction.kind) ? '−' : '+' }}{{ formatMoney(transaction.amount) }}</strong></div></article></section>
  <div v-else class="loading">{{ t('finance.dashboard.loading') }}</div>
</template></template>
<style scoped>.overview-chart{margin-bottom:18px}.chart-scroll{overflow-x:auto}.chart-columns{display:flex;gap:clamp(16px,4vw,48px);align-items:end;min-width:320px;padding:12px 12px 4px}.chart-column{flex:1;min-width:70px;max-width:160px;display:grid;text-align:center;gap:8px;font-size:12px}.chart-column strong{font-size:12px;white-space:nowrap}.chart-column-area{height:170px;display:flex;justify-content:center;align-items:end;border-bottom:1px solid var(--el-border-color)}.chart-column-area span{width:min(58px,70%);min-height:3px;border-radius:5px 5px 0 0;background:linear-gradient(180deg,#7598ff,#2450c5)}.chart-label{overflow-wrap:anywhere;color:var(--el-text-color-regular);font-weight:600}</style>

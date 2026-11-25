<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { api } from '@/api'
import { formatMoney, t } from '@/i18n'
import { accountTypeLabel, type Dashboard } from '@/types'
import { useTransactions } from '@/composables/useTransactions'
const data = ref<Dashboard>(); const error = ref('')
const analytics = useTransactions()
onMounted(async () => {
  try { data.value = await api.dashboard() } catch { error.value = t('finance.dashboard.loadError'); return }
  analytics.applyPreset()
  try { await analytics.load() } catch { /* The account summary remains available if analytics cannot load. */ }
})
</script>
<template><div v-if="error" class="notice">{{ error }}</div><template v-else>
  <section class="stats" v-if="data"><article><span>{{ t('finance.dashboard.totalBalance') }}</span><strong>{{ formatMoney(data.totalBalance) }}</strong><small>{{ t('finance.dashboard.activeAccounts') }}</small></article><article><span>{{ t('finance.dashboard.monthlyIncome') }}</span><strong class="positive">{{ formatMoney(data.incomeThisMonth) }}</strong><small>{{ t('finance.dashboard.actualIncome') }}</small></article><article><span>{{ t('finance.dashboard.monthlyExpenses') }}</span><strong class="negative">{{ formatMoney(data.expenseThisMonth) }}</strong><small>{{ t('finance.dashboard.actualExpenses') }}</small></article></section>
  <section class="panel overview-chart" v-if="data"><h2>{{ t('finance.transactions.largestExpenseCategories') }}</h2><p class="muted">{{ t('finance.dashboard.actualExpenses') }}</p><div v-for="entry in analytics.categoryBreakdown.value" :key="entry.name" class="overview-bar"><div><span>{{ entry.name }}</span><strong>{{ formatMoney(entry.cents / 100) }}</strong></div><div class="overview-track"><span :style="{ width: `${entry.cents / analytics.maxCategoryCents.value * 100}%` }"></span></div></div><p v-if="!analytics.categoryBreakdown.value.length" class="empty">{{ t('finance.transactions.noExpenses') }}</p></section>
  <section class="grid two" v-if="data"><article class="panel"><div class="panel-title"><h2>{{ t('finance.dashboard.yourAccounts') }}</h2><RouterLink to="/konta">{{ t('finance.dashboard.viewAll') }}</RouterLink></div><div class="account-row" v-for="account in data.accounts" :key="account.id"><span class="account-icon">▣</span><span>{{ account.name }}<small>{{ accountTypeLabel(account.type) }}</small></span><strong>{{ formatMoney(account.balance) }}</strong></div></article><article class="panel"><div class="panel-title"><h2>{{ t('finance.dashboard.recentTransactions') }}</h2><RouterLink to="/transakcje">{{ t('finance.dashboard.fullHistory') }}</RouterLink></div><div class="transaction-row" v-for="transaction in data.recentTransactions" :key="transaction.id"><span class="round-icon" :class="transaction.kind">{{ transaction.kind === 'expense' ? '−' : '+' }}</span><span>{{ transaction.description || transaction.categoryName || t('finance.dashboard.noDescription') }}<small>{{ transaction.accountName }} · {{ transaction.occurredOn }}</small></span><strong :class="transaction.kind === 'expense' ? 'negative' : 'positive'">{{ transaction.kind === 'expense' ? '−' : '+' }}{{ formatMoney(transaction.amount) }}</strong></div></article></section>
  <div v-else class="loading">{{ t('finance.dashboard.loading') }}</div>
</template></template>
<style scoped>.overview-chart{margin-bottom:18px}.overview-bar{margin-top:16px;font-size:13px}.overview-bar>div:first-child{display:flex;justify-content:space-between;gap:12px}.overview-track{height:9px;margin-top:7px;border-radius:99px;background:#edf2fc;overflow:hidden}.overview-track span{display:block;height:100%;border-radius:inherit;background:#2450c5}</style>

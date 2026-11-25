<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { api } from '@/api'
import { formatMoney, t } from '@/i18n'
import { accountTypeLabel, type Dashboard } from '@/types'
const data = ref<Dashboard>(); const error = ref('')
onMounted(async () => { try { data.value = await api.dashboard() } catch { error.value = t('finance.dashboard.loadError') } })
</script>
<template><div v-if="error" class="notice">{{ error }}</div><template v-else>
  <section class="stats" v-if="data"><article><span>{{ t('finance.dashboard.totalBalance') }}</span><strong>{{ formatMoney(data.totalBalance) }}</strong><small>{{ t('finance.dashboard.activeAccounts') }}</small></article><article><span>{{ t('finance.dashboard.monthlyIncome') }}</span><strong class="positive">{{ formatMoney(data.incomeThisMonth) }}</strong><small>{{ t('finance.dashboard.actualIncome') }}</small></article><article><span>{{ t('finance.dashboard.monthlyExpenses') }}</span><strong class="negative">{{ formatMoney(data.expenseThisMonth) }}</strong><small>{{ t('finance.dashboard.actualExpenses') }}</small></article></section>
  <section class="grid two" v-if="data"><article class="panel"><div class="panel-title"><h2>{{ t('finance.dashboard.yourAccounts') }}</h2><RouterLink to="/konta">{{ t('finance.dashboard.viewAll') }}</RouterLink></div><div class="account-row" v-for="account in data.accounts" :key="account.id"><span class="account-icon">▣</span><span>{{ account.name }}<small>{{ accountTypeLabel(account.type) }}</small></span><strong>{{ formatMoney(account.balance) }}</strong></div></article><article class="panel"><div class="panel-title"><h2>{{ t('finance.dashboard.recentTransactions') }}</h2><RouterLink to="/transakcje">{{ t('finance.dashboard.fullHistory') }}</RouterLink></div><div class="transaction-row" v-for="transaction in data.recentTransactions" :key="transaction.id"><span class="round-icon" :class="transaction.kind">{{ transaction.kind === 'expense' ? '−' : '+' }}</span><span>{{ transaction.description || transaction.categoryName || t('finance.dashboard.noDescription') }}<small>{{ transaction.accountName }} · {{ transaction.occurredOn }}</small></span><strong :class="transaction.kind === 'expense' ? 'negative' : 'positive'">{{ transaction.kind === 'expense' ? '−' : '+' }}{{ formatMoney(transaction.amount) }}</strong></div></article></section>
  <div v-else class="loading">{{ t('finance.dashboard.loading') }}</div>
</template></template>

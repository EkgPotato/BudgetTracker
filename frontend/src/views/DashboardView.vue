<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { api } from '@/api'
import { accountTypeLabel, type Dashboard } from '@/types'
const data = ref<Dashboard>(); const error = ref('')
const money = (v: number) => new Intl.NumberFormat('pl-PL', { style: 'currency', currency: 'PLN' }).format(v)
onMounted(async () => { try { data.value = await api.dashboard() } catch { error.value = 'Nie można połączyć się z API. Uruchom backend i odśwież stronę.' } })
</script>
<template><div v-if="error" class="notice">{{ error }}</div><template v-else>
  <section class="stats" v-if="data"><article><span>Łączne saldo</span><strong>{{ money(data.totalBalance) }}</strong><small>wszystkie aktywne konta</small></article><article><span>Przychody w tym miesiącu</span><strong class="positive">{{ money(data.incomeThisMonth) }}</strong><small>rzeczywiste wpływy</small></article><article><span>Wydatki w tym miesiącu</span><strong class="negative">{{ money(data.expenseThisMonth) }}</strong><small>rzeczywiste koszty</small></article></section>
  <section class="grid two" v-if="data"><article class="panel"><div class="panel-title"><h2>Twoje konta</h2><RouterLink to="/konta">Zobacz wszystkie</RouterLink></div><div class="account-row" v-for="account in data.accounts" :key="account.id"><span class="account-icon">▣</span><span>{{ account.name }}<small>{{ accountTypeLabel(account.type) }}</small></span><strong>{{ money(account.balance) }}</strong></div></article><article class="panel"><div class="panel-title"><h2>Ostatnie operacje</h2><RouterLink to="/transakcje">Pełna historia</RouterLink></div><div class="transaction-row" v-for="t in data.recentTransactions" :key="t.id"><span class="round-icon" :class="t.kind">{{ t.kind === 'expense' ? '−' : '+' }}</span><span>{{ t.description || t.categoryName || 'Bez opisu' }}<small>{{ t.accountName }} · {{ t.occurredOn }}</small></span><strong :class="t.kind === 'expense' ? 'negative' : 'positive'">{{ t.kind === 'expense' ? '−' : '+' }}{{ money(t.amount) }}</strong></div></article></section>
  <div v-else class="loading">Ładowanie danych…</div>
</template></template>

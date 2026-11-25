<script setup lang="ts">
import { formatCents } from '@/money'
import type { TransactionCategoryTotal } from '@/composables/useTransactions'
import type { Transaction } from '@/types'

defineProps<{
  items: Transaction[]
  incomeCents: number
  expenseCents: number
  balanceCents: number
  categoryBreakdown: TransactionCategoryTotal[]
  maxCategoryCents: number
  categories: { id: string; name: string }[]
}>()
</script>

<template>
  <section class="transaction-summary" aria-label="Podsumowanie transakcji">
    <article><span>Przychody</span><strong class="income">{{ formatCents(incomeCents) }}</strong><small>dla wybranych filtrów</small></article>
    <article><span>Wydatki</span><strong class="expense">{{ formatCents(expenseCents) }}</strong><small>dla wybranych filtrów</small></article>
    <article><span>Bilans</span><strong :class="balanceCents >= 0 ? 'income' : 'expense'">{{ formatCents(balanceCents) }}</strong><small>{{ items.length }} wpisów</small></article>
  </section>
  <section class="analytics-grid">
    <article class="panel category-chart"><div class="panel-head"><div><p class="section-kicker">ANALITYKA</p><h3>Największe kategorie wydatków</h3></div><span class="chart-icon">◔</span></div><div v-if="categoryBreakdown.length" class="category-bars"><div v-for="entry in categoryBreakdown" :key="entry.name" class="bar-row"><div class="bar-label"><span>{{ entry.name }}</span><strong>{{ formatCents(entry.cents) }}</strong></div><div class="bar-track"><i :style="{ width: `${(entry.cents / maxCategoryCents) * 100}%` }"></i></div></div></div><p v-else class="empty">Brak wydatków dla wybranych filtrów.</p></article>
    <article class="panel category-guide"><p class="section-kicker">KONTEKST</p><h3>Kategorie w filtrze</h3><p>Wybierz kategorię, aby szybko porównać ją z pozostałymi wpisami w danym miesiącu.</p><div class="category-chips"><span v-for="category in categories.slice(0, 6)" :key="category.id">{{ category.name }}</span></div></article>
  </section>
</template>

<style scoped>
.transaction-summary{display:grid;grid-template-columns:repeat(3,1fr);gap:16px}.transaction-summary article{padding:19px 20px;border:1px solid #e1e8fa;border-radius:16px;background:#fff;box-shadow:0 5px 18px #1038ab0c}.transaction-summary span,.transaction-summary small{display:block;color:#71809e;font-size:12px}.transaction-summary strong{display:block;margin:9px 0 4px;font-size:25px}.income{color:#0e8a63}.expense{color:#e55b48}.analytics-grid{display:grid;grid-template-columns:1.4fr .8fr;gap:18px}.panel{padding:22px;border-radius:18px}.panel-head{display:flex;align-items:flex-start;justify-content:space-between;gap:16px}.section-kicker{margin:0 0 6px;color:#4d7cfe;font-size:11px;font-weight:800;letter-spacing:.12em}.panel h3{margin:0;color:#172a4d}.chart-icon{display:grid;place-items:center;width:38px;height:38px;border-radius:12px;background:#e8efff;color:#1038ab;font-size:20px}.category-bars{display:grid;gap:15px;margin-top:23px}.bar-label{display:flex;justify-content:space-between;gap:12px;font-size:13px}.bar-label span{font-weight:700}.bar-label strong{color:#50607e}.bar-track{height:9px;margin-top:7px;overflow:hidden;border-radius:99px;background:#edf2fc}.bar-track i{display:block;height:100%;border-radius:inherit;background:linear-gradient(90deg,#4d7cfe,#1038ab)}.category-guide{background:linear-gradient(160deg,#172a4d,#234780);color:#fff}.category-guide h3{color:#fff}.category-guide>p:not(.section-kicker){color:#c7d5f3;line-height:1.55}.category-guide .section-kicker{color:#9cb9ff}.category-chips{display:flex;flex-wrap:wrap;gap:8px;margin-top:22px}.category-chips span{padding:6px 9px;border:1px solid #5773a7;border-radius:99px;color:#dce7ff;font-size:11px}@media(max-width:800px){.transaction-summary,.analytics-grid{grid-template-columns:1fr}.transaction-summary article,.panel{padding:18px}.transaction-summary strong{font-size:22px}}
</style>

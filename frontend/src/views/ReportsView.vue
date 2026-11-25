<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { api, apiErrorMessage } from '@/api'
import { dateRangeForPreset, type DateRangePreset } from '@/dateRange'
import { t } from '@/i18n'
import { formatCents, formatMoney, toCents } from '@/money'
import type { DateRangeReport } from '@/types'

const preset = ref<DateRangePreset>('month')
const from = ref('')
const to = ref('')
const report = ref<DateRangeReport>()
const error = ref('')
const title = computed(() => t(`finance.reports.title${preset.value[0].toUpperCase()}${preset.value.slice(1)}`))
const balanceCents = computed(() => toCents(report.value?.income ?? 0) - toCents(report.value?.expenses ?? 0))
function applyPreset(): void {
  if (preset.value === 'custom') return
  const range = dateRangeForPreset(preset.value)
  from.value = range.from
  to.value = range.to
}
async function load(): Promise<void> { error.value = ''; if (!from.value || !to.value || from.value > to.value) { error.value = t('finance.reports.invalidRange'); return } try { report.value = await api.reportRange(from.value, to.value) } catch (caught: unknown) { error.value = apiErrorMessage(caught, t('finance.reports.loadError')) } }
watch(preset, () => { if (preset.value !== 'custom') applyPreset(); void load() })
onMounted(() => { applyPreset(); void load() })
</script>

<template>
  <div class="reports-page">
    <section class="report-hero"><div><p class="section-kicker">{{ t('finance.reports.kicker') }}</p><h2>{{ title }}</h2><p>{{ t('finance.reports.subtitle') }}</p></div><div class="range-controls"><label>{{ t('finance.reports.range') }}<select v-model="preset"><option value="day">{{ t('finance.reports.day') }}</option><option value="month">{{ t('finance.reports.month') }}</option><option value="year">{{ t('finance.reports.year') }}</option><option value="custom">{{ t('finance.reports.custom') }}</option></select></label><label>{{ t('finance.reports.from') }}<input v-model="from" :disabled="preset !== 'custom'" type="date" @change="load"></label><label>{{ t('finance.reports.to') }}<input v-model="to" :disabled="preset !== 'custom'" type="date" @change="load"></label><button v-if="preset === 'custom'" class="button primary" type="button" @click="load">{{ t('finance.reports.apply') }}</button></div></section>
    <p v-if="error" class="notice">{{ error }}</p>
    <section class="report-totals"><article><span>{{ t('finance.reports.income') }}</span><strong class="positive">{{ formatMoney(report?.income ?? 0) }}</strong></article><article><span>{{ t('finance.reports.expenses') }}</span><strong class="negative">{{ formatMoney(report?.expenses ?? 0) }}</strong></article><article><span>{{ t('finance.reports.balance') }}</span><strong :class="balanceCents >= 0 ? 'positive' : 'negative'">{{ formatCents(balanceCents) }}</strong></article></section>
    <section class="panel"><div class="panel-title"><div><h2>{{ t('finance.reports.categories') }}</h2><p class="muted">{{ report?.from }} — {{ report?.to }}</p></div></div><table class="report-table"><thead><tr><th>{{ t('finance.reports.category') }}</th><th>{{ t('finance.reports.kind') }}</th><th>{{ t('finance.reports.amount') }}</th><th>{{ t('finance.reports.entryCount') }}</th></tr></thead><tbody><tr v-for="row in report?.categories ?? []" :key="`${row.kind}-${row.categoryId}`"><td :data-label="t('finance.reports.category')">{{ row.category }}</td><td :data-label="t('finance.reports.kind')">{{ row.kind === 'income' ? t('finance.reports.income') : t('finance.categories.expense') }}</td><td :data-label="t('finance.reports.amount')" :class="row.kind === 'income' ? 'positive' : 'negative'">{{ formatMoney(row.total) }}</td><td :data-label="t('finance.reports.entryCount')">{{ row.count }}</td></tr><tr v-if="!(report?.categories.length)"><td colspan="4" class="empty">{{ t('finance.reports.empty') }}</td></tr></tbody></table></section>
  </div>
</template>

<style scoped>
.reports-page{display:grid;gap:20px}.report-hero{display:flex;align-items:end;justify-content:space-between;gap:22px;padding:25px 27px;border:1px solid #e1e8fa;border-radius:20px;background:linear-gradient(135deg,#fff,#f2f6ff);box-shadow:0 12px 28px #1038ab0a}.report-hero h2{margin:0;color:#172a4d;font-size:25px}.report-hero p:not(.section-kicker){margin:8px 0 0;color:#71809e}.section-kicker{margin:0 0 6px;color:#4d7cfe;font-size:11px;font-weight:800;letter-spacing:.12em}.range-controls{display:grid;grid-template-columns:130px 132px 132px auto;gap:10px;align-items:end}.range-controls label{display:grid;gap:6px}.range-controls input,.range-controls select{min-width:0;width:100%;min-height:44px}.range-controls input:disabled{color:#64748b;background:#f5f7fb}.range-controls button{height:44px;white-space:nowrap}.report-totals{display:grid;grid-template-columns:repeat(3,1fr);gap:16px}.report-totals article{padding:18px 20px;border:1px solid #e2e8f4;border-radius:16px;background:#fff;box-shadow:0 10px 28px #1a30590e}.report-totals span{display:block;color:#71809e;font-size:12px}.report-totals strong{display:block;margin-top:8px;font-size:23px}@media(max-width:1000px){.report-hero{align-items:flex-start;flex-direction:column}.range-controls{width:100%;grid-template-columns:repeat(3,1fr)}.range-controls button{grid-column:span 3}}@media(max-width:600px){.report-hero{padding:21px}.range-controls,.report-totals{grid-template-columns:1fr}.range-controls button{grid-column:auto}.report-totals article{padding:16px}}
</style>

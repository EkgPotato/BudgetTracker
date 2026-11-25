<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import TransactionFilters from '@/components/transactions/TransactionFilters.vue'
import { api, apiErrorMessage } from '@/api'
import { useTransactions } from '@/composables/useTransactions'
import { localDateInputValue } from '@/date'
import { t } from '@/i18n'
import { formatMoney } from '@/money'
import type { TransactionKind } from '@/types'
import { isDebit, operationLabel } from '@/operations'
import '@/locales/operationMessages'

const route = useRoute()
const router = useRouter()
const isModalOpen = ref(false)
const busy = ref(false)
const error = ref('')
const saved = ref('')
const pageError = ref('')
const pageMessage = ref('')
const loading = ref(false)
const form = ref({ accountId: '', categoryId: '', kind: 'expense' as TransactionKind, amount: 0, occurredOn: localDateInputValue(), description: '' })

const {
  accounts,
  categories,
  filters,
  datePreset,
  filteredItems,
  total,
  page,
  pageSize,
  load,
  loadPage: loadResults,
  applyPreset,
  resetFilters,
} = useTransactions()

const availableCategories = computed(() => categories.value.filter(category => category.appliesTo === form.value.kind))
let initialized = false
let resultsVersion = 0

async function loadPage(): Promise<void> {
  await load()
  form.value.accountId ||= accounts.value[0]?.id ?? ''
  form.value.categoryId ||= availableCategories.value[0]?.id ?? ''
}

async function refreshPage(): Promise<void> {
  loading.value = true
  pageError.value = ''
  try { await loadPage(); initialized = true } catch (caught: unknown) { pageError.value = apiErrorMessage(caught, t('finance.transactions.loadError')) }
  finally { loading.value = false }
}

async function refreshResults(): Promise<void> {
  const version = ++resultsVersion
  loading.value = true
  pageError.value = ''
  try { await loadResults() } catch (caught: unknown) { if (version === resultsVersion) pageError.value = apiErrorMessage(caught, t('finance.transactions.loadError')) }
  finally { if (version === resultsVersion) loading.value = false }
}

watch(filters, () => {
  if (!initialized) return
  if (page.value !== 1) page.value = 1
  else void refreshResults()
}, { deep: true, flush: 'post' })
watch(page, () => { if (initialized) void refreshResults() })

function openModal(): void {
  error.value = ''
  saved.value = ''
  isModalOpen.value = true
}

function closeModal(): void {
  if (!busy.value) isModalOpen.value = false
}

async function save(): Promise<void> {
  if (busy.value) return
  error.value = ''
  saved.value = ''
  if (!form.value.accountId || !form.value.categoryId) { error.value = t('finance.transactions.missingAccountOrCategory'); return }
  if (!Number.isFinite(form.value.amount) || form.value.amount < 0.01) { error.value = t('finance.transactions.invalidAmount'); return }

  busy.value = true
  try {
    await api.createTransaction(form.value)
  } catch (caughtError: unknown) {
    error.value = apiErrorMessage(caughtError, t('finance.transactions.saveError'))
    busy.value = false
    return
  }
  form.value.amount = 0
  form.value.description = ''
  isModalOpen.value = false
  pageMessage.value = t('finance.transactions.saved')
  await refreshPage()
  if (pageError.value) pageMessage.value = t('operations.refreshFailed')
  busy.value = false
}

watch(() => route.query.add, value => {
  if (value === '1') { openModal(); void router.replace('/transakcje') }
})

onMounted(() => {
  applyPreset()
  window.addEventListener('grosik:open-transaction', openModal)
  if (route.query.add === '1') { openModal(); void router.replace('/transakcje') }
  void refreshPage()
})

onBeforeUnmount(() => {
  window.removeEventListener('grosik:open-transaction', openModal)
})
</script>

<template>
  <div class="transactions-page">
    <section class="transaction-hero">
      <div><p class="section-kicker">{{ t('finance.transactions.kicker') }}</p><h2>{{ t('finance.transactions.title') }}</h2><p>{{ t('finance.transactions.subtitle') }}</p></div>
    </section>

    <p v-if="pageMessage" class="success" role="status">{{ pageMessage }}</p>
    <div v-if="pageError" class="notice" role="alert">{{ pageError }} <el-button @click="refreshPage">{{ t('operations.retry') }}</el-button></div>

    <TransactionFilters
      :accounts="accounts"
      :categories="categories"
      :filters="filters"
      :date-preset="datePreset"
      @clear="resetFilters"
      @update:date-preset="datePreset = $event"
      @preset-changed="applyPreset"
    />

    <section class="panel transaction-list">
      <div class="panel-head"><div><p class="section-kicker">{{ t('finance.transactions.historyKicker') }}</p><h3>{{ t('finance.transactions.listTitle') }}</h3></div><span class="count-badge">{{ t('finance.transactions.count', { count: total }) }}</span></div>
      <el-table v-loading="loading" :data="filteredItems" :empty-text="pageError ? t('finance.transactions.loadError') : t('finance.transactions.empty')" style="width:100%">
        <el-table-column :label="t('finance.transactions.description')" min-width="180"><template #default="{ row }">{{ row.description || operationLabel(row.kind) }}</template></el-table-column>
        <el-table-column :label="t('finance.transactions.kind')" min-width="160"><template #default="{ row }">{{ operationLabel(row.kind) }}</template></el-table-column>
        <el-table-column :label="t('finance.transactions.category')" min-width="150"><template #default="{ row }">{{ row.categoryName || t('finance.transactions.noCategory') }}</template></el-table-column>
        <el-table-column prop="accountName" :label="t('finance.transactions.account')" min-width="150" />
        <el-table-column prop="occurredOn" :label="t('finance.transactions.date')" min-width="120" />
        <el-table-column :label="t('finance.transactions.amount')" min-width="130" align="right"><template #default="{ row }"><strong :class="isDebit(row.kind) ? 'expense' : 'income'">{{ isDebit(row.kind) ? '−' : '+' }}{{ formatMoney(row.amount) }}</strong></template></el-table-column>
      </el-table>
      <el-pagination v-if="total > pageSize" v-model:current-page="page" :page-size="pageSize" :total="total" layout="prev, pager, next" />
    </section>
  </div>

  <el-dialog v-model="isModalOpen" :title="t('finance.transactions.modalTitle')" width="min(620px, 92vw)" :close-on-click-modal="!busy" :close-on-press-escape="!busy" :show-close="!busy" destroy-on-close>
        <section class="transaction-modal">
          <form novalidate @submit.prevent="save">
            <label>{{ t('finance.transactions.kind') }}<el-select v-model="form.kind" @change="form.categoryId = availableCategories[0]?.id || ''"><el-option value="expense" :label="t('finance.transactions.expense')" /><el-option value="income" :label="t('finance.transactions.income')" /></el-select></label>
            <div class="modal-two-columns"><label>{{ t('finance.transactions.account') }}<el-select v-model="form.accountId" filterable><el-option v-for="account in accounts" :key="account.id" :value="account.id" :label="account.name" /></el-select></label><label>{{ t('finance.transactions.category') }}<el-select v-model="form.categoryId" filterable><el-option v-for="category in availableCategories" :key="category.id" :value="category.id" :label="`${category.parentCategoryId ? '↳ ' : ''}${category.name}`" /></el-select></label></div>
            <div class="modal-two-columns"><label>{{ t('finance.transactions.amount') }}<el-input v-model.number="form.amount" step="0.01" type="number" inputmode="decimal" required /></label><label>{{ t('finance.transactions.date') }}<el-date-picker v-model="form.occurredOn" type="date" value-format="YYYY-MM-DD" format="DD/MM/YYYY" :clearable="false" style="width:100%" /></label></div>
            <label>{{ t('finance.transactions.description') }}<el-input v-model.trim="form.description" :placeholder="t('finance.transactions.descriptionPlaceholder')" /></label>
            <p v-if="error" class="notice">{{ error }}</p><p v-if="saved" class="success">{{ saved }}</p>
            <footer><el-button :disabled="busy" @click="closeModal">{{ t('finance.transactions.cancel') }}</el-button><el-button type="primary" native-type="submit" :loading="busy">{{ busy ? t('finance.transactions.saving') : t('finance.transactions.save') }}</el-button></footer>
          </form>
        </section>
  </el-dialog>
</template>

<style scoped>
.transactions-page{display:grid;gap:22px}.transaction-hero{display:flex;align-items:center;justify-content:space-between;gap:18px;padding:24px 26px;border:1px solid #e1e8fa;border-radius:20px;background:linear-gradient(135deg,#fff,#f5f8ff);box-shadow:0 12px 28px #1038ab0a}.section-kicker{margin:0 0 6px;color:#4d7cfe;font-size:11px;font-weight:800;letter-spacing:.12em}.transaction-hero h2,.panel h3,.transaction-modal h2{margin:0;color:#172a4d}.transaction-hero p:not(.section-kicker){margin:7px 0 0;color:#71809e;font-size:14px}.add-transaction,.primary-button{border:0;border-radius:12px;background:#1038ab;color:#fff;font:700 14px inherit;padding:13px 18px;box-shadow:0 9px 18px #1038ab2e;cursor:pointer;white-space:nowrap}.panel{border-radius:18px}.transaction-list{display:grid;gap:3px;padding:22px}.panel-head{display:flex;justify-content:space-between;gap:16px;align-items:flex-start}.count-badge{border-radius:99px;padding:7px 10px;background:#edf2fc;color:#1038ab;font-size:12px;font-weight:700}.transaction-item{display:grid;grid-template-columns:38px minmax(0,1fr) auto;align-items:center;gap:12px;padding:15px 0;border-bottom:1px solid #edf1fb}.transaction-item:last-of-type{border-bottom:0}.transaction-item>div{min-width:0}.transaction-item>div strong,.transaction-item small{display:block}.transaction-item small{margin-top:4px;color:#7b88a2;font-size:12px}.transaction-icon{display:grid;place-items:center;width:34px;height:34px;border-radius:11px;background:#e0eaff;color:#1038ab;font-weight:800}.transaction-icon.expense{background:#ffe8e3;color:#e55b48}.income{color:#0e8a63}.expense{color:#e55b48}.empty{margin:18px 0;color:#71809e}.modal-backdrop{position:fixed;z-index:20;inset:0;display:grid;place-items:center;padding:20px;background:#071b3bb8;backdrop-filter:blur(5px)}.transaction-modal{width:min(100%,620px);max-height:calc(100vh - 40px);overflow:auto;padding:28px;border-radius:22px;background:#fff;box-shadow:0 28px 70px #00143866}.transaction-modal header,.transaction-modal footer{display:flex;justify-content:space-between;align-items:center;gap:16px}.close-modal{display:grid;place-items:center;width:36px;height:36px;border:0;border-radius:10px;background:#edf2fc;color:#172a4d;font-size:26px;line-height:1;cursor:pointer}.transaction-modal form{display:grid;gap:15px;margin-top:23px}.transaction-modal input,.transaction-modal select{width:100%}.modal-two-columns{display:grid;grid-template-columns:1fr 1fr;gap:14px}.transaction-modal footer{justify-content:flex-end;margin-top:8px}.secondary-button{border:1px solid #d6e0f5;border-radius:12px;background:#fff;color:#41516f;font:700 14px inherit;padding:12px 16px;cursor:pointer}.modal-enter-active,.modal-leave-active{transition:opacity .18s ease}.modal-enter-active .transaction-modal,.modal-leave-active .transaction-modal{transition:transform .18s ease}.modal-enter-from,.modal-leave-to{opacity:0}.modal-enter-from .transaction-modal,.modal-leave-to .transaction-modal{transform:translateY(14px) scale(.98)}@media(max-width:800px){.transaction-hero{align-items:flex-start;flex-direction:column;padding:21px}.transaction-hero h2{font-size:25px}.add-transaction{width:100%}.transaction-list{padding:18px}.modal-two-columns{grid-template-columns:1fr}.transaction-modal{padding:22px 18px}.transaction-modal footer{display:grid;grid-template-columns:1fr 1fr}.transaction-modal footer button{width:100%}}@media(max-width:440px){.transaction-item{grid-template-columns:34px minmax(0,1fr);gap:10px}.transaction-item>strong{grid-column:2}.count-badge{font-size:11px}.modal-backdrop{padding:12px}.transaction-modal{max-height:calc(100vh - 24px)}}
</style>

<style scoped>
.transaction-modal{width:100%;max-height:none;overflow:visible;padding:0;border-radius:0;box-shadow:none;background:transparent}
.transaction-modal form{margin-top:0}
</style>

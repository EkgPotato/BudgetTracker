<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import TransactionAnalytics from '@/components/transactions/TransactionAnalytics.vue'
import TransactionFilters from '@/components/transactions/TransactionFilters.vue'
import { api, apiErrorMessage } from '@/api'
import { useTransactions } from '@/composables/useTransactions'
import { localDateInputValue } from '@/date'
import { formatMoney } from '@/money'
import type { TransactionKind } from '@/types'

const route = useRoute()
const router = useRouter()
const dialogRef = ref<HTMLElement | null>(null)
const opener = ref<HTMLElement | null>(null)
const isModalOpen = ref(false)
const busy = ref(false)
const error = ref('')
const saved = ref('')
const closeTimer = ref<number | undefined>()
const form = ref({ accountId: '', categoryId: '', kind: 'expense' as TransactionKind, amount: 0, occurredOn: localDateInputValue(), description: '' })

const {
  accounts,
  categories,
  filters,
  datePreset,
  filteredItems,
  incomeCents,
  expenseCents,
  balanceCents,
  categoryBreakdown,
  maxCategoryCents,
  load,
  applyPreset,
  resetFilters,
} = useTransactions()

const availableCategories = computed(() => categories.value.filter(category => category.appliesTo === form.value.kind))

async function loadPage(): Promise<void> {
  await load()
  form.value.accountId ||= accounts.value[0]?.id ?? ''
  form.value.categoryId ||= availableCategories.value[0]?.id ?? ''
}

function openModal(event?: Event): void {
  opener.value = event?.currentTarget instanceof HTMLElement ? event.currentTarget : document.activeElement instanceof HTMLElement ? document.activeElement : null
  error.value = ''
  saved.value = ''
  isModalOpen.value = true
}

function closeModal(): void {
  if (!busy.value) isModalOpen.value = false
}

async function save(): Promise<void> {
  error.value = ''
  saved.value = ''
  if (!form.value.accountId || !form.value.categoryId) { error.value = 'Wybierz konto i kategorię.'; return }
  if (!Number.isFinite(form.value.amount) || form.value.amount < 0.01) { error.value = 'Kwota musi być większa od 0,00 zł.'; return }

  busy.value = true
  try {
    await api.createTransaction(form.value)
    form.value.amount = 0
    form.value.description = ''
    await loadPage()
    saved.value = 'Zapisano transakcję.'
    closeTimer.value = window.setTimeout(() => { isModalOpen.value = false }, 650)
  } catch (caughtError: unknown) {
    error.value = apiErrorMessage(caughtError, 'Nie udało się zapisać transakcji.')
  } finally {
    busy.value = false
  }
}

function onDialogKeydown(event: KeyboardEvent): void {
  if (event.key === 'Escape') { event.preventDefault(); closeModal(); return }
  if (event.key !== 'Tab' || !dialogRef.value) return
  const focusable = [...dialogRef.value.querySelectorAll<HTMLElement>('button:not([disabled]), input:not([disabled]), select:not([disabled]), [href], [tabindex]:not([tabindex="-1"])')]
  if (!focusable.length) return
  const first = focusable[0]
  const last = focusable[focusable.length - 1]
  if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus() }
  if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus() }
}

watch(isModalOpen, async open => {
  if (open) {
    await nextTick()
    dialogRef.value?.querySelector<HTMLElement>('[data-autofocus]')?.focus()
    return
  }
  opener.value?.focus()
})

onMounted(() => {
  applyPreset()
  window.addEventListener('grosik:open-transaction', openModal)
  if (route.query.add === '1') { openModal(); void router.replace('/transakcje') }
  void loadPage().catch(caughtError => { error.value = apiErrorMessage(caughtError, 'Nie udało się pobrać danych.') })
})

onBeforeUnmount(() => {
  window.removeEventListener('grosik:open-transaction', openModal)
  if (closeTimer.value) window.clearTimeout(closeTimer.value)
})
</script>

<template>
  <div class="transactions-page">
    <section class="transaction-hero">
      <div><p class="section-kicker">PRZEGLĄD PRZEPŁYWÓW</p><h2>Twoje transakcje</h2><p>Filtruj historię i kontroluj, na co przeznaczasz budżet.</p></div>
      <button class="add-transaction" type="button" @click="openModal">＋ Dodaj transakcję</button>
    </section>

    <TransactionAnalytics
      :items="filteredItems"
      :income-cents="incomeCents"
      :expense-cents="expenseCents"
      :balance-cents="balanceCents"
      :category-breakdown="categoryBreakdown"
      :max-category-cents="maxCategoryCents"
      :categories="categories"
    />

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
      <div class="panel-head"><div><p class="section-kicker">HISTORIA</p><h3>Lista transakcji</h3></div><span class="count-badge">{{ filteredItems.length }} wpisów</span></div>
      <div v-for="transaction in filteredItems" :key="transaction.id" class="transaction-item">
        <span class="transaction-icon" :class="transaction.kind">{{ transaction.kind === 'expense' ? '↙' : '↗' }}</span>
        <div><strong>{{ transaction.description || transaction.categoryName || 'Bez opisu' }}</strong><small>{{ transaction.categoryName || 'Bez kategorii' }} · {{ transaction.accountName }} · {{ transaction.occurredOn }}</small></div>
        <strong :class="transaction.kind === 'expense' ? 'expense' : 'income'">{{ transaction.kind === 'expense' ? '−' : '+' }}{{ formatMoney(transaction.amount) }}</strong>
      </div>
      <p v-if="!filteredItems.length" class="empty">Brak transakcji dla wybranych filtrów.</p>
    </section>
  </div>

  <Teleport to="body">
    <Transition name="modal">
      <div v-if="isModalOpen" class="modal-backdrop" @mousedown.self="closeModal">
        <section ref="dialogRef" class="transaction-modal" role="dialog" aria-modal="true" aria-labelledby="transaction-modal-title" @keydown="onDialogKeydown">
          <header><div><p class="section-kicker">NOWY WPIS</p><h2 id="transaction-modal-title">Dodaj transakcję</h2></div><button data-autofocus class="close-modal" type="button" aria-label="Zamknij" @click="closeModal">×</button></header>
          <form novalidate @submit.prevent="save">
            <label>Rodzaj<select v-model="form.kind" @change="form.categoryId = availableCategories[0]?.id || ''"><option value="expense">Wydatek</option><option value="income">Przychód</option></select></label>
            <div class="modal-two-columns"><label>Konto<select v-model="form.accountId"><option v-for="account in accounts" :key="account.id" :value="account.id">{{ account.name }}</option></select></label><label>Kategoria<select v-model="form.categoryId"><option v-for="category in availableCategories" :key="category.id" :value="category.id">{{ category.parentCategoryId ? '↳ ' : '' }}{{ category.name }}</option></select></label></div>
            <div class="modal-two-columns"><label>Kwota<input v-model.number="form.amount" step="0.01" type="number" inputmode="decimal" required></label><label>Data<input v-model="form.occurredOn" type="date" required></label></div>
            <label>Opis<input v-model.trim="form.description" placeholder="np. Zakupy spożywcze"></label>
            <p v-if="error" class="notice">{{ error }}</p><p v-if="saved" class="success">{{ saved }}</p>
            <footer><button type="button" class="secondary-button" :disabled="busy" @click="closeModal">Anuluj</button><button class="primary-button" :disabled="busy">{{ busy ? 'Zapisuję…' : 'Zapisz transakcję' }}</button></footer>
          </form>
        </section>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
.transactions-page{display:grid;gap:22px}.transaction-hero{display:flex;align-items:center;justify-content:space-between;gap:18px;padding:24px 26px;border:1px solid #e1e8fa;border-radius:20px;background:linear-gradient(135deg,#fff,#f5f8ff);box-shadow:0 12px 28px #1038ab0a}.section-kicker{margin:0 0 6px;color:#4d7cfe;font-size:11px;font-weight:800;letter-spacing:.12em}.transaction-hero h2,.panel h3,.transaction-modal h2{margin:0;color:#172a4d}.transaction-hero p:not(.section-kicker){margin:7px 0 0;color:#71809e;font-size:14px}.add-transaction,.primary-button{border:0;border-radius:12px;background:#1038ab;color:#fff;font:700 14px inherit;padding:13px 18px;box-shadow:0 9px 18px #1038ab2e;cursor:pointer;white-space:nowrap}.panel{border-radius:18px}.transaction-list{display:grid;gap:3px;padding:22px}.panel-head{display:flex;justify-content:space-between;gap:16px;align-items:flex-start}.count-badge{border-radius:99px;padding:7px 10px;background:#edf2fc;color:#1038ab;font-size:12px;font-weight:700}.transaction-item{display:grid;grid-template-columns:38px minmax(0,1fr) auto;align-items:center;gap:12px;padding:15px 0;border-bottom:1px solid #edf1fb}.transaction-item:last-of-type{border-bottom:0}.transaction-item>div{min-width:0}.transaction-item>div strong,.transaction-item small{display:block}.transaction-item small{margin-top:4px;color:#7b88a2;font-size:12px}.transaction-icon{display:grid;place-items:center;width:34px;height:34px;border-radius:11px;background:#e0eaff;color:#1038ab;font-weight:800}.transaction-icon.expense{background:#ffe8e3;color:#e55b48}.income{color:#0e8a63}.expense{color:#e55b48}.empty{margin:18px 0;color:#71809e}.modal-backdrop{position:fixed;z-index:20;inset:0;display:grid;place-items:center;padding:20px;background:#071b3bb8;backdrop-filter:blur(5px)}.transaction-modal{width:min(100%,620px);max-height:calc(100vh - 40px);overflow:auto;padding:28px;border-radius:22px;background:#fff;box-shadow:0 28px 70px #00143866}.transaction-modal header,.transaction-modal footer{display:flex;justify-content:space-between;align-items:center;gap:16px}.close-modal{display:grid;place-items:center;width:36px;height:36px;border:0;border-radius:10px;background:#edf2fc;color:#172a4d;font-size:26px;line-height:1;cursor:pointer}.transaction-modal form{display:grid;gap:15px;margin-top:23px}.transaction-modal input,.transaction-modal select{width:100%}.modal-two-columns{display:grid;grid-template-columns:1fr 1fr;gap:14px}.transaction-modal footer{justify-content:flex-end;margin-top:8px}.secondary-button{border:1px solid #d6e0f5;border-radius:12px;background:#fff;color:#41516f;font:700 14px inherit;padding:12px 16px;cursor:pointer}.modal-enter-active,.modal-leave-active{transition:opacity .18s ease}.modal-enter-active .transaction-modal,.modal-leave-active .transaction-modal{transition:transform .18s ease}.modal-enter-from,.modal-leave-to{opacity:0}.modal-enter-from .transaction-modal,.modal-leave-to .transaction-modal{transform:translateY(14px) scale(.98)}@media(max-width:800px){.transaction-hero{align-items:flex-start;flex-direction:column;padding:21px}.transaction-hero h2{font-size:25px}.add-transaction{width:100%}.transaction-list{padding:18px}.modal-two-columns{grid-template-columns:1fr}.transaction-modal{padding:22px 18px}.transaction-modal footer{display:grid;grid-template-columns:1fr 1fr}.transaction-modal footer button{width:100%}}@media(max-width:440px){.transaction-item{grid-template-columns:34px minmax(0,1fr);gap:10px}.transaction-item>strong{grid-column:2}.count-badge{font-size:11px}.modal-backdrop{padding:12px}.transaction-modal{max-height:calc(100vh - 24px)}}
</style>

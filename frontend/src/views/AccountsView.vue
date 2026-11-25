<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { api, apiErrorMessage } from '@/api'
import { formatMoney } from '@/money'
import { accountTypeLabel, type Account, type AccountType, type BudgetSpace } from '@/types'

const accounts = ref<Account[]>([])
const spaces = ref<BudgetSpace[]>([])
const error = ref('')
const saved = ref('')
const loading = ref(true)
const busy = ref(false)
const form = ref<{ name: string; budgetSpaceId: string; type: AccountType; openingBalance: number }>({ name: '', budgetSpaceId: '', type: 'bank', openingBalance: 0 })

async function load(): Promise<void> {
  loading.value = true
  error.value = ''
  try {
    [accounts.value, spaces.value] = await Promise.all([api.accounts(), api.spaces()])
    form.value.budgetSpaceId ||= spaces.value[0]?.id ?? ''
  } catch (caughtError: unknown) {
    error.value = apiErrorMessage(caughtError, 'Nie udało się pobrać kont.')
  } finally {
    loading.value = false
  }
}

async function save(): Promise<void> {
  error.value = ''
  saved.value = ''
  if (!form.value.budgetSpaceId) { error.value = 'Wybierz portfel dla nowego konta.'; return }
  if (!Number.isFinite(form.value.openingBalance)) { error.value = 'Podaj poprawne saldo otwarcia.'; return }

  busy.value = true
  try {
    await api.createAccount(form.value)
    saved.value = 'Konto zostało utworzone.'
    form.value = { name: '', budgetSpaceId: form.value.budgetSpaceId, type: 'bank', openingBalance: 0 }
    await load()
  } catch (caughtError: unknown) {
    error.value = apiErrorMessage(caughtError, 'Nie udało się utworzyć konta.')
  } finally {
    busy.value = false
  }
}

onMounted(() => { void load() })
</script>

<template>
  <div class="grid two wide-first">
    <section class="panel" aria-live="polite">
      <div class="panel-title"><h2>Aktywne konta</h2><span>{{ accounts.length }} kont</span></div>
      <p v-if="loading" class="muted">Ładowanie kont…</p>
      <template v-else>
        <div v-for="account in accounts" :key="account.id" class="account-row"><span class="account-icon">▣</span><span>{{ account.name }}<small>{{ accountTypeLabel(account.type) }}</small></span><strong>{{ formatMoney(account.balance) }}</strong></div>
        <p v-if="!accounts.length" class="empty">Nie masz jeszcze kont.</p>
      </template>
    </section>

    <section class="panel"><h2>Nowe konto</h2><form novalidate @submit.prevent="save">
      <label>Nazwa<input v-model.trim="form.name" required placeholder="np. Konto osobiste"></label>
      <label>Portfel<select v-model="form.budgetSpaceId" required><option v-for="space in spaces" :key="space.id" :value="space.id">{{ space.name }}</option></select></label>
      <label>Typ<select v-model="form.type"><option value="cash">Gotówkowe</option><option value="bank">Bankowe</option><option value="savings">Oszczędnościowe</option><option value="deposit">Lokata</option><option value="investment">Inwestycyjne</option><option value="liability">Zobowiązanie</option></select></label>
      <label>Saldo otwarcia<input v-model.number="form.openingBalance" type="number" step="0.01"></label>
      <p v-if="error" class="notice">{{ error }}</p><p v-if="saved" class="success">{{ saved }}</p>
      <button class="button primary" :disabled="busy || loading">{{ busy ? 'Zapisuję…' : 'Dodaj konto' }}</button>
    </form></section>
  </div>
</template>

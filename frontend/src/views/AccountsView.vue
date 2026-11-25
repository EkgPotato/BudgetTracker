<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { api, apiErrorMessage } from '@/api'
import { t } from '@/i18n'
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
    error.value = apiErrorMessage(caughtError, t('finance.accounts.loadError'))
  } finally {
    loading.value = false
  }
}

async function save(): Promise<void> {
  error.value = ''
  saved.value = ''
  if (!form.value.budgetSpaceId) { error.value = t('finance.accounts.selectBudgetSpace'); return }
  if (!Number.isFinite(form.value.openingBalance)) { error.value = t('finance.accounts.invalidOpeningBalance'); return }

  busy.value = true
  try {
    await api.createAccount(form.value)
    saved.value = t('finance.accounts.created')
    form.value = { name: '', budgetSpaceId: form.value.budgetSpaceId, type: 'bank', openingBalance: 0 }
    await load()
  } catch (caughtError: unknown) {
    error.value = apiErrorMessage(caughtError, t('finance.accounts.createError'))
  } finally {
    busy.value = false
  }
}

onMounted(() => { void load() })
</script>

<template>
  <div class="grid two wide-first">
    <section class="panel" aria-live="polite">
      <div class="panel-title"><h2>{{ t('finance.accounts.activeAccounts') }}</h2><span>{{ t('finance.accounts.count', { count: accounts.length }) }}</span></div>
      <p v-if="loading" class="muted">{{ t('finance.accounts.loading') }}</p>
      <template v-else>
        <div v-for="account in accounts" :key="account.id" class="account-row"><span class="account-icon">▣</span><span>{{ account.name }}<small>{{ accountTypeLabel(account.type) }}</small></span><strong>{{ formatMoney(account.balance) }}</strong></div>
        <p v-if="!accounts.length" class="empty">{{ t('finance.accounts.empty') }}</p>
      </template>
    </section>

    <section class="panel"><h2>{{ t('finance.accounts.newAccount') }}</h2><form novalidate @submit.prevent="save">
      <label>{{ t('finance.accounts.name') }}<input v-model.trim="form.name" required :placeholder="t('finance.accounts.namePlaceholder')"></label>
      <label>{{ t('finance.accounts.budgetSpace') }}<select v-model="form.budgetSpaceId" required><option v-for="space in spaces" :key="space.id" :value="space.id">{{ space.name }}</option></select></label>
      <label>{{ t('finance.accounts.type') }}<select v-model="form.type"><option value="cash">{{ t('finance.accountTypes.cash') }}</option><option value="bank">{{ t('finance.accountTypes.bank') }}</option><option value="savings">{{ t('finance.accountTypes.savings') }}</option><option value="deposit">{{ t('finance.accountTypes.deposit') }}</option><option value="investment">{{ t('finance.accountTypes.investment') }}</option><option value="liability">{{ t('finance.accountTypes.liability') }}</option></select></label>
      <label>{{ t('finance.accounts.openingBalance') }}<input v-model.number="form.openingBalance" type="number" step="0.01"></label>
      <p v-if="error" class="notice">{{ error }}</p><p v-if="saved" class="success">{{ saved }}</p>
      <button class="button primary" :disabled="busy || loading">{{ busy ? t('finance.accounts.saving') : t('finance.accounts.add') }}</button>
    </form></section>
  </div>
</template>

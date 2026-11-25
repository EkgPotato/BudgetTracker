<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { api, apiErrorMessage } from '@/api'
import { t } from '@/i18n'
import { formatMoney } from '@/money'
import { accountTypeLabel, type Account, type AccountType, type BudgetSpace } from '@/types'
import TransferView from './TransferView.vue'

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
  if (busy.value) return
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
  <div class="accounts-page"><div class="grid two wide-first">
    <section class="panel" aria-live="polite">
      <div class="panel-title"><h2>{{ t('finance.accounts.activeAccounts') }}</h2><span>{{ t('finance.accounts.count', { count: accounts.length }) }}</span></div>
      <p v-if="loading" class="muted">{{ t('finance.accounts.loading') }}</p>
      <template v-else>
        <div v-for="account in accounts" :key="account.id" class="account-row"><span class="account-icon">▣</span><span>{{ account.name }}<small>{{ accountTypeLabel(account.type) }}</small></span><strong>{{ formatMoney(account.balance) }}</strong></div>
        <p v-if="!accounts.length && !error" class="empty">{{ t('finance.accounts.empty') }}</p>
        <div v-if="error" class="notice" role="alert">{{ error }} <el-button @click="load">{{ t('operations.retry') }}</el-button></div>
      </template>
    </section>

    <section class="panel"><h2>{{ t('finance.accounts.newAccount') }}</h2><form novalidate @submit.prevent="save">
      <label>{{ t('finance.accounts.name') }}<el-input v-model.trim="form.name" required :placeholder="t('finance.accounts.namePlaceholder')" /></label>
      <label>{{ t('finance.accounts.budgetSpace') }}<el-select v-model="form.budgetSpaceId" filterable><el-option v-for="space in spaces" :key="space.id" :value="space.id" :label="space.name" /></el-select></label>
      <label>{{ t('finance.accounts.type') }}<el-select v-model="form.type"><el-option v-for="kind in (['cash', 'bank', 'savings', 'deposit', 'investment', 'liability'] as const)" :key="kind" :value="kind" :label="t(`finance.accountTypes.${kind}`)" /></el-select></label>
      <label>{{ t('finance.accounts.openingBalance') }}<el-input v-model.number="form.openingBalance" type="number" step="0.01" /></label>
      <p v-if="error" class="notice">{{ error }}</p><p v-if="saved" class="success">{{ saved }}</p>
      <el-button type="primary" native-type="submit" :disabled="busy || loading">{{ busy ? t('finance.accounts.saving') : t('finance.accounts.add') }}</el-button>
    </form></section>
  </div><TransferView :key="accounts.map(account => account.id).join(',')" @transferred="load" /></div>
</template>

<style scoped>.accounts-page{display:grid;gap:24px}</style>

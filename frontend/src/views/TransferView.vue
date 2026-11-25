<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { api, apiErrorMessage } from '@/api'
import { localDateInputValue } from '@/date'
import { t } from '@/i18n'
import type { Account } from '@/types'

const accounts = ref<Account[]>([])
const done = ref('')
const error = ref('')
const busy = ref(false)
const form = ref({ sourceAccountId: '', destinationAccountId: '', amount: 0, occurredOn: localDateInputValue(), description: '' })

async function load(): Promise<void> {
  accounts.value = await api.accounts()
  form.value.sourceAccountId ||= accounts.value[0]?.id ?? ''
  form.value.destinationAccountId ||= accounts.value.find(account => account.id !== form.value.sourceAccountId)?.id ?? ''
}

async function save(): Promise<void> {
  error.value = ''
  done.value = ''
  if (form.value.sourceAccountId === form.value.destinationAccountId) { error.value = t('finance.transfer.sameAccounts'); return }
  if (!form.value.sourceAccountId || !form.value.destinationAccountId) { error.value = t('finance.transfer.missingAccounts'); return }
  if (!Number.isFinite(form.value.amount) || form.value.amount < 0.01) { error.value = t('finance.transfer.invalidAmount'); return }
  busy.value = true
  try { await api.createTransfer(form.value); done.value = t('finance.transfer.created'); form.value.amount = 0; await load() } catch (caughtError: unknown) { error.value = apiErrorMessage(caughtError, t('finance.transfer.createError')) } finally { busy.value = false }
}

onMounted(() => { void load().catch(caughtError => { error.value = apiErrorMessage(caughtError, t('finance.transfer.loadError')) }) })
</script>

<template>
  <section class="transfer-layout">
    <aside class="transfer-intro">
      <span class="transfer-kicker">{{ t('finance.transfer.kicker') }}</span>
      <h2>{{ t('finance.transfer.title') }}</h2>
      <p>{{ t('finance.transfer.subtitle') }}</p>
      <div class="transfer-note">
        <span aria-hidden="true">↔</span>
        <p>{{ t('finance.transfer.note') }}</p>
      </div>
    </aside>

    <section class="transfer-card" aria-labelledby="transfer-form-title">
      <div class="transfer-card-header">
        <div>
          <span class="section-eyebrow">{{ t('finance.transfer.formKicker') }}</span>
          <h2 id="transfer-form-title">{{ t('finance.transfer.formTitle') }}</h2>
        </div>
        <span class="transfer-mark" aria-hidden="true">⇄</span>
      </div>

      <form novalidate @submit.prevent="save">
        <div class="account-pair">
          <label>
            <span>{{ t('finance.transfer.fromAccount') }}</span>
            <select v-model="form.sourceAccountId" required>
              <option v-for="account in accounts" :key="account.id" :value="account.id">{{ account.name }}</option>
            </select>
          </label>
          <span class="transfer-arrow" aria-hidden="true">→</span>
          <label>
            <span>{{ t('finance.transfer.toAccount') }}</span>
            <select v-model="form.destinationAccountId" required>
              <option v-for="account in accounts" :key="account.id" :value="account.id" :disabled="account.id === form.sourceAccountId">{{ account.name }}</option>
            </select>
          </label>
        </div>

        <div class="transfer-details">
          <label>
            <span>{{ t('finance.transfer.amount') }}</span>
            <div class="amount-input"><input v-model.number="form.amount" type="number" step="0.01" required><b>{{ t('finance.transfer.currency') }}</b></div>
          </label>
          <label><span>{{ t('finance.transfer.date') }}</span><input v-model="form.occurredOn" type="date" required></label>
        </div>

        <label class="description-field"><span>{{ t('finance.transfer.description') }} <em>{{ t('finance.transfer.optional') }}</em></span><input v-model.trim="form.description" :placeholder="t('finance.transfer.descriptionPlaceholder')"></label>
        <p v-if="error" class="notice">{{ error }}</p>
        <div class="transfer-actions">
          <button class="button primary transfer-submit" :disabled="busy || accounts.length < 2">{{ busy ? t('finance.transfer.saving') : t('finance.transfer.submit') }}</button>
          <p v-if="done" class="success">{{ done }}</p>
        </div>
      </form>
    </section>
  </section>
</template>

<style scoped>
.transfer-layout { display:grid; grid-template-columns:minmax(220px,.6fr) minmax(0,1.2fr); gap:28px; max-width:980px; align-items:start; }
.transfer-intro { padding:28px 8px 12px; }
.transfer-kicker,.section-eyebrow { display:block; color:#4d7cfe; font-size:11px; font-weight:800; letter-spacing:.12em; }
.transfer-intro h2,.transfer-card h2 { color:#172a4d; font-size:25px; line-height:1.14; margin:10px 0; }
.transfer-intro>p { color:#71809e; font-size:14px; line-height:1.65; margin:0; }
.transfer-note { display:flex; gap:10px; margin-top:28px; padding:14px; border-radius:13px; background:#eaf1ff; color:#425a85; }
.transfer-note span { display:grid; flex:0 0 30px; place-items:center; width:30px; height:30px; border-radius:9px; background:#fff; color:#1747ba; font-weight:800; }
.transfer-note p { margin:0; font-size:12px; line-height:1.55; }
.transfer-card { padding:28px; background:#fff; border:1px solid #e1e8fa; border-radius:18px; box-shadow:0 12px 34px #1038ab12; }
.transfer-card-header { display:flex; justify-content:space-between; gap:16px; align-items:flex-start; padding-bottom:22px; border-bottom:1px solid #edf1fb; }
.transfer-card h2 { font-size:20px; margin:7px 0 0; }
.transfer-mark { display:grid; place-items:center; flex:0 0 42px; width:42px; height:42px; border-radius:13px; background:#1038ab; color:#fff; font-size:20px; }
.transfer-card form { margin-top:24px; gap:20px; }
.account-pair { display:grid; grid-template-columns:minmax(0,1fr) 34px minmax(0,1fr); gap:12px; align-items:end; }
.account-pair label,.transfer-details label,.description-field { display:grid; gap:7px; color:#42536f; font-size:12px; font-weight:700; }
.transfer-arrow { display:grid; place-items:center; width:34px; height:40px; margin-bottom:1px; border-radius:11px; background:#edf3ff; color:#1747ba; font-size:18px; font-weight:800; }
.transfer-details { display:grid; grid-template-columns:minmax(0,.85fr) minmax(0,1.15fr); gap:16px; }
.amount-input { position:relative; }
.amount-input input { width:100%; padding-right:36px; font-size:16px; font-weight:700; }
.amount-input b { position:absolute; top:50%; right:13px; transform:translateY(-50%); color:#71809e; font-size:13px; }
.description-field em { color:#92a0b8; font-style:normal; font-weight:500; }
.transfer-actions { display:flex; align-items:center; gap:16px; margin-top:2px; }
.transfer-submit { min-width:174px; padding-inline:20px; }
.transfer-actions .success { margin:0; font-size:13px; }
@media (max-width:800px) { .transfer-layout { grid-template-columns:1fr; gap:4px; } .transfer-intro { padding:0 4px 20px; } .transfer-intro h2 { font-size:22px; } .transfer-note { margin-top:18px; } .transfer-card { padding:22px 18px; } }
@media (max-width:480px) { .account-pair,.transfer-details { grid-template-columns:1fr; gap:14px; } .transfer-arrow { transform:rotate(90deg); margin:0 auto; } .transfer-actions { align-items:stretch; flex-direction:column; } .transfer-submit { width:100%; } }
</style>

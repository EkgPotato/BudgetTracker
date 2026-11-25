<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { api, apiErrorMessage, session } from '@/api'
import type { AccountType } from '@/types'
import LanguageSelector from '@/components/LanguageSelector.vue'
import { t } from '@/i18n'

const router = useRouter()
const displayName = ref(session.user?.displayName ?? '')
const spaceName = ref('My budget')
const accountName = ref('Main account')
const type = ref<AccountType>('bank')
const openingBalance = ref(0)
const error = ref('')
const busy = ref(false)

async function finish() {
  error.value = ''
  busy.value = true
  try {
    const user = await api.completeOnboarding({
      displayName: displayName.value,
      budgetSpaceName: spaceName.value,
      accountName: accountName.value,
      accountType: type.value,
      openingBalance: openingBalance.value,
    })
    session.updateUser(user)
    await router.push('/')
  } catch (caughtError: unknown) {
    error.value = apiErrorMessage(caughtError, t('onboarding.saveFailed'))
  } finally { busy.value = false }
}
</script>
<template><div class="onboarding-shell"><section class="onboarding-card"><div class="onboarding-language"><LanguageSelector /></div><div class="auth-logo"><span>G</span><b>rosik</b></div><span class="step">{{ t('onboarding.step') }}</span><h1>{{ t('onboarding.title') }}</h1><p class="muted">{{ t('onboarding.description') }}</p><form @submit.prevent="finish"><label>{{ t('onboarding.displayName') }}<el-input v-model.trim="displayName" required :placeholder="t('onboarding.displayNamePlaceholder')" /></label><label>{{ t('onboarding.spaceName') }}<el-input v-model.trim="spaceName" required :placeholder="t('onboarding.spaceNamePlaceholder')" /></label><label>{{ t('onboarding.accountName') }}<el-input v-model.trim="accountName" required :placeholder="t('onboarding.accountNamePlaceholder')" /></label><div class="form-grid"><label>{{ t('onboarding.accountType') }}<el-select v-model="type"><el-option value="bank" :label="t('onboarding.bank')" /><el-option value="cash" :label="t('onboarding.cash')" /><el-option value="savings" :label="t('onboarding.savings')" /><el-option value="investment" :label="t('onboarding.investment')" /></el-select></label><label>{{ t('onboarding.openingBalance') }}<el-input-number v-model="openingBalance" :min="0" :step="0.01" :precision="2" controls-position="right" /></label></div><p v-if="error" class="notice">{{ error }}</p><el-button type="primary" native-type="submit" :loading="busy">{{ busy ? t('onboarding.saving') : t('onboarding.finish') }}</el-button></form></section></div></template>
<style scoped>.onboarding-shell{min-height:100vh;display:grid;place-items:center;padding:24px;background:#f4f7ff}.onboarding-card{position:relative;width:min(100%,560px);padding:36px;background:#fff;border:1px solid #e1e8fa;border-radius:18px;box-shadow:0 18px 48px #1038ab14}.onboarding-language{position:absolute;top:16px;right:16px}.auth-logo{color:#1038ab;font-size:28px;font-weight:700;margin-bottom:26px}.auth-logo span{display:inline-grid;place-items:center;width:30px;height:30px;border:4px solid #ffc833;border-radius:50%;font-size:18px;margin-right:2px}.step{font-size:11px;font-weight:700;letter-spacing:.12em;color:#4d7cfe}.onboarding-card h1{margin-top:9px}.form-grid{display:grid;grid-template-columns:1fr 1fr;gap:13px}.form-grid .el-input-number{width:100%}.onboarding-card form{margin-top:24px}.onboarding-card form>.el-button{justify-self:start}@media(max-width:500px){.onboarding-card{padding:25px}.form-grid{grid-template-columns:1fr}}</style>

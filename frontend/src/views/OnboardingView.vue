<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { api, apiErrorMessage, session } from '@/api'
import type { AccountType } from '@/types'

const router = useRouter()
const displayName = ref(session.user?.displayName ?? '')
const spaceName = ref('Mój budżet')
const accountName = ref('Konto główne')
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
    error.value = apiErrorMessage(caughtError, 'Nie udało się zapisać konfiguracji. Sprawdź dane i spróbuj ponownie.')
  } finally { busy.value = false }
}
</script>
<template><div class="onboarding-shell"><section class="onboarding-card"><div class="auth-logo"><span>G</span><b>rosik</b></div><span class="step">KROK 1 Z 1</span><h1>Zacznijmy od pierwszego konta</h1><p class="muted">To konto będzie punktem wyjścia do zarządzania wydatkami i przelewami.</p><form @submit.prevent="finish"><label>Jak mamy się do Ciebie zwracać?<input v-model.trim="displayName" required placeholder="np. Anna"/></label><label>Nazwa portfela<input v-model.trim="spaceName" required placeholder="np. Mój budżet"/></label><label>Nazwa pierwszego konta<input v-model.trim="accountName" required placeholder="np. Konto osobiste"/></label><div class="form-grid"><label>Typ konta<select v-model="type"><option value="bank">Bankowe</option><option value="cash">Gotówkowe</option><option value="savings">Oszczędnościowe</option><option value="investment">Inwestycyjne</option></select></label><label>Saldo początkowe<input v-model.number="openingBalance" type="number" min="0" step="0.01"/></label></div><p v-if="error" class="notice">{{ error }}</p><button class="button primary" :disabled="busy">{{ busy ? 'Zapisuję…' : 'Przejdź do Grosik' }}</button></form></section></div></template>
<style scoped>.onboarding-shell{min-height:100vh;display:grid;place-items:center;padding:24px;background:#f4f7ff}.onboarding-card{width:min(100%,560px);padding:36px;background:#fff;border:1px solid #e1e8fa;border-radius:18px;box-shadow:0 18px 48px #1038ab14}.auth-logo{color:#1038ab;font-size:28px;font-weight:700;margin-bottom:26px}.auth-logo span{display:inline-grid;place-items:center;width:30px;height:30px;border:4px solid #ffc833;border-radius:50%;font-size:18px;margin-right:2px}.step{font-size:11px;font-weight:700;letter-spacing:.12em;color:#4d7cfe}.onboarding-card h1{margin-top:9px}.form-grid{display:grid;grid-template-columns:1fr 1fr;gap:13px}.onboarding-card .button{width:100%;cursor:pointer}.onboarding-card form{margin-top:24px}@media(max-width:500px){.onboarding-card{padding:25px}.form-grid{grid-template-columns:1fr}}</style>

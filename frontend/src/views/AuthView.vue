<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ApiError, api, apiErrorMessage, session } from '@/api'
import LanguageSelector from '@/components/LanguageSelector.vue'
import { t } from '@/i18n'

const register = ref(false)
const email = ref('')
const confirmEmail = ref('')
const password = ref('')
const confirm = ref('')
const message = ref('')
const error = ref('')
const busy = ref(false)
const router = useRouter()
const route = useRoute()
const heading = computed(() => t(register.value ? 'auth.registerHeading' : 'auth.loginHeading'))

onMounted(async () => {
  const userId = String(route.query.confirmUserId ?? '')
  const token = String(route.query.confirmToken ?? '')
  if (!userId || !token) return
  try { await api.confirmEmail(userId, token); message.value = t('auth.confirmed') } catch (caughtError: unknown) { error.value = apiErrorMessage(caughtError, t('auth.confirmFailed')) }
})

function switchMode(value: boolean): void { register.value = value; error.value = ''; message.value = '' }

async function submit(): Promise<void> {
  error.value = ''
  message.value = ''
  if (register.value && email.value.toLocaleLowerCase() !== confirmEmail.value.toLocaleLowerCase()) { error.value = t('auth.emailMismatch'); return }
  if (register.value && password.value !== confirm.value) { error.value = t('auth.passwordMismatch'); return }
  busy.value = true
  try {
    if (register.value) { await api.register(email.value, password.value); message.value = t('auth.registrationSent') }
    else { const result = await api.login(email.value, password.value); session.save(result); await router.push(String(route.query.redirect ?? '/')) }
  } catch (caughtError: unknown) {
    error.value = caughtError instanceof ApiError && caughtError.status === 403 ? t('auth.emailUnconfirmed') : apiErrorMessage(caughtError, t('auth.operationFailed'))
  } finally { busy.value = false }
}
</script>

<template><div class="auth-shell"><section class="auth-card"><div class="auth-language"><LanguageSelector /></div><img src="/grosik-logo.png" alt="Grosik" style="display:block;width:190px;height:auto;margin:0 auto 28px;border-radius:8px"><div class="auth-tabs"><button :class="{active:!register}" @click="switchMode(false)">{{ t('auth.signIn') }}</button><button :class="{active:register}" @click="switchMode(true)">{{ t('auth.register') }}</button></div><Transition name="form" mode="out-in"><div :key="register ? 'register' : 'login'"><h1>{{heading}}</h1><p class="muted">{{ t(register ? 'auth.registerDescription' : 'auth.loginDescription') }}</p><form @submit.prevent="submit"><label>{{ t('auth.email') }}<input v-model.trim="email" type="email" required :placeholder="t('auth.emailPlaceholder')"></label><label v-if="register">{{ t('auth.confirmEmail') }}<input v-model.trim="confirmEmail" type="email" required :placeholder="t('auth.emailPlaceholder')"></label><label>{{ t('auth.password') }}<input v-model="password" type="password" minlength="8" required :placeholder="t('auth.passwordPlaceholder')"></label><label v-if="register">{{ t('auth.confirmPassword') }}<input v-model="confirm" type="password" required></label><p v-if="error" class="notice">{{error}}</p><p v-if="message" class="success">{{message}}</p><button class="button primary" :disabled="busy">{{ busy ? t('auth.pleaseWait') : t(register ? 'auth.register' : 'auth.signIn') }}</button></form></div></Transition></section></div></template>

<style scoped>.auth-shell{min-height:100vh;display:grid;place-items:center;background:radial-gradient(circle at 20% 20%,#1976e9,#052563 68%);padding:24px}.auth-card{position:relative;width:min(100%,420px);background:#fff;padding:32px;border-radius:18px;box-shadow:0 24px 60px #00143880}.auth-language{position:absolute;top:16px;right:16px}.auth-tabs{display:grid;grid-template-columns:1fr 1fr;border:1px solid #d9e4f7;border-radius:10px;overflow:hidden;margin-bottom:24px}.auth-tabs button{border:0;background:#fff;padding:10px;color:#52617c;cursor:pointer}.auth-tabs .active{background:#1038ab;color:#fff;font-weight:700}.auth-card h1{font-size:27px;margin-bottom:8px}.auth-card .button{width:100%;border:0;cursor:pointer}.auth-card label{display:grid;gap:6px;font-size:12px;font-weight:700;color:#43536f}.auth-card input{padding:12px;border:1px solid #d8e2f5;border-radius:9px;font:14px inherit}.auth-card form{display:grid;gap:14px;margin-top:22px}.notice{margin:0}.success{margin:0;color:#087b58;font-size:13px}.form-enter-active,.form-leave-active{transition:opacity .22s ease,transform .2s}.form-enter-from{opacity:0;transform:translateX(14px)}.form-leave-to{opacity:0;transform:translateX(-14px)}</style>

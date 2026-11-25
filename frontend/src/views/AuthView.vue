<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ApiError, api, apiErrorMessage, session } from '@/api'

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
const heading = computed(() => register.value ? 'Utwórz konto' : 'Zaloguj')

onMounted(async () => {
  const userId = String(route.query.confirmUserId ?? '')
  const token = String(route.query.confirmToken ?? '')
  if (!userId || !token) return
  try { await api.confirmEmail(userId, token); message.value = 'E-mail potwierdzony. Możesz się zalogować.' } catch (caughtError: unknown) { error.value = apiErrorMessage(caughtError, 'Nie udało się potwierdzić adresu e-mail.') }
})

function switchMode(value: boolean): void { register.value = value; error.value = ''; message.value = '' }

async function submit(): Promise<void> {
  error.value = ''
  message.value = ''
  if (register.value && email.value.toLocaleLowerCase() !== confirmEmail.value.toLocaleLowerCase()) { error.value = 'Adresy e-mail muszą być identyczne.'; return }
  if (register.value && password.value !== confirm.value) { error.value = 'Hasła muszą być identyczne.'; return }
  busy.value = true
  try {
    if (register.value) { await api.register(email.value, password.value); message.value = 'Sprawdź skrzynkę Mailpit i potwierdź adres e-mail.' }
    else { const result = await api.login(email.value, password.value); session.save(result); await router.push(String(route.query.redirect ?? '/')) }
  } catch (caughtError: unknown) {
    error.value = caughtError instanceof ApiError && caughtError.status === 403 ? 'Najpierw potwierdź adres e-mail.' : apiErrorMessage(caughtError, 'Nie udało się wykonać operacji.')
  } finally { busy.value = false }
}
</script>

<template><div class="auth-shell"><section class="auth-card"><img src="/grosik-logo.png" alt="Grosik" style="display:block;width:190px;height:auto;margin:0 auto 28px;border-radius:8px"><div class="auth-tabs"><button :class="{active:!register}" @click="switchMode(false)">Logowanie</button><button :class="{active:register}" @click="switchMode(true)">Rejestracja</button></div><Transition name="form" mode="out-in"><div :key="register ? 'register' : 'login'"><h1>{{heading}}</h1><p class="muted">{{register?'Wyślemy link potwierdzający na Twój e-mail.':'Zaloguj się do swojego konta.'}}</p><form @submit.prevent="submit"><label>Adres e-mail<input v-model.trim="email" type="email" required placeholder="ty@example.com"></label><label v-if="register">Powtórz adres e-mail<input v-model.trim="confirmEmail" type="email" required placeholder="ty@example.com"></label><label>Hasło<input v-model="password" type="password" minlength="8" required placeholder="Minimum 8 znaków"></label><label v-if="register">Powtórz hasło<input v-model="confirm" type="password" required></label><p v-if="error" class="notice">{{error}}</p><p v-if="message" class="success">{{message}}</p><button class="button primary" :disabled="busy">{{busy?'Chwileczkę…':register?'Załóż konto':'Zaloguj się'}}</button></form></div></Transition></section></div></template>

<style scoped>.auth-shell{min-height:100vh;display:grid;place-items:center;background:radial-gradient(circle at 20% 20%,#1976e9,#052563 68%);padding:24px}.auth-card{width:min(100%,420px);background:#fff;padding:32px;border-radius:18px;box-shadow:0 24px 60px #00143880}.auth-tabs{display:grid;grid-template-columns:1fr 1fr;border:1px solid #d9e4f7;border-radius:10px;overflow:hidden;margin-bottom:24px}.auth-tabs button{border:0;background:#fff;padding:10px;color:#52617c;cursor:pointer}.auth-tabs .active{background:#1038ab;color:#fff;font-weight:700}.auth-card h1{font-size:27px;margin-bottom:8px}.auth-card .button{width:100%;border:0;cursor:pointer}.auth-card label{display:grid;gap:6px;font-size:12px;font-weight:700;color:#43536f}.auth-card input{padding:12px;border:1px solid #d8e2f5;border-radius:9px;font:14px inherit}.auth-card form{display:grid;gap:14px;margin-top:22px}.notice{margin:0}.success{margin:0;color:#087b58;font-size:13px}.form-enter-active,.form-leave-active{transition:opacity .22s ease,transform .2s}.form-enter-from{opacity:0;transform:translateX(14px)}.form-leave-to{opacity:0;transform:translateX(-14px)}</style>

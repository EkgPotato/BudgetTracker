<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { api, apiErrorMessage, session } from '@/api'

const profile = reactive({ displayName: '', email: '' })
const emailForm = reactive({ newEmail: '', currentPassword: '' })
const passwordForm = reactive({ currentPassword: '', newPassword: '', confirmation: '' })
const status = ref('')
const error = ref('')
const busy = ref<'profile' | 'email' | 'password' | null>(null)

async function refresh(): Promise<void> {
  const user = await api.me()
  session.updateUser(user)
  profile.displayName = user.displayName || ''
  profile.email = user.email
  emailForm.newEmail = user.email
}

async function saveProfile(): Promise<void> {
  error.value = ''; status.value = ''; busy.value = 'profile'
  try {
    const user = await api.updateProfile(profile.displayName, Boolean(session.user?.onboardingCompleted))
    session.updateUser(user); status.value = 'Dane profilu zostały zapisane.'
  } catch (caught: unknown) { error.value = apiErrorMessage(caught, 'Nie udało się zapisać danych.') } finally { busy.value = null }
}

async function changeEmail(): Promise<void> {
  error.value = ''; status.value = ''; busy.value = 'email'
  try {
    const user = await api.changeEmail(emailForm)
    session.updateUser(user); profile.email = user.email; emailForm.currentPassword = ''
    status.value = 'Adres został zmieniony. Potwierdź go przez wiadomość e-mail, aby ponownie się zalogować.'
  } catch (caught: unknown) { error.value = apiErrorMessage(caught, 'Nie udało się zmienić adresu e-mail.') } finally { busy.value = null }
}

async function changePassword(): Promise<void> {
  error.value = ''; status.value = ''
  if (passwordForm.newPassword !== passwordForm.confirmation) { error.value = 'Nowe hasła muszą być takie same.'; return }
  busy.value = 'password'
  try {
    await api.changePassword({ currentPassword: passwordForm.currentPassword, newPassword: passwordForm.newPassword })
    passwordForm.currentPassword = ''; passwordForm.newPassword = ''; passwordForm.confirmation = ''
    status.value = 'Hasło zostało zmienione.'
  } catch (caught: unknown) { error.value = apiErrorMessage(caught, 'Nie udało się zmienić hasła.') } finally { busy.value = null }
}

onMounted(() => { void refresh().catch(caught => { error.value = apiErrorMessage(caught, 'Nie udało się pobrać profilu.') }) })
</script>

<template>
  <div class="profile-page">
    <section class="profile-hero"><p class="section-kicker">KONTO</p><h2>Ustawienia użytkownika</h2><p>Zarządzaj widocznymi danymi i zabezpieczeniami konta.</p></section>
    <p v-if="error" class="notice">{{ error }}</p><p v-if="status" class="success profile-status">{{ status }}</p>
    <div class="profile-grid">
      <section class="panel"><h2>Twoje dane</h2><p class="muted">Te informacje są widoczne tylko w Twojej aplikacji.</p><form @submit.prevent="saveProfile"><label>Imię i nazwisko<input v-model.trim="profile.displayName" required autocomplete="name"></label><label>Aktualny e-mail<input :value="profile.email" readonly aria-readonly="true"></label><button class="button primary" :disabled="busy !== null">{{ busy === 'profile' ? 'Zapisuję…' : 'Zapisz dane' }}</button></form></section>
      <section class="panel"><h2>Zmień e-mail</h2><p class="muted">Wymagamy bieżącego hasła i ponownego potwierdzenia nowego adresu.</p><form @submit.prevent="changeEmail"><label>Nowy adres e-mail<input v-model.trim="emailForm.newEmail" type="email" required autocomplete="email"></label><label>Bieżące hasło<input v-model="emailForm.currentPassword" type="password" required autocomplete="current-password"></label><button class="button primary" :disabled="busy !== null">{{ busy === 'email' ? 'Zmieniam…' : 'Zmień e-mail' }}</button></form></section>
      <section class="panel password-panel"><h2>Zmień hasło</h2><p class="muted">Użyj co najmniej ośmiu znaków i nie udostępniaj hasła nikomu.</p><form @submit.prevent="changePassword"><label>Bieżące hasło<input v-model="passwordForm.currentPassword" type="password" required autocomplete="current-password"></label><label>Nowe hasło<input v-model="passwordForm.newPassword" type="password" minlength="8" required autocomplete="new-password"></label><label>Powtórz nowe hasło<input v-model="passwordForm.confirmation" type="password" minlength="8" required autocomplete="new-password"></label><button class="button primary" :disabled="busy !== null">{{ busy === 'password' ? 'Zmieniam…' : 'Zmień hasło' }}</button></form></section>
    </div>
  </div>
</template>

<style scoped>
.profile-page{display:grid;gap:20px}.profile-hero{padding:25px 27px;border:1px solid #e1e8fa;border-radius:20px;background:linear-gradient(135deg,#fff,#f2f6ff);box-shadow:0 12px 28px #1038ab0a}.profile-hero h2{margin:0;color:#172a4d;font-size:25px}.profile-hero p:not(.section-kicker){margin:8px 0 0;color:#71809e}.section-kicker{margin:0 0 6px;color:#4d7cfe;font-size:11px;font-weight:800;letter-spacing:.12em}.profile-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:18px}.password-panel{grid-column:1/-1}.password-panel form{grid-template-columns:repeat(3,minmax(0,1fr));align-items:end}.password-panel button{width:max-content}.profile-status{margin:0}.button:disabled{opacity:.65;cursor:wait}@media(max-width:800px){.profile-grid,.password-panel form{grid-template-columns:1fr}.password-panel button{width:100%}.profile-hero{padding:21px}}
</style>

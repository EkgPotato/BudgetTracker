<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { api, apiErrorMessage, session } from '@/api'
import { t } from '@/i18n'
import { useRouter } from 'vue-router'

const profile = reactive({ displayName: '', email: '' })
const emailForm = reactive({ newEmail: '', currentPassword: '' })
const passwordForm = reactive({ currentPassword: '', newPassword: '', confirmation: '' })
const status = ref('')
const error = ref('')
const busy = ref<'profile' | 'email' | 'password' | null>(null)
const router = useRouter()

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
    session.updateUser(user); status.value = t('profile.saved')
  } catch (caught: unknown) { error.value = apiErrorMessage(caught, t('profile.saveFailed')) } finally { busy.value = null }
}

async function changeEmail(): Promise<void> {
  error.value = ''; status.value = ''; busy.value = 'email'
  try {
    const user = await api.changeEmail(emailForm)
    session.updateUser(user); profile.email = user.email; emailForm.currentPassword = ''
    session.clear()
    await router.replace({ path: '/auth', query: { reason: 'emailChanged' } })
  } catch (caught: unknown) { error.value = apiErrorMessage(caught, t('profile.emailChangeFailed')) } finally { busy.value = null }
}

async function changePassword(): Promise<void> {
  error.value = ''; status.value = ''
  if (passwordForm.newPassword !== passwordForm.confirmation) { error.value = t('profile.passwordMismatch'); return }
  busy.value = 'password'
  try {
    await api.changePassword({ currentPassword: passwordForm.currentPassword, newPassword: passwordForm.newPassword })
    passwordForm.currentPassword = ''; passwordForm.newPassword = ''; passwordForm.confirmation = ''
    session.clear()
    await router.replace({ path: '/auth', query: { reason: 'passwordChanged' } })
  } catch (caught: unknown) { error.value = apiErrorMessage(caught, t('profile.passwordChangeFailed')) } finally { busy.value = null }
}

onMounted(() => { void refresh().catch(caught => { error.value = apiErrorMessage(caught, t('profile.loadFailed')) }) })
</script>

<template>
  <div class="profile-page">
    <section class="profile-hero"><p class="section-kicker">{{ t('profile.kicker') }}</p><h2>{{ t('profile.title') }}</h2><p>{{ t('profile.description') }}</p></section>
    <p v-if="error" class="notice">{{ error }}</p><p v-if="status" class="success profile-status">{{ status }}</p>
    <div class="profile-grid">
      <section class="panel"><h2>{{ t('profile.detailsTitle') }}</h2><p class="muted">{{ t('profile.detailsDescription') }}</p><form @submit.prevent="saveProfile"><label>{{ t('profile.displayName') }}<el-input v-model.trim="profile.displayName" required autocomplete="name" /></label><label>{{ t('profile.currentEmail') }}<el-input :model-value="profile.email" readonly aria-readonly="true" /></label><el-button type="primary" native-type="submit" :disabled="busy !== null" :loading="busy === 'profile'">{{ busy === 'profile' ? t('profile.saving') : t('profile.save') }}</el-button></form></section>
      <section class="panel"><h2>{{ t('profile.emailTitle') }}</h2><p class="muted">{{ t('profile.emailDescription') }}</p><form @submit.prevent="changeEmail"><label>{{ t('profile.newEmail') }}<el-input v-model.trim="emailForm.newEmail" type="email" required autocomplete="email" /></label><label>{{ t('profile.currentPassword') }}<el-input v-model="emailForm.currentPassword" type="password" required autocomplete="current-password" show-password /></label><el-button type="primary" native-type="submit" :disabled="busy !== null" :loading="busy === 'email'">{{ busy === 'email' ? t('profile.changing') : t('profile.changeEmail') }}</el-button></form></section>
      <section class="panel password-panel"><h2>{{ t('profile.passwordTitle') }}</h2><p class="muted">{{ t('profile.passwordDescription') }}</p><form @submit.prevent="changePassword"><label>{{ t('profile.currentPassword') }}<el-input v-model="passwordForm.currentPassword" type="password" required autocomplete="current-password" show-password /></label><label>{{ t('profile.newPassword') }}<el-input v-model="passwordForm.newPassword" type="password" minlength="8" required autocomplete="new-password" show-password /></label><label>{{ t('profile.confirmNewPassword') }}<el-input v-model="passwordForm.confirmation" type="password" minlength="8" required autocomplete="new-password" show-password /></label><el-button type="primary" native-type="submit" :disabled="busy !== null" :loading="busy === 'password'">{{ busy === 'password' ? t('profile.changing') : t('profile.changePassword') }}</el-button></form></section>
    </div>
  </div>
</template>

<style scoped>
.profile-page{display:grid;gap:20px}.profile-hero{padding:25px 27px;border:1px solid #e1e8fa;border-radius:20px;background:linear-gradient(135deg,#fff,#f2f6ff);box-shadow:0 12px 28px #1038ab0a}.profile-hero h2{margin:0;color:#172a4d;font-size:25px}.profile-hero p:not(.section-kicker){margin:8px 0 0;color:#71809e}.section-kicker{margin:0 0 6px;color:#4d7cfe;font-size:11px;font-weight:800;letter-spacing:.12em}.profile-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:18px}.profile-grid form>.el-button{justify-self:start}.password-panel{grid-column:1/-1}.password-panel form{grid-template-columns:repeat(3,minmax(0,1fr));align-items:end}.profile-status{margin:0}@media(max-width:800px){.profile-grid,.password-panel form{grid-template-columns:1fr}.profile-hero{padding:21px}}
</style>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { ChevronLeft, CreditCard, FolderTree, House, LogOut, Plus, ReceiptText } from '@lucide/vue'
import { useRoute, useRouter } from 'vue-router'
import { api, session } from '@/api'
import LanguageSelector from '@/components/LanguageSelector.vue'
import ThemeToggle from '@/components/ThemeToggle.vue'
import { locale, t } from '@/i18n'
import en from 'element-plus/es/locale/lang/en'
import pl from 'element-plus/es/locale/lang/pl'

const route = useRoute()
const router = useRouter()
const title = computed(() => t(String(route.meta.titleKey ?? 'nav.overview')))
const isPublic = computed(() => Boolean(route.meta.public))
const user = computed(() => session.user)
const collapsed = ref(localStorage.getItem('grosik.sidebar-collapsed') === 'true')
function toggleSidebar(): void {
  collapsed.value = !collapsed.value
  localStorage.setItem('grosik.sidebar-collapsed', String(collapsed.value))
}
const links = [
  { to: '/', labelKey: 'nav.overview', icon: House },
  { to: '/transakcje', labelKey: 'nav.transactions', icon: ReceiptText },
  { to: '/konta', labelKey: 'nav.accounts', icon: CreditCard },
  { to: '/kategorie', labelKey: 'nav.categories', icon: FolderTree },
]

function logout(): void { session.clear(); router.push('/auth') }

onMounted(async () => {
  const token = session.token
  if (!token) return

  try {
    const user = await api.me()
    if (session.token === token) session.updateUser(user)
  } catch { /* Keep the current session if the server cannot be reached. */ }
})
</script>

<template>
  <el-config-provider :locale="locale === 'pl' ? pl : en">
  <RouterView v-if="isPublic" />
  <div v-else class="app-shell" :class="{ 'sidebar-collapsed': collapsed }">
    <aside id="main-sidebar" class="sidebar">
      <RouterLink class="brand" to="/" :aria-label="t('app.brandHome')">
        <span class="brand-mark">G</span><span class="brand-name">Grosik</span>
      </RouterLink>
      <button class="sidebar-toggle" type="button" :aria-label="t(collapsed ? 'nav.expand' : 'nav.collapse')" :title="t(collapsed ? 'nav.expand' : 'nav.collapse')" aria-controls="main-sidebar" :aria-expanded="!collapsed" @click="toggleSidebar"><ChevronLeft :size="16" aria-hidden="true" /></button>
      <p class="nav-label">{{ t('nav.finance') }}</p>
      <nav class="main-nav" :aria-label="t('app.mainNavigation')">
        <RouterLink v-for="link in links" :key="link.to" :to="link.to" :aria-label="t(link.labelKey)">
          <component :is="link.icon" :size="18" :stroke-width="2" aria-hidden="true" /><span>{{ t(link.labelKey) }}</span>
        </RouterLink>
      </nav>
      <div class="sidebar-account">
        <RouterLink class="avatar" to="/profil" :aria-label="t('app.openProfile')">{{ user?.email?.slice(0, 2).toUpperCase() || 'G' }}</RouterLink>
        <RouterLink class="account-details" to="/profil" :aria-label="t('app.openProfile')"><strong>{{ user?.displayName || user?.email || t('app.defaultUser') }}</strong><small>{{ user?.email || 'Grosik' }}</small></RouterLink>
        <button class="logout-button" type="button" :aria-label="t('app.logout')" @click="logout"><LogOut :size="15" /><span>{{ t('app.logout') }}</span></button>
      </div>
    </aside>
    <main>
      <header class="app-header">
        <h1>{{ title }}</h1>
        <div class="header-actions"><ThemeToggle /><LanguageSelector /><RouterLink class="button primary add-transaction-button" :to="{ path: '/transakcje', query: { add: '1' } }"><Plus :size="17" /><span>{{ t('app.addExpense') }}</span></RouterLink></div>
      </header>
      <RouterView />
    </main>
  </div>
  </el-config-provider>
</template>
<style scoped>
.sidebar{z-index:5;transition:width .25s ease,padding .25s ease}.app-shell main{transition:margin-left .25s ease}.sidebar-toggle{position:absolute;top:31px;right:-14px;display:grid;place-items:center;width:28px;height:28px;padding:0;border:1px solid #8296b5;border-radius:50%;background:#244a87;color:#fff;box-shadow:0 2px 8px #071a3e40;cursor:pointer;z-index:1;transition:background-color .2s ease,box-shadow .2s ease}.sidebar-toggle:hover{background:#3664aa;box-shadow:0 3px 12px #071a3e66}.sidebar-toggle:focus-visible{outline:3px solid #ffc941;outline-offset:3px}.sidebar-toggle svg{transition:transform .25s ease}.sidebar-collapsed .sidebar-toggle svg{transform:rotate(180deg)}.avatar{text-decoration:none}.sidebar-collapsed .sidebar{width:68px;padding:19px 8px 14px}.sidebar-collapsed .brand{padding:2px 7px 31px}.brand-name,.nav-label,.main-nav a span,.account-details,.logout-button span{white-space:nowrap;overflow:hidden;transition:opacity .15s ease}.sidebar-collapsed .brand-name,.sidebar-collapsed .nav-label,.sidebar-collapsed .main-nav a span,.sidebar-collapsed .account-details,.sidebar-collapsed .logout-button span{opacity:0;visibility:hidden;width:0;margin:0;padding:0}.sidebar-collapsed .main-nav a{justify-content:center;padding:11px;gap:0}.sidebar-collapsed .main-nav a svg{flex-shrink:0}.sidebar-collapsed .sidebar-account{display:block;padding:15px 7px 0;text-align:center}.sidebar-collapsed .avatar{margin:auto}.sidebar-collapsed .logout-button{width:36px;min-height:36px;margin-top:12px;gap:0}.sidebar-collapsed main{margin-left:68px}@media(max-width:800px){.sidebar-toggle{display:none}}@media(prefers-reduced-motion:reduce){.sidebar,.app-shell main,.sidebar-toggle,.sidebar-toggle svg,.brand-name,.nav-label,.main-nav a span,.account-details,.logout-button span{transition:none}}
</style>

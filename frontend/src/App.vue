<script setup lang="ts">
import { computed } from 'vue'
import { ArrowLeftRight, ChartNoAxesCombined, CreditCard, FolderTree, House, LogOut, Plus, ReceiptText } from '@lucide/vue'
import { useRoute, useRouter } from 'vue-router'
import { session } from '@/api'
import LanguageSelector from '@/components/LanguageSelector.vue'
import { t } from '@/i18n'

const route = useRoute()
const router = useRouter()
const title = computed(() => t(String(route.meta.titleKey ?? 'nav.overview')))
const isPublic = computed(() => Boolean(route.meta.public))
const user = computed(() => session.user)
const links = [
  { to: '/', labelKey: 'nav.overview', icon: House },
  { to: '/transakcje', labelKey: 'nav.transactions', icon: ReceiptText },
  { to: '/transfery', labelKey: 'nav.transfers', icon: ArrowLeftRight },
  { to: '/konta', labelKey: 'nav.accounts', icon: CreditCard },
  { to: '/kategorie', labelKey: 'nav.categories', icon: FolderTree },
  { to: '/raporty', labelKey: 'nav.reports', icon: ChartNoAxesCombined },
]

function logout(): void { session.clear(); router.push('/auth') }
</script>

<template>
  <RouterView v-if="isPublic" />
  <div v-else class="app-shell">
    <aside class="sidebar">
      <RouterLink class="brand" to="/" :aria-label="t('app.brandHome')">
        <span class="brand-mark">G</span><span class="brand-name">Grosik</span>
      </RouterLink>
      <p class="nav-label">{{ t('nav.finance') }}</p>
      <nav class="main-nav" :aria-label="t('app.mainNavigation')">
        <RouterLink v-for="link in links" :key="link.to" :to="link.to" :aria-label="t(link.labelKey)">
          <component :is="link.icon" :size="18" :stroke-width="2" aria-hidden="true" /><span>{{ t(link.labelKey) }}</span>
        </RouterLink>
      </nav>
      <div class="sidebar-account">
        <span class="avatar">{{ user?.email?.slice(0, 2).toUpperCase() || 'G' }}</span>
        <RouterLink class="account-details" to="/profil" :aria-label="t('app.openProfile')"><strong>{{ user?.displayName || user?.email || t('app.defaultUser') }}</strong><small>{{ user?.email || 'Grosik' }}</small></RouterLink>
        <button class="logout-button" type="button" :aria-label="t('app.logout')" @click="logout"><LogOut :size="15" /><span>{{ t('app.logout') }}</span></button>
      </div>
    </aside>
    <main>
      <header class="app-header">
        <h1>{{ title }}</h1>
        <div class="header-actions"><LanguageSelector /><RouterLink class="button primary add-transaction-button" :to="{ path: '/transakcje', query: { add: '1' } }"><Plus :size="17" /><span>{{ t('app.addExpense') }}</span></RouterLink></div>
      </header>
      <RouterView />
    </main>
  </div>
</template>

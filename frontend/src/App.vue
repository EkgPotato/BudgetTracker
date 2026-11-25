<script setup lang="ts">
import { computed } from 'vue'
import { ArrowLeftRight, ChartNoAxesCombined, CreditCard, FolderTree, House, LogOut, Plus, ReceiptText } from '@lucide/vue'
import { useRoute, useRouter } from 'vue-router'
import { session } from '@/api'

const route = useRoute()
const router = useRouter()
const title = computed(() => route.meta.title ?? 'Przegląd')
const isPublic = computed(() => Boolean(route.meta.public))
const user = computed(() => session.user)
const links = [
  { to: '/', label: 'Przegląd', icon: House },
  { to: '/transakcje', label: 'Transakcje', icon: ReceiptText },
  { to: '/transfery', label: 'Przelewy', icon: ArrowLeftRight },
  { to: '/konta', label: 'Konta', icon: CreditCard },
  { to: '/kategorie', label: 'Kategorie', icon: FolderTree },
  { to: '/raporty', label: 'Raporty', icon: ChartNoAxesCombined },
]

function logout(): void { session.clear(); router.push('/auth') }
</script>

<template>
  <RouterView v-if="isPublic" />
  <div v-else class="app-shell">
    <aside class="sidebar">
      <RouterLink class="brand" to="/" aria-label="Grosik — przejdź do przeglądu">
        <span class="brand-mark">G</span><span class="brand-name">Grosik</span>
      </RouterLink>
      <p class="nav-label">FINANSE</p>
      <nav class="main-nav" aria-label="Główna nawigacja">
        <RouterLink v-for="link in links" :key="link.to" :to="link.to" :aria-label="link.label">
          <component :is="link.icon" :size="18" :stroke-width="2" aria-hidden="true" /><span>{{ link.label }}</span>
        </RouterLink>
      </nav>
      <div class="sidebar-account">
        <span class="avatar">{{ user?.email?.slice(0, 2).toUpperCase() || 'G' }}</span>
        <RouterLink class="account-details" to="/profil" aria-label="Otwórz mój profil"><strong>{{ user?.displayName || user?.email || 'Użytkownik' }}</strong><small>{{ user?.email || 'Grosik' }}</small></RouterLink>
        <button class="logout-button" type="button" aria-label="Wyloguj" @click="logout"><LogOut :size="15" /><span>Wyloguj</span></button>
      </div>
    </aside>
    <main>
      <header class="app-header">
        <h1>{{ title }}</h1>
        <div class="header-actions"><RouterLink class="button primary add-transaction-button" :to="{ path: '/transakcje', query: { add: '1' } }"><Plus :size="17" /><span>Dodaj wydatek</span></RouterLink></div>
      </header>
      <RouterView />
    </main>
  </div>
</template>

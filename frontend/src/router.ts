import { createRouter, createWebHistory } from 'vue-router'
import DashboardView from './views/DashboardView.vue'
import TransactionsView from './views/TransactionsView.vue'
import AccountsView from './views/AccountsView.vue'
import CategoriesView from './views/CategoriesView.vue'
import TransferView from './views/TransferView.vue'
import ReportsView from './views/ReportsView.vue'
import AuthView from './views/AuthView.vue'
import OnboardingView from './views/OnboardingView.vue'
import ProfileView from './views/ProfileView.vue'
import { session } from './api'

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/auth', component: AuthView, meta: { public: true, title: 'Witaj w Grosik' } },
    { path: '/onboarding', component: OnboardingView, meta: { public: true, onboarding: true, title: 'Pierwsza konfiguracja' } },
    { path: '/', component: DashboardView, meta: { title: 'Przegląd' } },
    { path: '/transakcje', component: TransactionsView, meta: { title: 'Transakcje' } },
    { path: '/transfery', component: TransferView, meta: { title: 'Transfer środków' } },
    { path: '/konta', component: AccountsView, meta: { title: 'Konta' } },
    { path: '/kategorie', component: CategoriesView, meta: { title: 'Kategorie' } },
    { path: '/raporty', component: ReportsView, meta: { title: 'Raporty' } },
    { path: '/profil', component: ProfileView, meta: { title: 'Mój profil' } }
  ]
})
router.beforeEach((to) => {
  if (!to.meta.public && !session.token) return { path: '/auth', query: { redirect: to.fullPath } }
  if (to.meta.onboarding && !session.token) return '/auth'
  if (session.token && !session.user?.onboardingCompleted && !to.meta.onboarding) return '/onboarding'
  if (to.path === '/auth' && session.token) return session.user?.onboardingCompleted ? '/' : '/onboarding'
})

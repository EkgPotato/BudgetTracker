import { createRouter, createWebHistory } from 'vue-router'
import { onSessionExpired, session } from './api'

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/auth', component: () => import('./views/AuthView.vue'), meta: { public: true, titleKey: 'route.auth' } },
    { path: '/onboarding', component: () => import('./views/OnboardingView.vue'), meta: { public: true, onboarding: true, titleKey: 'route.onboarding' } },
    { path: '/', component: () => import('./views/DashboardView.vue'), meta: { titleKey: 'nav.overview' } },
    { path: '/transakcje', component: () => import('./views/TransactionsView.vue'), meta: { titleKey: 'nav.transactions' } },
    { path: '/transfery', redirect: '/konta' },
    { path: '/konta', component: () => import('./views/AccountsView.vue'), meta: { titleKey: 'nav.accounts' } },
    { path: '/kategorie', component: () => import('./views/CategoriesView.vue'), meta: { titleKey: 'nav.categories' } },
    { path: '/raporty', component: () => import('./views/ReportsView.vue'), meta: { titleKey: 'nav.reports' } },
    { path: '/profil', component: () => import('./views/ProfileView.vue'), meta: { titleKey: 'nav.profile' } }
  ]
})
onSessionExpired(() => {
  const current = router.currentRoute.value
  if (current.path !== '/auth') {
    void router.replace({ path: '/auth', query: { reason: 'expired', redirect: current.fullPath } })
  }
})
router.beforeEach((to) => {
  if (!to.meta.public && !session.token) return { path: '/auth', query: { redirect: to.fullPath } }
  if (to.meta.onboarding && !session.token) return '/auth'
  if (session.token && !session.user?.onboardingCompleted && !to.meta.onboarding) return '/onboarding'
  if (to.path === '/auth' && session.token) return session.user?.onboardingCompleted ? '/' : '/onboarding'
})

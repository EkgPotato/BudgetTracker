import { createRouter, createWebHistory } from 'vue-router'
import DashboardView from './views/DashboardView.vue'
import TransactionsView from './views/TransactionsView.vue'
import AccountsView from './views/AccountsView.vue'
import CategoriesView from './views/CategoriesView.vue'
import ReportsView from './views/ReportsView.vue'
import AuthView from './views/AuthView.vue'
import OnboardingView from './views/OnboardingView.vue'
import ProfileView from './views/ProfileView.vue'
import { session } from './api'

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/auth', component: AuthView, meta: { public: true, titleKey: 'route.auth' } },
    { path: '/onboarding', component: OnboardingView, meta: { public: true, onboarding: true, titleKey: 'route.onboarding' } },
    { path: '/', component: DashboardView, meta: { titleKey: 'nav.overview' } },
    { path: '/transakcje', component: TransactionsView, meta: { titleKey: 'nav.transactions' } },
    { path: '/transfery', redirect: '/konta' },
    { path: '/konta', component: AccountsView, meta: { titleKey: 'nav.accounts' } },
    { path: '/kategorie', component: CategoriesView, meta: { titleKey: 'nav.categories' } },
    { path: '/raporty', component: ReportsView, meta: { titleKey: 'nav.reports' } },
    { path: '/profil', component: ProfileView, meta: { titleKey: 'nav.profile' } }
  ]
})
router.beforeEach((to) => {
  if (!to.meta.public && !session.token) return { path: '/auth', query: { redirect: to.fullPath } }
  if (to.meta.onboarding && !session.token) return '/auth'
  if (session.token && !session.user?.onboardingCompleted && !to.meta.onboarding) return '/onboarding'
  if (to.path === '/auth' && session.token) return session.user?.onboardingCompleted ? '/' : '/onboarding'
})

import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import { router } from './router'
import { locale, setLocale } from './i18n'
import './styles.css'
import './responsive.css'
import './shell.css'

setLocale(locale.value)

createApp(App)
  .use(createPinia())
  .use(router)
  .mount('#app')

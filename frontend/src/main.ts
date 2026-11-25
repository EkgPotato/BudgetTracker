import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import { router } from './router'
import { locale, setLocale } from './i18n'
import ElementPlus from 'element-plus'
import 'element-plus/dist/index.css'
import './styles.css'
import './responsive.css'
import './shell.css'
import './element-theme.css'

setLocale(locale.value)

createApp(App)
  .use(ElementPlus)
  .use(createPinia())
  .use(router)
  .mount('#app')

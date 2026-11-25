import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import { router } from './router'
import { locale, setLocale } from './i18n'
import ElementPlus from 'element-plus'
import 'element-plus/dist/index.css'
import 'element-plus/theme-chalk/dark/css-vars.css'
import { setTheme, theme } from './theme'
import { installValidationMessages } from './validation'
import './styles.css'
import './responsive.css'
import './shell.css'
import './element-theme.css'
import './dark.css'

setLocale(locale.value)
setTheme(theme.value)
installValidationMessages()

createApp(App)
  .use(ElementPlus)
  .use(createPinia())
  .use(router)
  .mount('#app')

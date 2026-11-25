import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import { router } from './router'
import { locale, setLocale } from './i18n'
import { ElButton, ElConfigProvider, ElDatePicker, ElDialog, ElInput, ElInputNumber, ElLoading, ElOption, ElPagination, ElPopover, ElSelect, ElTable, ElTableColumn, ElTreeSelect } from 'element-plus'
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

const app = createApp(App)
for (const component of [ElButton, ElConfigProvider, ElDatePicker, ElDialog, ElInput, ElInputNumber, ElOption, ElPagination, ElPopover, ElSelect, ElTable, ElTableColumn, ElTreeSelect]) app.use(component)
app.directive('loading', ElLoading.directive)
app.use(createPinia()).use(router).mount('#app')

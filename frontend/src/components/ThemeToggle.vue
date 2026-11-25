<script setup lang="ts">
import { Moon, Sun } from '@lucide/vue'
import { t } from '@/i18n'
import { setTheme, theme } from '@/theme'
</script>

<template>
  <el-button class="theme-toggle" :aria-label="t(theme === 'dark' ? 'theme.light' : 'theme.dark')" :title="t(theme === 'dark' ? 'theme.light' : 'theme.dark')" @click="setTheme(theme === 'dark' ? 'light' : 'dark')">
    <Sun v-if="theme === 'dark'" :size="18" /><Moon v-else :size="18" />
  </el-button>
</template>

<style scoped>.theme-toggle{height:44px;width:44px;padding:0;flex-shrink:0}</style>

<script setup lang="ts">
import { locale, setLocale, t, type Locale } from '@/i18n'

function updateLocale(event: Event): void {
  const value = (event.target as HTMLSelectElement).value
  if (value === 'en' || value === 'pl') setLocale(value as Locale)
}
</script>

<template>
  <label class="language-selector">
    <span class="sr-only">{{ t('language.label') }}</span>
    <select :value="locale" :aria-label="t('language.label')" @change="updateLocale">
      <option value="en">{{ t('language.english') }}</option>
      <option value="pl">{{ t('language.polish') }}</option>
    </select>
  </label>
</template>

<style scoped>
.language-selector select{min-height:38px;padding:7px 32px 7px 10px;border:1px solid #d8e1ef;border-radius:9px;background-color:#fff;color:#1b2b47;font:700 12px inherit;cursor:pointer}
</style>

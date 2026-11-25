<script setup lang="ts">
import { locale, setLocale, t, type Locale } from '@/i18n'

function updateLocale(value: Locale): void {
  if (value === 'en' || value === 'pl') setLocale(value)
}
</script>

<template>
  <el-select class="language-selector" :model-value="locale" :aria-label="t('language.label')" @update:model-value="updateLocale">
    <template #prefix><img class="flag" :src="locale === 'pl' ? '/flag-pl.svg' : '/flag-gb.svg'" alt=""></template>
    <el-option value="en" :label="t('language.english')"><span class="language-option"><img class="flag" src="/flag-gb.svg" alt="">{{ t('language.english') }}</span></el-option>
    <el-option value="pl" :label="t('language.polish')"><span class="language-option"><img class="flag" src="/flag-pl.svg" alt="">{{ t('language.polish') }}</span></el-option>
  </el-select>
</template>

<style scoped>
.language-selector{width:145px}.flag{width:23px;height:23px;object-fit:cover;border-radius:50%;border:1px solid #e0e5ef}.language-option{display:flex;align-items:center;gap:10px;height:100%}
</style>

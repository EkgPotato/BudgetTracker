<script setup lang="ts">
import { ref } from 'vue'
import { api, type ImportPreview } from '@/api'
const preview = ref<ImportPreview>(); const error = ref(''); const done = ref(''); const date = new Date().toISOString().slice(0, 10)
async function selectFile(event: Event) {
  const file = (event.target as HTMLInputElement).files?.[0]; if (!file) return
  error.value = ''; done.value = ''
  try { preview.value = await api.previewImport(file) } catch (e) { error.value = e instanceof Error ? e.message : 'Import nie powiódł się.' }
}
async function commit() { if (!preview.value) return; await api.commitImport({ budgetSpaceName: preview.value.suggestedBudgetSpace, accounts: preview.value.accounts, categories: preview.value.categories, openingBalanceDate: date }); done.value = 'Dane zostały zaimportowane.' }
</script>
<template><section class="panel"><h2>Import danych z Excela</h2><p class="muted">Wybierz jeden z arkuszy budżetowych. Najpierw zobaczysz dane wykryte przez aplikację.</p><input accept=".xlsx" type="file" @change="selectFile"/><p v-if="error" class="notice">{{error}}</p><template v-if="preview"><div class="panel-title"><h3>Portfel: {{preview.suggestedBudgetSpace}}</h3><button class="button primary" @click="commit">Zapisz import</button></div><p v-for="warning in preview.warnings" :key="warning" class="hint">{{warning}}</p><div class="grid two"><div><h3>Konta i salda otwarcia</h3><p v-for="account in preview.accounts" :key="account.name">{{account.name}} — {{account.openingBalance ?? 0}} PLN</p></div><div><h3>Kategorie</h3><p v-for="category in preview.categories" :key="category.name">{{category.name}}</p></div></div></template><p v-if="done" class="success">{{done}}</p></section></template>

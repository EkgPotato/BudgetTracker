<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { api, apiErrorMessage } from '@/api'
import { locale, t } from '@/i18n'
import type { BudgetSpace, Category, TransactionKind } from '@/types'

const categories = ref<Category[]>([])
const spaces = ref<BudgetSpace[]>([])
const error = ref('')
const search = ref('')
const form = ref({ name: '', budgetSpaceId: '', appliesTo: 'expense' as TransactionKind, parentCategoryId: '' })
const mainCategories = computed(() => categories.value.filter(category => !category.parentCategoryId).sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name, locale.value)))
const parents = computed(() => mainCategories.value.filter(category => category.budgetSpaceId === form.value.budgetSpaceId && category.appliesTo === form.value.appliesTo))
const normalizedSearch = computed(() => search.value.trim().toLocaleLowerCase(locale.value))
function children(parentId: string): Category[] { return categories.value.filter(category => category.parentCategoryId === parentId).sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name, locale.value)) }
function isVisible(category: Category): boolean { const query = normalizedSearch.value; return !query || category.name.toLocaleLowerCase(locale.value).includes(query) || children(category.id).some(child => child.name.toLocaleLowerCase(locale.value).includes(query)) }
async function load(): Promise<void> { [categories.value, spaces.value] = await Promise.all([api.categories(), api.spaces()]); form.value.budgetSpaceId ||= spaces.value[0]?.id ?? '' }
async function save(): Promise<void> { error.value = ''; try { await api.createCategory({ ...form.value, parentCategoryId: form.value.parentCategoryId || null, sortOrder: categories.value.length }); form.value.name = ''; form.value.parentCategoryId = ''; await load() } catch (caught: unknown) { error.value = apiErrorMessage(caught, t('finance.categories.saveError')) } }
onMounted(() => { void load().catch(caught => { error.value = apiErrorMessage(caught, t('finance.categories.loadError')) }) })
</script>

<template>
  <div class="categories-page">
    <section class="categories-hero"><div><p class="section-kicker">{{ t('finance.categories.kicker') }}</p><h2>{{ t('finance.categories.title') }}</h2><p>{{ t('finance.categories.subtitle') }}</p></div><span>{{ t('finance.categories.count', { count: categories.length }) }}</span></section>
    <p v-if="error" class="notice">{{ error }}</p>
    <div class="categories-grid">
      <section class="panel"><div class="panel-title"><div><h2>{{ t('finance.categories.mainCategories') }}</h2><p class="muted">{{ t('finance.categories.searchHint') }}</p></div></div><label class="category-search"><span class="sr-only">{{ t('finance.categories.searchLabel') }}</span><input v-model.trim="search" type="search" :placeholder="t('finance.categories.searchPlaceholder')"></label><div v-if="mainCategories.filter(isVisible).length" class="category-tree"><article v-for="category in mainCategories.filter(isVisible)" :key="category.id" class="category-group"><div class="category-main"><div><strong>{{ category.name }}</strong><small>{{ category.appliesTo === 'expense' ? t('finance.categories.expense') : t('finance.categories.income') }}</small></div><span>{{ t('finance.categories.subcategoryCount', { count: children(category.id).length }) }}</span></div><ul v-if="children(category.id).length"><li v-for="child in children(category.id).filter(child => !normalizedSearch || child.name.toLocaleLowerCase(locale).includes(normalizedSearch))" :key="child.id"><i>↳</i>{{ child.name }}</li></ul><p v-else class="no-children">{{ t('finance.categories.noSubcategories') }}</p></article></div><p v-else class="empty">{{ t('finance.categories.noMatches') }}</p></section>
      <section class="panel"><h2>{{ t('finance.categories.addCategory') }}</h2><p class="muted">{{ t('finance.categories.parentHint') }}</p><form @submit.prevent="save"><label>{{ t('finance.categories.name') }}<el-input v-model.trim="form.name" required :placeholder="t('finance.categories.namePlaceholder')" /></label><label>{{ t('finance.categories.budgetSpace') }}<el-select v-model="form.budgetSpaceId" filterable @change="form.parentCategoryId=''"><el-option v-for="space in spaces" :key="space.id" :value="space.id" :label="space.name" /></el-select></label><label>{{ t('finance.categories.kind') }}<el-select v-model="form.appliesTo" @change="form.parentCategoryId=''"><el-option value="expense" :label="t('finance.categories.expense')" /><el-option value="income" :label="t('finance.categories.income')" /></el-select></label><label>{{ t('finance.categories.mainCategory') }}<el-select v-model="form.parentCategoryId" filterable><el-option value="" :label="t('finance.categories.noParent')" /><el-option v-for="category in parents" :key="category.id" :value="category.id" :label="category.name" /></el-select></label><el-button type="primary" native-type="submit">{{ t('finance.categories.submit') }}</el-button></form></section>
    </div>
  </div>
</template>

<style scoped>
.categories-page{display:grid;gap:20px}.categories-hero{display:flex;justify-content:space-between;align-items:center;gap:16px;padding:25px 27px;border:1px solid #e1e8fa;border-radius:20px;background:linear-gradient(135deg,#fff,#f2f6ff);box-shadow:0 12px 28px #1038ab0a}.categories-hero h2{margin:0;color:#172a4d;font-size:25px}.categories-hero p:not(.section-kicker){margin:8px 0 0;color:#71809e}.categories-hero>span{border-radius:999px;padding:8px 11px;background:#e8efff;color:#1038ab;font-size:12px;font-weight:800}.section-kicker{margin:0 0 6px;color:#4d7cfe;font-size:11px;font-weight:800;letter-spacing:.12em}.categories-grid{display:grid;grid-template-columns:minmax(0,1.3fr) minmax(310px,.7fr);gap:18px}.category-search{display:block;margin-top:18px}.category-search input{width:100%}.category-tree{display:grid;gap:12px;margin-top:14px}.category-group{overflow:hidden;border:1px solid #e4eafa;border-radius:14px}.category-main{display:flex;align-items:center;justify-content:space-between;gap:15px;padding:15px 16px;background:#f8faff}.category-main strong,.category-main small{display:block}.category-main strong{color:#1b2c4a}.category-main small{margin-top:3px;color:#73829d;font-size:11px}.category-main>span{color:#6f7f9d;font-size:11px;font-weight:700}.category-group ul{display:grid;gap:8px;margin:0;padding:12px 16px;list-style:none}.category-group li{display:flex;gap:9px;align-items:center;color:#42536f;font-size:13px}.category-group li i{font-style:normal;color:#4d7cfe}.no-children{margin:0;padding:0 16px 14px;color:#8793a9;font-size:12px}.panel-title{margin-bottom:0}.sr-only{position:absolute;width:1px;height:1px;overflow:hidden;clip:rect(0,0,0,0)}@media(max-width:800px){.categories-grid{grid-template-columns:1fr}.categories-hero{align-items:flex-start;flex-direction:column;padding:21px}.categories-hero h2{font-size:23px}}
</style>
<style scoped>
.category-tree{gap:6px}.category-group{border-radius:7px}.category-main{padding:9px 12px}.category-main strong,.category-main small{display:inline}.category-main small{margin-left:10px}.category-group ul{display:flex;flex-wrap:wrap;gap:6px;padding:4px 12px 10px}.category-group li{padding:3px 8px;background:#eef3fc;border-radius:5px}.no-children{display:none}.categories-hero{padding:16px 20px}
</style>

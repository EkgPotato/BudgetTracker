<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { api, apiErrorMessage } from '@/api'
import type { BudgetSpace, Category, TransactionKind } from '@/types'

const categories = ref<Category[]>([])
const spaces = ref<BudgetSpace[]>([])
const error = ref('')
const search = ref('')
const form = ref({ name: '', budgetSpaceId: '', appliesTo: 'expense' as TransactionKind, parentCategoryId: '' })
const mainCategories = computed(() => categories.value.filter(category => !category.parentCategoryId).sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name, 'pl')))
const parents = computed(() => mainCategories.value.filter(category => category.budgetSpaceId === form.value.budgetSpaceId && category.appliesTo === form.value.appliesTo))
const normalizedSearch = computed(() => search.value.trim().toLocaleLowerCase('pl'))
function children(parentId: string): Category[] { return categories.value.filter(category => category.parentCategoryId === parentId).sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name, 'pl')) }
function isVisible(category: Category): boolean { const query = normalizedSearch.value; return !query || category.name.toLocaleLowerCase('pl').includes(query) || children(category.id).some(child => child.name.toLocaleLowerCase('pl').includes(query)) }
async function load(): Promise<void> { [categories.value, spaces.value] = await Promise.all([api.categories(), api.spaces()]); form.value.budgetSpaceId ||= spaces.value[0]?.id ?? '' }
async function save(): Promise<void> { error.value = ''; try { await api.createCategory({ ...form.value, parentCategoryId: form.value.parentCategoryId || null, sortOrder: categories.value.length }); form.value.name = ''; form.value.parentCategoryId = ''; await load() } catch (caught: unknown) { error.value = apiErrorMessage(caught, 'Nie udało się zapisać kategorii.') } }
onMounted(() => { void load().catch(caught => { error.value = apiErrorMessage(caught, 'Nie udało się pobrać kategorii.') }) })
</script>

<template>
  <div class="categories-page">
    <section class="categories-hero"><div><p class="section-kicker">STRUKTURA BUDŻETU</p><h2>Kategorie i podkategorie</h2><p>Porządkuj wydatki od szerokich obszarów do konkretnych celów.</p></div><span>{{ categories.length }} pozycji</span></section>
    <p v-if="error" class="notice">{{ error }}</p>
    <div class="categories-grid">
      <section class="panel"><div class="panel-title"><div><h2>Kategorie główne</h2><p class="muted">Wyszukuj w kategoriach oraz ich podkategoriach.</p></div></div><label class="category-search"><span class="sr-only">Szukaj kategorii</span><input v-model.trim="search" type="search" placeholder="Szukaj kategorii…"></label><div v-if="mainCategories.filter(isVisible).length" class="category-tree"><article v-for="category in mainCategories.filter(isVisible)" :key="category.id" class="category-group"><div class="category-main"><div><strong>{{ category.name }}</strong><small>{{ category.appliesTo === 'expense' ? 'Wydatek' : 'Przychód' }}</small></div><span>{{ children(category.id).length }} podkategorii</span></div><ul v-if="children(category.id).length"><li v-for="child in children(category.id).filter(child => !normalizedSearch || child.name.toLocaleLowerCase('pl').includes(normalizedSearch))" :key="child.id"><i>↳</i>{{ child.name }}</li></ul><p v-else class="no-children">Brak podkategorii</p></article></div><p v-else class="empty">Brak kategorii pasujących do wyszukiwania.</p></section>
      <section class="panel"><h2>Dodaj kategorię</h2><p class="muted">Wybierz kategorię główną, aby dodać podkategorię.</p><form @submit.prevent="save"><label>Nazwa<input v-model.trim="form.name" required placeholder="np. Zakupy spożywcze"></label><label>Portfel<select v-model="form.budgetSpaceId"><option v-for="space in spaces" :key="space.id" :value="space.id">{{ space.name }}</option></select></label><label>Rodzaj<select v-model="form.appliesTo" @change="form.parentCategoryId=''"><option value="expense">Wydatek</option><option value="income">Przychód</option></select></label><label>Kategoria główna<select v-model="form.parentCategoryId"><option value="">Brak — utwórz kategorię główną</option><option v-for="category in parents" :key="category.id" :value="category.id">{{ category.name }}</option></select></label><button class="button primary">Dodaj kategorię</button></form></section>
    </div>
  </div>
</template>

<style scoped>
.categories-page{display:grid;gap:20px}.categories-hero{display:flex;justify-content:space-between;align-items:center;gap:16px;padding:25px 27px;border:1px solid #e1e8fa;border-radius:20px;background:linear-gradient(135deg,#fff,#f2f6ff);box-shadow:0 12px 28px #1038ab0a}.categories-hero h2{margin:0;color:#172a4d;font-size:25px}.categories-hero p:not(.section-kicker){margin:8px 0 0;color:#71809e}.categories-hero>span{border-radius:999px;padding:8px 11px;background:#e8efff;color:#1038ab;font-size:12px;font-weight:800}.section-kicker{margin:0 0 6px;color:#4d7cfe;font-size:11px;font-weight:800;letter-spacing:.12em}.categories-grid{display:grid;grid-template-columns:minmax(0,1.3fr) minmax(310px,.7fr);gap:18px}.category-search{display:block;margin-top:18px}.category-search input{width:100%}.category-tree{display:grid;gap:12px;margin-top:14px}.category-group{overflow:hidden;border:1px solid #e4eafa;border-radius:14px}.category-main{display:flex;align-items:center;justify-content:space-between;gap:15px;padding:15px 16px;background:#f8faff}.category-main strong,.category-main small{display:block}.category-main strong{color:#1b2c4a}.category-main small{margin-top:3px;color:#73829d;font-size:11px}.category-main>span{color:#6f7f9d;font-size:11px;font-weight:700}.category-group ul{display:grid;gap:8px;margin:0;padding:12px 16px;list-style:none}.category-group li{display:flex;gap:9px;align-items:center;color:#42536f;font-size:13px}.category-group li i{font-style:normal;color:#4d7cfe}.no-children{margin:0;padding:0 16px 14px;color:#8793a9;font-size:12px}.panel-title{margin-bottom:0}.sr-only{position:absolute;width:1px;height:1px;overflow:hidden;clip:rect(0,0,0,0)}@media(max-width:800px){.categories-grid{grid-template-columns:1fr}.categories-hero{align-items:flex-start;flex-direction:column;padding:21px}.categories-hero h2{font-size:23px}}
</style>

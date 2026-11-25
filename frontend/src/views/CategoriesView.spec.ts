import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import CategoriesView from './CategoriesView.vue'

const { categories, spaces, createCategory } = vi.hoisted(() => ({ categories: vi.fn(), spaces: vi.fn(), createCategory: vi.fn() }))
vi.mock('@/api', () => ({
  api: { categories, spaces, createCategory },
  apiErrorMessage: (_error: unknown, fallback: string) => fallback,
}))

describe('categories', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    categories.mockResolvedValue([
      { id: 'home', name: 'Home', appliesTo: 'expense', budgetSpaceId: 'space', sortOrder: 1 },
      { id: 'bills', name: 'Bills', appliesTo: 'expense', budgetSpaceId: 'space', parentCategoryId: 'home', sortOrder: 2 },
    ])
    spaces.mockResolvedValue([{ id: 'space', name: 'Home budget' }])
  })

  it('shows the subcategories when the parent category matches search', async () => {
    const wrapper = mount(CategoriesView, { global: { stubs: ['el-input', 'el-select', 'el-option', 'el-button'] } })
    await flushPromises()
    await wrapper.get('input[type="search"]').setValue('Home')
    expect(wrapper.text()).toContain('Bills')
  })

  it('prevents duplicate category submissions while saving', async () => {
    let finish!: () => void
    createCategory.mockImplementation(() => new Promise<void>(resolve => { finish = resolve }))
    const wrapper = mount(CategoriesView, { global: { stubs: ['el-input', 'el-select', 'el-option', 'el-button'] } })
    await flushPromises()
    ;(wrapper.vm as unknown as { form: { name: string } }).form.name = 'Transport'
    await wrapper.get('form').trigger('submit')
    await wrapper.get('form').trigger('submit')
    expect(createCategory).toHaveBeenCalledTimes(1)
    finish()
    await flushPromises()
    expect(wrapper.text()).toContain('Category saved.')
  })
})

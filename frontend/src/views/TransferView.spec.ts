import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import TransferView from './TransferView.vue'

const { accounts, createTransfer } = vi.hoisted(() => ({ accounts: vi.fn(), createTransfer: vi.fn() }))
vi.mock('@/api', () => ({
  api: { accounts, createTransfer },
  apiErrorMessage: (_error: unknown, fallback: string) => fallback,
}))

describe('transfer submission', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    accounts.mockReset()
    createTransfer.mockReset()
    localStorage.removeItem('grosik.locale')
  })

  it('confirms a saved transfer even when refreshing accounts fails and ignores repeated submissions', async () => {
    accounts.mockResolvedValueOnce([{ id: 'a', name: 'A' }, { id: 'b', name: 'B' }]).mockRejectedValueOnce(new Error('offline'))
    let finish!: () => void
    createTransfer.mockImplementation(() => new Promise<void>(resolve => { finish = resolve }))
    const wrapper = mount(TransferView, { global: { stubs: ['el-select', 'el-option', 'el-input', 'el-date-picker', 'el-button'] } })
    await flushPromises()
    ;(wrapper.vm as unknown as { form: { amount: number } }).form.amount = 10

    await wrapper.find('form').trigger('submit')
    await wrapper.find('form').trigger('submit')
    expect(createTransfer).toHaveBeenCalledTimes(1)
    finish()
    await flushPromises()

    expect(wrapper.emitted('transferred')).toHaveLength(1)
    expect(wrapper.text()).toContain('Transfer saved.')
    expect(wrapper.text()).toContain('Saved successfully, but the data could not be refreshed.')
    expect(wrapper.text()).not.toContain('Unable to make the transfer.')
  })
})

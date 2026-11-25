import { afterEach, describe, expect, it, vi } from 'vitest'

const keys = ['grosik.access-token', 'grosik.user']

afterEach(() => {
  localStorage.clear()
  vi.restoreAllMocks()
  vi.resetModules()
})

describe('session persistence', () => {
  it('removes malformed user data together with a stale token', async () => {
    localStorage.setItem('grosik.access-token', 'stale-token')
    localStorage.setItem('grosik.user', '{not-json')

    const { session } = await import('./api')

    expect(session.user).toBeNull()
    expect(session.token).toBeNull()
  })

  it('clears the session after an unauthorized API response', async () => {
    localStorage.setItem('grosik.access-token', 'token')
    localStorage.setItem('grosik.user', JSON.stringify({ id: '1', email: 'user@example.test', isAdmin: false }))
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('', { status: 401 })))

    const { api, ApiError, session } = await import('./api')

    await expect(api.dashboard()).rejects.toBeInstanceOf(ApiError)
    expect(session.token).toBeNull()
    expect(session.user).toBeNull()
  })

  it('sends the selected locale with API requests', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('{}', { status: 200 })))
    const { setLocale } = await import('./i18n')
    const { api } = await import('./api')

    setLocale('pl')
    await api.dashboard()

    const [, options] = vi.mocked(fetch).mock.calls[0]
    expect((options?.headers as Headers).get('Accept-Language')).toBe('pl')
  })

  it('stores a validated authenticated user', async () => {
    const { session } = await import('./api')
    session.save({ accessToken: 'token', user: { id: '1', email: 'user@example.test', isAdmin: false } })

    expect(session.token).toBe('token')
    expect(session.user?.email).toBe('user@example.test')
    expect(keys.every(key => localStorage.getItem(key) !== null)).toBe(true)
  })
})

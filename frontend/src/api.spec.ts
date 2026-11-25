import { afterEach, describe, expect, it, vi } from 'vitest'

const keys = ['grosik.access-token', 'grosik.user']

afterEach(() => {
  localStorage.clear()
  vi.restoreAllMocks()
  vi.unstubAllGlobals()
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

    const { api, ApiError, session, onSessionExpired } = await import('./api')
    const expired = vi.fn()
    onSessionExpired(expired)

    await expect(api.dashboard()).rejects.toBeInstanceOf(ApiError)
    expect(session.token).toBeNull()
    expect(session.user).toBeNull()
    expect(expired).toHaveBeenCalledOnce()
  })

  it('keeps the session when access is forbidden', async () => {
    localStorage.setItem('grosik.access-token', 'token')
    localStorage.setItem('grosik.user', JSON.stringify({ id: '1', email: 'user@example.test', isAdmin: false }))
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('', { status: 403 })))
    const { api, session, onSessionExpired } = await import('./api')
    const expired = vi.fn()
    onSessionExpired(expired)

    await expect(api.dashboard()).rejects.toMatchObject({ status: 403 })
    expect(session.token).toBe('token')
    expect(expired).not.toHaveBeenCalled()
  })

  it('does not expire a new session when an older request returns 401', async () => {
    localStorage.setItem('grosik.access-token', 'old-token')
    let resolveResponse!: (response: Response) => void
    vi.stubGlobal('fetch', vi.fn().mockImplementation(() => new Promise<Response>(resolve => { resolveResponse = resolve })))
    const { api, session, onSessionExpired } = await import('./api')
    const expired = vi.fn()
    onSessionExpired(expired)
    const request = api.dashboard()
    session.save({ accessToken: 'new-token', user: { id: '2', email: 'other@example.test', isAdmin: false } })
    resolveResponse(new Response('', { status: 401 }))

    await expect(request).rejects.toMatchObject({ status: 401 })
    expect(session.token).toBe('new-token')
    expect(expired).not.toHaveBeenCalled()
  })

  it('reports bad login credentials without triggering session expiry', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('', { status: 401 })))
    const { api, onSessionExpired } = await import('./api')
    const expired = vi.fn()
    onSessionExpired(expired)

    await expect(api.login('user@example.test', 'bad-password')).rejects.toMatchObject({ status: 401, message: expect.stringContaining('email') })
    expect(expired).not.toHaveBeenCalled()
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

  it('sends page and repeated filter parameters to the transaction endpoint', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('{"items":[],"total":0,"offset":25,"limit":25}', { status: 200 })))
    const { api } = await import('./api')

    await api.transactionPage({ offset: 25, limit: 25, kinds: ['expense', 'transferDebit'], accountIds: ['account-1'], from: '2026-01-01' })

    const [url] = vi.mocked(fetch).mock.calls[0]
    const parsed = new URL(String(url), 'http://localhost')
    expect(parsed.pathname).toBe('/api/transactions/page')
    expect(parsed.searchParams.get('offset')).toBe('25')
    expect(parsed.searchParams.getAll('kinds')).toEqual(['expense', 'transferDebit'])
    expect(parsed.searchParams.get('accountIds')).toBe('account-1')
    expect(parsed.searchParams.get('from')).toBe('2026-01-01')
  })

  it('stores a validated authenticated user', async () => {
    const { session } = await import('./api')
    session.save({ accessToken: 'token', user: { id: '1', email: 'user@example.test', isAdmin: false } })

    expect(session.token).toBe('token')
    expect(session.user?.email).toBe('user@example.test')
    expect(keys.every(key => localStorage.getItem(key) !== null)).toBe(true)
  })
})

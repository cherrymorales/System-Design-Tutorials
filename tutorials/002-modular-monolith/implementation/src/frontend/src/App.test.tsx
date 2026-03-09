import { render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import App from './App'

const originalFetch = globalThis.fetch

afterEach(() => {
  vi.restoreAllMocks()
  globalThis.fetch = originalFetch
})

describe('App', () => {
  it('renders the login page when session restoration fails', async () => {
    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: false,
      status: 401,
      json: async () => ({ message: 'Authentication is required.' }),
    } as Response)

    render(
      <MemoryRouter>
        <App />
      </MemoryRouter>,
    )

    await waitFor(() => expect(screen.getByRole('heading', { name: /b2b wholesale operations console/i })).toBeInTheDocument())
    expect(screen.getByRole('button', { name: /sales coordinator/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /operations manager/i })).toBeInTheDocument()
  })
})


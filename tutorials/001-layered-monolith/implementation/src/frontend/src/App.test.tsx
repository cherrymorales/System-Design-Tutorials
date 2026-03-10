import { BrowserRouter } from 'react-router-dom'
import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import App from './App'
import { vi } from 'vitest'

describe('App', () =>
{
  beforeEach(() =>
  {
    vi.restoreAllMocks()
  })

  it('renders the login page when session restoration fails', async () =>
  {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(null, { status: 401 }))

    render(
      <BrowserRouter>
        <App />
      </BrowserRouter>,
    )

    await waitFor(() =>
    {
      expect(screen.getByRole('heading', { name: /identity-driven inventory console/i })).toBeInTheDocument()
    })
  })

  it('updates the email field when a seeded account card is selected', async () =>
  {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(null, { status: 401 }))

    render(
      <BrowserRouter>
        <App />
      </BrowserRouter>,
    )

    await waitFor(() =>
    {
      expect(screen.getByRole('heading', { name: /identity-driven inventory console/i })).toBeInTheDocument()
    })

    fireEvent.click(screen.getByRole('button', { name: /brisbane operator/i }))

    expect(screen.getByLabelText(/email/i)).toHaveValue('operator.brisbane@layeredmonolith.local')
  })
})

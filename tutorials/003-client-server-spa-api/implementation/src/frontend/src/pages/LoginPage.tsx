import { useState } from 'react'
import type { FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { fetchJson } from '../api'
import { defaultLoginForm, seededUsers, type LoginFormState, type UserSession } from '../models'

type LoginPageProps = {
  onLoggedIn: (session: UserSession) => void
}

export function LoginPage({ onLoggedIn }: LoginPageProps) {
  const navigate = useNavigate()
  const [form, setForm] = useState<LoginFormState>(defaultLoginForm)
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSubmitting(true)
    setError(null)

    try {
      const session = await fetchJson<UserSession>('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(form),
      })
      onLoggedIn(session)
      navigate('/', { replace: true })
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : 'Login failed.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="page-shell page-shell--auth">
      <section className="auth-card">
        <p className="eyebrow">003 Client-Server SPA + API</p>
        <h1>Project delivery workspace</h1>
        <p className="intro">
          This tutorial uses same-origin cookie auth with a React SPA consuming stable ASP.NET Core API contracts.
          Password for every seeded account is <code>Password123!</code>.
        </p>
        <form className="entity-form auth-form" onSubmit={handleSubmit}>
          <div className="form-grid">
            <label>
              <span>Email</span>
              <input
                value={form.email}
                onChange={(event) => setForm((current) => ({ ...current, email: event.target.value }))}
              />
            </label>
            <label>
              <span>Password</span>
              <input
                type="password"
                value={form.password}
                onChange={(event) => setForm((current) => ({ ...current, password: event.target.value }))}
              />
            </label>
          </div>
          <div className="form-actions">
            <button className="primary-button" type="submit" disabled={isSubmitting}>
              {isSubmitting ? 'Signing in...' : 'Sign in'}
            </button>
          </div>
        </form>
        {error ? <p className="banner banner--error">{error}</p> : null}
        <div className="account-grid">
          {seededUsers.map((user) => (
            <button
              key={user.email}
              type="button"
              className="account-card"
              onClick={() => setForm({ email: user.email, password: 'Password123!' })}
            >
              <strong>{user.label}</strong>
              <span>{user.email}</span>
              <small>{user.role}</small>
            </button>
          ))}
        </div>
      </section>
    </main>
  )
}

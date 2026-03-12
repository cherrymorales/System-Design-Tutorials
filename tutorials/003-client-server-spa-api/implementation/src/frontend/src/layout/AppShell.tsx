import { useEffect, useState } from 'react'
import { NavLink, Outlet } from 'react-router-dom'
import { fetchJson } from '../api'
import type { HealthResponse, UserSession } from '../models'
import { navClassName } from '../lib/ui'

type AppShellProps = {
  session: UserSession
  onLogout: (callApi?: boolean) => Promise<void>
}

export function AppShell({ session, onLogout }: AppShellProps) {
  const [health, setHealth] = useState<HealthResponse | null>(null)

  useEffect(() => {
    void loadHealth()
  }, [])

  async function loadHealth() {
    try {
      setHealth(await fetchJson<HealthResponse>('/api/health'))
    } catch {
      setHealth(null)
    }
  }

  return (
    <main className="page-shell">
      <section className="shell-header">
        <div className="shell-copy">
          <p className="eyebrow">003 Client-Server SPA + API</p>
          <h1>Project delivery and task collaboration</h1>
          <p className="intro">
            Route-based SPA navigation, stable JSON contracts, and server-owned workflow rules for projects, tasks,
            comments, and dashboard summaries.
          </p>
          <div className="session-meta">
            <span className="pill pill--active">{session.displayName}</span>
            <span className="pill">{session.email}</span>
            <span className="pill">{session.roles.join(', ')}</span>
          </div>
        </div>
        <div className="shell-actions">
          <div className={`health-chip ${health ? 'health-chip--ok' : 'health-chip--pending'}`}>
            <span>{health?.status ?? 'unavailable'}</span>
            <small>{health ? new Date(health.timestamp).toLocaleString() : 'Health endpoint not loaded'}</small>
          </div>
          <button className="ghost-button" onClick={() => void onLogout()}>
            Sign out
          </button>
        </div>
      </section>

      <nav className="nav-strip" aria-label="Primary">
        <NavLink end to="/" className={({ isActive }) => navClassName(isActive)}>
          Dashboard
        </NavLink>
        <NavLink to="/projects" className={({ isActive }) => navClassName(isActive)}>
          Projects
        </NavLink>
        <NavLink to="/tasks" className={({ isActive }) => navClassName(isActive)}>
          Tasks
        </NavLink>
      </nav>

      <Outlet />
    </main>
  )
}

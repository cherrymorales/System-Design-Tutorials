import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { fetchJson } from '../api'
import type { DashboardSummary, ProjectTaskSummary } from '../models'
import { formatDateTime, formatDateValue, handlePageError, statusBadgeClass } from '../lib/ui'

type DashboardPageProps = {
  onSessionExpired: () => void
}

export function DashboardPage({ onSessionExpired }: DashboardPageProps) {
  const [summary, setSummary] = useState<DashboardSummary | null>(null)
  const [myWork, setMyWork] = useState<ProjectTaskSummary[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [isRefreshing, setIsRefreshing] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    void loadDashboard()
  }, [])

  async function loadDashboard(refresh = false) {
    if (refresh) setIsRefreshing(true)
    else setIsLoading(true)

    setError(null)

    try {
      const [summaryPayload, myWorkPayload] = await Promise.all([
        fetchJson<DashboardSummary>('/api/dashboard/summary'),
        fetchJson<ProjectTaskSummary[]>('/api/dashboard/my-work'),
      ])
      setSummary(summaryPayload)
      setMyWork(myWorkPayload)
    } catch (loadError) {
      handlePageError(loadError, onSessionExpired, setError, 'Failed to load dashboard data.')
    } finally {
      setIsLoading(false)
      setIsRefreshing(false)
    }
  }

  return (
    <section className="page-section">
      <div className="section-heading">
        <div>
          <p className="eyebrow">Dashboard</p>
          <h2>Workspace summary</h2>
        </div>
        <button className="secondary-button" onClick={() => void loadDashboard(true)} disabled={isRefreshing}>
          {isRefreshing ? 'Refreshing...' : 'Refresh'}
        </button>
      </div>

      {error ? <p className="banner banner--error">{error}</p> : null}
      {isLoading ? <p className="banner">Loading dashboard...</p> : null}

      {summary ? (
        <section className="stats-grid">
          <article className="stat-card">
            <span className="stat-label">Active projects</span>
            <strong>{summary.activeProjectCount}</strong>
            <small>Currently in delivery</small>
          </article>
          <article className="stat-card">
            <span className="stat-label">At risk</span>
            <strong>{summary.atRiskProjectCount}</strong>
            <small>Projects needing intervention</small>
          </article>
          <article className="stat-card">
            <span className="stat-label">Overdue tasks</span>
            <strong>{summary.overdueTaskCount}</strong>
            <small>Open work past due date</small>
          </article>
          <article className="stat-card">
            <span className="stat-label">My open work</span>
            <strong>{summary.myOpenTaskCount}</strong>
            <small>Assigned to the signed-in user</small>
          </article>
        </section>
      ) : null}

      <section className="panel-grid panel-grid--two">
        <article className="panel">
          <div className="panel-heading">
            <div>
              <p className="eyebrow">My work</p>
              <h3>Assigned tasks</h3>
            </div>
            <span className="pill">{myWork.length} items</span>
          </div>
          {myWork.length === 0 ? <p className="empty-state">No active assignments for this account.</p> : null}
          <div className="entity-list">
            {myWork.map((task) => (
              <Link key={task.id} className="entity-card entity-card--stacked" to={`/tasks/${task.id}`}>
                <div>
                  <strong>{task.title}</strong>
                  <p>{task.projectCode} · {task.assigneeDisplayName}</p>
                  <small>Due {formatDateValue(task.dueDate)} · Updated {formatDateTime(task.updatedAt)}</small>
                </div>
                <div className="card-summary">
                  <span className={statusBadgeClass(task.status)}>{task.status}</span>
                  <span className="pill">{task.priority}</span>
                  {task.isOverdue ? <span className="pill pill--warning">Overdue</span> : null}
                </div>
              </Link>
            ))}
          </div>
        </article>

        <article className="panel">
          <div className="panel-heading">
            <div>
              <p className="eyebrow">Architecture outcome</p>
              <h3>What this tutorial is demonstrating</h3>
            </div>
          </div>
          <div className="checklist">
            <div className="checklist-item">
              <strong>Server-owned rules</strong>
              <p>Project and task lifecycle rules stay on the backend and the SPA renders what the API contract exposes.</p>
            </div>
            <div className="checklist-item">
              <strong>Route-based client</strong>
              <p>The frontend treats dashboard, projects, and task detail as pages backed by API requests rather than server-rendered screens.</p>
            </div>
            <div className="checklist-item">
              <strong>Role-aware UX</strong>
              <p>Visible actions and editable forms follow returned session and detail permissions instead of assuming write access.</p>
            </div>
          </div>
        </article>
      </section>
    </section>
  )
}

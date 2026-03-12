import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { fetchJson } from '../api'
import { emptyTaskFilters, taskStatusOptions, type ProjectSummary, type ProjectTaskSummary, type TaskFiltersState, type UserOption } from '../models'
import { buildTaskQuery, formatDateTime, formatDateValue, handlePageError, statusBadgeClass } from '../lib/ui'

type TasksPageProps = {
  onSessionExpired: () => void
}

export function TasksPage({ onSessionExpired }: TasksPageProps) {
  const [projects, setProjects] = useState<ProjectSummary[]>([])
  const [assignableUsers, setAssignableUsers] = useState<UserOption[]>([])
  const [tasks, setTasks] = useState<ProjectTaskSummary[]>([])
  const [filters, setFilters] = useState<TaskFiltersState>(emptyTaskFilters())
  const [isLoading, setIsLoading] = useState(true)
  const [isRefreshing, setIsRefreshing] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    void loadInitialData()
  }, [])

  useEffect(() => {
    if (!filters.projectId) {
      setAssignableUsers([])
      setFilters((current) => ({ ...current, assigneeUserId: '' }))
      return
    }

    void loadAssignableUsers(filters.projectId)
  }, [filters.projectId])

  async function loadInitialData() {
    setIsLoading(true)
    setError(null)
    try {
      const [projectsPayload, tasksPayload] = await Promise.all([
        fetchJson<ProjectSummary[]>('/api/projects'),
        fetchJson<ProjectTaskSummary[]>('/api/tasks'),
      ])
      setProjects(projectsPayload)
      setTasks(tasksPayload)
    } catch (loadError) {
      handlePageError(loadError, onSessionExpired, setError, 'Failed to load tasks data.')
    } finally {
      setIsLoading(false)
    }
  }

  async function loadAssignableUsers(projectId: string) {
    try {
      setAssignableUsers(await fetchJson<UserOption[]>(`/api/users/assignable?projectId=${projectId}`))
    } catch (loadError) {
      handlePageError(loadError, onSessionExpired, setError, 'Failed to load project assignees.')
    }
  }

  async function applyFilters(event?: FormEvent<HTMLFormElement>) {
    event?.preventDefault()
    setIsRefreshing(true)
    setError(null)

    try {
      const query = buildTaskQuery(filters)
      setTasks(await fetchJson<ProjectTaskSummary[]>(`/api/tasks${query ? `?${query}` : ''}`))
    } catch (loadError) {
      handlePageError(loadError, onSessionExpired, setError, 'Failed to apply task filters.')
    } finally {
      setIsRefreshing(false)
    }
  }

  return (
    <section className="page-section">
      <div className="section-heading">
        <div>
          <p className="eyebrow">Tasks</p>
          <h2>Task directory and filters</h2>
        </div>
      </div>

      {error ? <p className="banner banner--error">{error}</p> : null}
      {isLoading ? <p className="banner">Loading tasks...</p> : null}

      <section className="panel-grid panel-grid--single">
        <article className="panel">
          <div className="panel-heading">
            <div>
              <p className="eyebrow">Filters</p>
              <h3>Query the task list</h3>
            </div>
            <span className="pill">{tasks.length} tasks</span>
          </div>

          <form className="entity-form" onSubmit={(event) => void applyFilters(event)}>
            <div className="form-grid">
              <label>
                <span>Project</span>
                <select value={filters.projectId} onChange={(event) => setFilters((current) => ({ ...current, projectId: event.target.value }))}>
                  <option value="">All projects</option>
                  {projects.map((project) => (
                    <option key={project.id} value={project.id}>
                      {project.code} · {project.name}
                    </option>
                  ))}
                </select>
              </label>
              <label>
                <span>Assignee</span>
                <select value={filters.assigneeUserId} onChange={(event) => setFilters((current) => ({ ...current, assigneeUserId: event.target.value }))} disabled={!filters.projectId}>
                  <option value="">All assignees</option>
                  {assignableUsers.map((user) => (
                    <option key={user.id} value={user.id}>
                      {user.displayName} · {user.roleInProject}
                    </option>
                  ))}
                </select>
              </label>
              <label>
                <span>Status</span>
                <select value={filters.status} onChange={(event) => setFilters((current) => ({ ...current, status: event.target.value }))}>
                  <option value="">All statuses</option>
                  {taskStatusOptions.map((status) => (
                    <option key={status} value={status}>{status}</option>
                  ))}
                </select>
              </label>
              <label className="checkbox-row">
                <input type="checkbox" checked={filters.overdueOnly} onChange={(event) => setFilters((current) => ({ ...current, overdueOnly: event.target.checked }))} />
                <span>Overdue only</span>
              </label>
            </div>
            <div className="form-actions">
              <button className="primary-button" type="submit" disabled={isRefreshing}>
                {isRefreshing ? 'Applying...' : 'Apply filters'}
              </button>
              <button
                className="secondary-button"
                type="button"
                onClick={() => {
                  setFilters(emptyTaskFilters())
                  void loadInitialData()
                }}
              >
                Clear filters
              </button>
            </div>
          </form>

          <div className="entity-list">
            {tasks.map((task) => (
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
            {tasks.length === 0 && !isLoading ? <p className="empty-state">No tasks matched the current filters.</p> : null}
          </div>
        </article>
      </section>
    </section>
  )
}

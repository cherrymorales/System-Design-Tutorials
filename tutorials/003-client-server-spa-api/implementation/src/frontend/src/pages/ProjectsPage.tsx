import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { fetchJson } from '../api'
import { emptyProjectForm, type ProjectDetail, type ProjectFormState, type ProjectSummary, type UserSession, type WorkspaceUser } from '../models'
import { handlePageError, statusBadgeClass } from '../lib/ui'

const ROLE_WORKSPACE_ADMIN = 'WorkspaceAdmin'
const ROLE_PROJECT_MANAGER = 'ProjectManager'

type ProjectsPageProps = {
  session: UserSession
  onSessionExpired: () => void
}

export function ProjectsPage({ session, onSessionExpired }: ProjectsPageProps) {
  const navigate = useNavigate()
  const [projects, setProjects] = useState<ProjectSummary[]>([])
  const [workspaceUsers, setWorkspaceUsers] = useState<WorkspaceUser[]>([])
  const [form, setForm] = useState<ProjectFormState>(emptyProjectForm())
  const [isLoading, setIsLoading] = useState(true)
  const [isRefreshing, setIsRefreshing] = useState(false)
  const [busyAction, setBusyAction] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  const canCreateProjects = session.roles.includes(ROLE_WORKSPACE_ADMIN) || session.roles.includes(ROLE_PROJECT_MANAGER)

  useEffect(() => {
    void loadProjects()
  }, [])

  async function loadProjects(refresh = false) {
    if (refresh) setIsRefreshing(true)
    else setIsLoading(true)

    setError(null)

    try {
      const [projectsPayload, workspaceUsersPayload] = await Promise.all([
        fetchJson<ProjectSummary[]>('/api/projects'),
        canCreateProjects ? fetchJson<WorkspaceUser[]>('/api/users/workspace') : Promise.resolve([]),
      ])

      setProjects(projectsPayload)
      setWorkspaceUsers(workspaceUsersPayload)
    } catch (loadError) {
      handlePageError(loadError, onSessionExpired, setError, 'Failed to load project data.')
    } finally {
      setIsLoading(false)
      setIsRefreshing(false)
    }
  }

  async function handleCreateProject(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setBusyAction('create-project')
    setError(null)

    try {
      const createdProject = await fetchJson<ProjectDetail>('/api/projects', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          ...form,
          targetDate: form.targetDate || null,
        }),
      })
      setForm(emptyProjectForm())
      navigate(`/projects/${createdProject.id}`)
    } catch (submitError) {
      handlePageError(submitError, onSessionExpired, setError, 'Failed to create the project.')
    } finally {
      setBusyAction(null)
    }
  }

  return (
    <section className="page-section">
      <div className="section-heading">
        <div>
          <p className="eyebrow">Projects</p>
          <h2>Project portfolio</h2>
        </div>
        <button className="secondary-button" onClick={() => void loadProjects(true)} disabled={isRefreshing}>
          {isRefreshing ? 'Refreshing...' : 'Refresh'}
        </button>
      </div>

      {error ? <p className="banner banner--error">{error}</p> : null}
      {isLoading ? <p className="banner">Loading projects...</p> : null}

      <section className="panel-grid panel-grid--two">
        <article className="panel">
          <div className="panel-heading">
            <div>
              <p className="eyebrow">Portfolio</p>
              <h3>Accessible projects</h3>
            </div>
            <span className="pill">{projects.length} records</span>
          </div>
          {projects.length === 0 ? <p className="empty-state">No accessible projects are available for this user.</p> : null}
          <div className="entity-list">
            {projects.map((project) => (
              <Link key={project.id} className="entity-card entity-card--stacked" to={`/projects/${project.id}`}>
                <div>
                  <strong>{project.name}</strong>
                  <p>{project.code} · owner {project.ownerDisplayName}</p>
                  <small>{project.description}</small>
                </div>
                <div className="card-summary">
                  <span className={statusBadgeClass(project.status)}>{project.status}</span>
                  <span>{project.openTaskCount}/{project.totalTaskCount} open tasks</span>
                  <span>{project.overdueTaskCount} overdue</span>
                </div>
              </Link>
            ))}
          </div>
        </article>

        <article className="panel">
          <div className="panel-heading">
            <div>
              <p className="eyebrow">New project</p>
              <h3>Create project</h3>
            </div>
          </div>

          {canCreateProjects ? (
            <form className="entity-form" onSubmit={handleCreateProject}>
              <div className="form-grid">
                <label>
                  <span>Name</span>
                  <input value={form.name} onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))} />
                </label>
                <label>
                  <span>Code</span>
                  <input value={form.code} onChange={(event) => setForm((current) => ({ ...current, code: event.target.value }))} />
                </label>
                <label className="form-grid__full">
                  <span>Description</span>
                  <textarea value={form.description} onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))} />
                </label>
                <label>
                  <span>Start date</span>
                  <input type="date" value={form.startDate} onChange={(event) => setForm((current) => ({ ...current, startDate: event.target.value }))} />
                </label>
                <label>
                  <span>Target date</span>
                  <input type="date" value={form.targetDate} onChange={(event) => setForm((current) => ({ ...current, targetDate: event.target.value }))} />
                </label>
              </div>
              <div className="form-actions">
                <button className="primary-button" type="submit" disabled={busyAction !== null}>
                  Create project
                </button>
              </div>
            </form>
          ) : (
            <p className="banner">Only workspace admins and project managers can create projects in this tutorial.</p>
          )}

          {workspaceUsers.length > 0 ? (
            <>
              <p className="section-note">Workspace directory available for membership management:</p>
              <div className="directory-list">
                {workspaceUsers.map((user) => (
                  <div key={user.id} className="directory-row">
                    <div>
                      <strong>{user.displayName}</strong>
                      <p>{user.email}</p>
                    </div>
                    <span className="pill">{user.roles.join(', ')}</span>
                  </div>
                ))}
              </div>
            </>
          ) : null}
        </article>
      </section>
    </section>
  )
}

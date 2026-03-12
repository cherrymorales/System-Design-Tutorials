import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { fetchJson } from '../api'
import {
  emptyProjectForm,
  emptyProjectMemberForm,
  emptyTaskForm,
  projectMemberRoleOptions,
  type ProjectDetail,
  type ProjectFormState,
  type ProjectMemberFormState,
  type ProjectTaskDetail,
  type TaskFormState,
  type UserOption,
  type UserSession,
  type WorkspaceUser,
} from '../models'
import { formatDateTime, formatDateValue, getProjectActions, handlePageError, statusBadgeClass } from '../lib/ui'

const ROLE_WORKSPACE_ADMIN = 'WorkspaceAdmin'
const ROLE_PROJECT_MANAGER = 'ProjectManager'

type ProjectDetailPageProps = {
  session: UserSession
  onSessionExpired: () => void
}

export function ProjectDetailPage({ session, onSessionExpired }: ProjectDetailPageProps) {
  const navigate = useNavigate()
  const { projectId } = useParams()
  const [project, setProject] = useState<ProjectDetail | null>(null)
  const [workspaceUsers, setWorkspaceUsers] = useState<WorkspaceUser[]>([])
  const [assignableUsers, setAssignableUsers] = useState<UserOption[]>([])
  const [projectForm, setProjectForm] = useState<ProjectFormState>(emptyProjectForm())
  const [memberForm, setMemberForm] = useState<ProjectMemberFormState>(emptyProjectMemberForm())
  const [taskForm, setTaskForm] = useState<TaskFormState>(emptyTaskForm())
  const [isLoading, setIsLoading] = useState(true)
  const [busyAction, setBusyAction] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (projectId) {
      void loadProject(projectId)
    }
  }, [projectId])

  async function loadProject(currentProjectId: string) {
    setIsLoading(true)
    setError(null)

    try {
      const detail = await fetchJson<ProjectDetail>(`/api/projects/${currentProjectId}`)
      setProject(detail)
      setProjectForm({
        name: detail.name,
        code: detail.code,
        description: detail.description,
        startDate: detail.startDate,
        targetDate: detail.targetDate ?? '',
      })

      if (detail.canManageMembership || session.roles.includes(ROLE_WORKSPACE_ADMIN) || session.roles.includes(ROLE_PROJECT_MANAGER)) {
        setWorkspaceUsers(await fetchJson<WorkspaceUser[]>('/api/users/workspace'))
      } else {
        setWorkspaceUsers([])
      }

      if (detail.canManageTasks) {
        const users = await fetchJson<UserOption[]>(`/api/users/assignable?projectId=${currentProjectId}`)
        setAssignableUsers(users)
        setTaskForm((current) => ({
          ...current,
          assigneeUserId: current.assigneeUserId || users[0]?.id || '',
        }))
      } else {
        setAssignableUsers([])
      }
    } catch (loadError) {
      handlePageError(loadError, onSessionExpired, setError, 'Failed to load the project detail.')
    } finally {
      setIsLoading(false)
    }
  }

  async function runAction(actionKey: string, work: () => Promise<void>) {
    setBusyAction(actionKey)
    setError(null)
    try {
      await work()
      if (projectId) {
        await loadProject(projectId)
      }
    } catch (actionError) {
      handlePageError(actionError, onSessionExpired, setError, 'Project action failed.')
    } finally {
      setBusyAction(null)
    }
  }

  if (!projectId) {
    return <p className="banner banner--error">Project id is missing from the route.</p>
  }

  const projectActions = project ? getProjectActions(project.status) : []

  return (
    <section className="page-section">
      <div className="section-heading">
        <div>
          <p className="eyebrow">Project detail</p>
          <h2>{project?.name ?? 'Loading project...'}</h2>
        </div>
        <div className="form-actions">
          <button className="secondary-button" onClick={() => void loadProject(projectId)} disabled={busyAction !== null}>
            Refresh
          </button>
          <button className="ghost-button" onClick={() => navigate('/projects')}>
            Back to projects
          </button>
        </div>
      </div>

      {error ? <p className="banner banner--error">{error}</p> : null}
      {isLoading && !project ? <p className="banner">Loading project detail...</p> : null}

      {project ? (
        <>
          <section className="stats-grid">
            <article className="stat-card">
              <span className="stat-label">Status</span>
              <strong>{project.status}</strong>
              <small>{project.code}</small>
            </article>
            <article className="stat-card">
              <span className="stat-label">Members</span>
              <strong>{project.members.length}</strong>
              <small>Project access entries</small>
            </article>
            <article className="stat-card">
              <span className="stat-label">Tasks</span>
              <strong>{project.tasks.length}</strong>
              <small>Visible task records</small>
            </article>
            <article className="stat-card">
              <span className="stat-label">Owner</span>
              <strong>{project.ownerDisplayName}</strong>
              <small>Started {formatDateValue(project.startDate)}</small>
            </article>
          </section>

          <section className="panel-grid panel-grid--two">
            <article className="panel">
              <div className="panel-heading">
                <div>
                  <p className="eyebrow">Project</p>
                  <h3>Project state and metadata</h3>
                </div>
                <div className="inline-actions">
                  {projectActions.map((action) => (
                    <button
                      key={action.key}
                      className={action.primary ? 'primary-button' : 'secondary-button'}
                      disabled={busyAction !== null || !project.canManageProject}
                      onClick={() => void runAction(action.key, async () => {
                        await fetchJson(`/api/projects/${project.id}/${action.key}`, { method: 'POST' })
                      })}
                    >
                      {action.label}
                    </button>
                  ))}
                </div>
              </div>

              {project.canManageProject ? (
                <form className="entity-form" onSubmit={(event) => {
                  event.preventDefault()
                  void runAction('update-project', async () => {
                    await fetchJson(`/api/projects/${project.id}`, {
                      method: 'PUT',
                      headers: { 'Content-Type': 'application/json' },
                      body: JSON.stringify({
                        ...projectForm,
                        targetDate: projectForm.targetDate || null,
                      }),
                    })
                  })
                }}>
                  <div className="form-grid">
                    <label>
                      <span>Name</span>
                      <input value={projectForm.name} onChange={(event) => setProjectForm((current) => ({ ...current, name: event.target.value }))} />
                    </label>
                    <label>
                      <span>Code</span>
                      <input value={projectForm.code} onChange={(event) => setProjectForm((current) => ({ ...current, code: event.target.value }))} />
                    </label>
                    <label className="form-grid__full">
                      <span>Description</span>
                      <textarea value={projectForm.description} onChange={(event) => setProjectForm((current) => ({ ...current, description: event.target.value }))} />
                    </label>
                    <label>
                      <span>Start date</span>
                      <input type="date" value={projectForm.startDate} onChange={(event) => setProjectForm((current) => ({ ...current, startDate: event.target.value }))} />
                    </label>
                    <label>
                      <span>Target date</span>
                      <input type="date" value={projectForm.targetDate} onChange={(event) => setProjectForm((current) => ({ ...current, targetDate: event.target.value }))} />
                    </label>
                  </div>
                  <div className="form-actions">
                    <button className="primary-button" type="submit" disabled={busyAction !== null}>
                      Save project
                    </button>
                  </div>
                </form>
              ) : (
                <div className="detail-block">
                  <p>{project.description}</p>
                  <div className="detail-meta">
                    <span className={statusBadgeClass(project.status)}>{project.status}</span>
                    <span className="pill">Owner {project.ownerDisplayName}</span>
                    <span className="pill">Target {formatDateValue(project.targetDate)}</span>
                  </div>
                </div>
              )}
            </article>

            <article className="panel">
              <div className="panel-heading">
                <div>
                  <p className="eyebrow">Membership</p>
                  <h3>Project access</h3>
                </div>
                <span className="pill">{project.members.length} members</span>
              </div>

              {project.canManageMembership ? (
                <form className="entity-form" onSubmit={(event: FormEvent<HTMLFormElement>) => {
                  event.preventDefault()
                  void runAction('add-member', async () => {
                    await fetchJson(`/api/projects/${project.id}/members`, {
                      method: 'POST',
                      headers: { 'Content-Type': 'application/json' },
                      body: JSON.stringify(memberForm),
                    })
                    setMemberForm(emptyProjectMemberForm())
                  })
                }}>
                  <div className="form-grid">
                    <label>
                      <span>Workspace user</span>
                      <select value={memberForm.userId} onChange={(event) => setMemberForm((current) => ({ ...current, userId: event.target.value }))}>
                        <option value="">Select user</option>
                        {workspaceUsers.map((user) => (
                          <option key={user.id} value={user.id}>
                            {user.displayName} · {user.email}
                          </option>
                        ))}
                      </select>
                    </label>
                    <label>
                      <span>Role in project</span>
                      <select value={memberForm.roleInProject} onChange={(event) => setMemberForm((current) => ({ ...current, roleInProject: event.target.value }))}>
                        {projectMemberRoleOptions.map((role) => (
                          <option key={role} value={role}>{role}</option>
                        ))}
                      </select>
                    </label>
                  </div>
                  <div className="form-actions">
                    <button className="primary-button" type="submit" disabled={busyAction !== null || !memberForm.userId}>
                      Add or update member
                    </button>
                  </div>
                </form>
              ) : (
                <p className="banner">This account can view membership but cannot change project access.</p>
              )}

              <div className="entity-list">
                {project.members.map((member) => (
                  <div key={member.id} className="entity-card">
                    <div>
                      <strong>{member.displayName}</strong>
                      <p>{member.email}</p>
                      <small>{member.roleInProject} · joined {formatDateTime(member.joinedAt)}</small>
                    </div>
                    {project.canManageMembership ? (
                      <button
                        className="ghost-button"
                        disabled={busyAction !== null || member.userId === project.ownerUserId}
                        onClick={() => void runAction(`remove-member-${member.id}`, async () => {
                          await fetchJson(`/api/projects/${project.id}/members/${member.id}`, { method: 'DELETE' })
                        })}
                      >
                        Remove
                      </button>
                    ) : null}
                  </div>
                ))}
              </div>
            </article>
          </section>

          <section className="panel-grid panel-grid--single">
            <article className="panel">
              <div className="panel-heading">
                <div>
                  <p className="eyebrow">Tasks</p>
                  <h3>Project work items</h3>
                </div>
                <span className="pill">{project.tasks.length} tasks</span>
              </div>

              {project.canManageTasks ? (
                <form className="entity-form" onSubmit={(event: FormEvent<HTMLFormElement>) => {
                  event.preventDefault()
                  void runAction('create-task', async () => {
                    const createdTask = await fetchJson<ProjectTaskDetail>('/api/tasks', {
                      method: 'POST',
                      headers: { 'Content-Type': 'application/json' },
                      body: JSON.stringify({
                        projectId: project.id,
                        title: taskForm.title,
                        description: taskForm.description,
                        assigneeUserId: taskForm.assigneeUserId,
                        priority: taskForm.priority,
                        dueDate: taskForm.dueDate || null,
                      }),
                    })
                    setTaskForm((current) => ({ ...emptyTaskForm(), assigneeUserId: current.assigneeUserId }))
                    navigate(`/tasks/${createdTask.id}`)
                  })
                }}>
                  <div className="form-grid">
                    <label>
                      <span>Title</span>
                      <input value={taskForm.title} onChange={(event) => setTaskForm((current) => ({ ...current, title: event.target.value }))} />
                    </label>
                    <label>
                      <span>Assignee</span>
                      <select value={taskForm.assigneeUserId} onChange={(event) => setTaskForm((current) => ({ ...current, assigneeUserId: event.target.value }))}>
                        <option value="">Select assignee</option>
                        {assignableUsers.map((user) => (
                          <option key={user.id} value={user.id}>
                            {user.displayName} · {user.roleInProject}
                          </option>
                        ))}
                      </select>
                    </label>
                    <label className="form-grid__full">
                      <span>Description</span>
                      <textarea value={taskForm.description} onChange={(event) => setTaskForm((current) => ({ ...current, description: event.target.value }))} />
                    </label>
                    <label>
                      <span>Priority</span>
                      <select value={taskForm.priority} onChange={(event) => setTaskForm((current) => ({ ...current, priority: event.target.value }))}>
                        {['Low', 'Medium', 'High', 'Critical'].map((priority) => (
                          <option key={priority} value={priority}>{priority}</option>
                        ))}
                      </select>
                    </label>
                    <label>
                      <span>Due date</span>
                      <input type="date" value={taskForm.dueDate} onChange={(event) => setTaskForm((current) => ({ ...current, dueDate: event.target.value }))} />
                    </label>
                  </div>
                  <div className="form-actions">
                    <button className="primary-button" type="submit" disabled={busyAction !== null || !taskForm.assigneeUserId}>
                      Create task
                    </button>
                  </div>
                </form>
              ) : (
                <p className="banner">Task creation is limited to the project owner or a workspace admin.</p>
              )}

              <div className="entity-list">
                {project.tasks.map((task) => (
                  <Link key={task.id} className="entity-card entity-card--stacked" to={`/tasks/${task.id}`}>
                    <div>
                      <strong>{task.title}</strong>
                      <p>{task.assigneeDisplayName}</p>
                      <small>Due {formatDateValue(task.dueDate)} · Updated {formatDateTime(task.updatedAt)}</small>
                    </div>
                    <div className="card-summary">
                      <span className={statusBadgeClass(task.status)}>{task.status}</span>
                      <span className="pill">{task.priority}</span>
                      {task.availableActions.length > 0 ? <span>{task.availableActions.join(', ')}</span> : null}
                    </div>
                  </Link>
                ))}
              </div>
            </article>
          </section>
        </>
      ) : null}
    </section>
  )
}

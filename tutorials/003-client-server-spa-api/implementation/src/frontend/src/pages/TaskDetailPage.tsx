import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { fetchJson } from '../api'
import { emptyTaskForm, taskPriorityOptions, type ProjectTaskDetail, type TaskFormState, type UserOption } from '../models'
import { formatDateTime, formatDateValue, formatTaskActionLabel, handlePageError, statusBadgeClass } from '../lib/ui'

type TaskDetailPageProps = {
  onSessionExpired: () => void
}

export function TaskDetailPage({ onSessionExpired }: TaskDetailPageProps) {
  const navigate = useNavigate()
  const { taskId } = useParams()
  const [task, setTask] = useState<ProjectTaskDetail | null>(null)
  const [assignableUsers, setAssignableUsers] = useState<UserOption[]>([])
  const [taskForm, setTaskForm] = useState<TaskFormState>(emptyTaskForm())
  const [commentBody, setCommentBody] = useState('')
  const [editingCommentId, setEditingCommentId] = useState<string | null>(null)
  const [editingCommentBody, setEditingCommentBody] = useState('')
  const [isLoading, setIsLoading] = useState(true)
  const [busyAction, setBusyAction] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (taskId) {
      void loadTask(taskId)
    }
  }, [taskId])

  async function loadTask(currentTaskId: string) {
    setIsLoading(true)
    setError(null)

    try {
      const detail = await fetchJson<ProjectTaskDetail>(`/api/tasks/${currentTaskId}`)
      setTask(detail)
      setTaskForm({
        title: detail.title,
        description: detail.description,
        assigneeUserId: detail.assigneeUserId,
        priority: detail.priority,
        dueDate: detail.dueDate ?? '',
      })

      if (detail.canEditTask) {
        setAssignableUsers(await fetchJson<UserOption[]>(`/api/users/assignable?projectId=${detail.projectId}`))
      } else {
        setAssignableUsers([])
      }
    } catch (loadError) {
      handlePageError(loadError, onSessionExpired, setError, 'Failed to load task detail.')
    } finally {
      setIsLoading(false)
    }
  }

  async function runTaskAction(actionKey: string, work: () => Promise<void>) {
    setBusyAction(actionKey)
    setError(null)
    try {
      await work()
      if (taskId) {
        await loadTask(taskId)
      }
    } catch (actionError) {
      handlePageError(actionError, onSessionExpired, setError, 'Task action failed.')
    } finally {
      setBusyAction(null)
    }
  }

  if (!taskId) {
    return <p className="banner banner--error">Task id is missing from the route.</p>
  }

  return (
    <section className="page-section">
      <div className="section-heading">
        <div>
          <p className="eyebrow">Task detail</p>
          <h2>{task?.title ?? 'Loading task...'}</h2>
        </div>
        <div className="form-actions">
          <button className="secondary-button" onClick={() => void loadTask(taskId)} disabled={busyAction !== null}>
            Refresh
          </button>
          {task ? (
            <button className="ghost-button" onClick={() => navigate(`/projects/${task.projectId}`)}>
              Back to project
            </button>
          ) : null}
        </div>
      </div>

      {error ? <p className="banner banner--error">{error}</p> : null}
      {isLoading && !task ? <p className="banner">Loading task detail...</p> : null}

      {task ? (
        <>
          <section className="stats-grid">
            <article className="stat-card">
              <span className="stat-label">Status</span>
              <strong>{task.status}</strong>
              <small>{task.projectCode}</small>
            </article>
            <article className="stat-card">
              <span className="stat-label">Priority</span>
              <strong>{task.priority}</strong>
              <small>Assigned to {task.assigneeDisplayName}</small>
            </article>
            <article className="stat-card">
              <span className="stat-label">Due date</span>
              <strong>{formatDateValue(task.dueDate)}</strong>
              <small>Updated {formatDateTime(task.updatedAt)}</small>
            </article>
            <article className="stat-card">
              <span className="stat-label">Comments</span>
              <strong>{task.comments.length}</strong>
              <small>{task.activity.length} activity entries</small>
            </article>
          </section>

          <section className="panel-grid panel-grid--two">
            <article className="panel">
              <div className="panel-heading">
                <div>
                  <p className="eyebrow">Task</p>
                  <h3>Task state and edit form</h3>
                </div>
                <div className="inline-actions">
                  {task.availableActions.map((action) => (
                    <button
                      key={action}
                      className={action === 'complete' ? 'primary-button' : 'secondary-button'}
                      disabled={busyAction !== null}
                      onClick={() => void runTaskAction(action, async () => {
                        const body = action === 'block'
                          ? JSON.stringify({ blockerNote: 'Blocked from the UI pending dependency review.' })
                          : undefined
                        await fetchJson(`/api/tasks/${task.id}/${action}`, {
                          method: 'POST',
                          headers: body ? { 'Content-Type': 'application/json' } : undefined,
                          body,
                        })
                      })}
                    >
                      {formatTaskActionLabel(action)}
                    </button>
                  ))}
                </div>
              </div>

              <div className="detail-block">
                <p>{task.description}</p>
                <div className="detail-meta">
                  <span className={statusBadgeClass(task.status)}>{task.status}</span>
                  <span className="pill">Project {task.projectCode}</span>
                  <span className="pill">Created by {task.createdByDisplayName}</span>
                  {task.blockerNote ? <span className="pill pill--warning">Blocked: {task.blockerNote}</span> : null}
                </div>
              </div>

              {task.canEditTask ? (
                <form className="entity-form" onSubmit={(event: FormEvent<HTMLFormElement>) => {
                  event.preventDefault()
                  void runTaskAction('update-task', async () => {
                    await fetchJson(`/api/tasks/${task.id}`, {
                      method: 'PUT',
                      headers: { 'Content-Type': 'application/json' },
                      body: JSON.stringify({
                        title: taskForm.title,
                        description: taskForm.description,
                        assigneeUserId: taskForm.assigneeUserId,
                        priority: taskForm.priority,
                        dueDate: taskForm.dueDate || null,
                      }),
                    })
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
                        {taskPriorityOptions.map((priority) => (
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
                    <button className="primary-button" type="submit" disabled={busyAction !== null}>
                      Save task
                    </button>
                  </div>
                </form>
              ) : (
                <p className="banner">This account cannot edit the task structure. Workflow actions remain server-controlled.</p>
              )}
            </article>

            <article className="panel">
              <div className="panel-heading">
                <div>
                  <p className="eyebrow">Comments</p>
                  <h3>Discussion thread</h3>
                </div>
                <span className="pill">{task.comments.length} comments</span>
              </div>

              {task.canComment ? (
                <form className="entity-form" onSubmit={(event: FormEvent<HTMLFormElement>) => {
                  event.preventDefault()
                  void runTaskAction('add-comment', async () => {
                    await fetchJson(`/api/tasks/${task.id}/comments`, {
                      method: 'POST',
                      headers: { 'Content-Type': 'application/json' },
                      body: JSON.stringify({ body: commentBody }),
                    })
                    setCommentBody('')
                  })
                }}>
                  <label>
                    <span>Add comment</span>
                    <textarea value={commentBody} onChange={(event) => setCommentBody(event.target.value)} />
                  </label>
                  <div className="form-actions">
                    <button className="primary-button" type="submit" disabled={busyAction !== null || !commentBody.trim()}>
                      Post comment
                    </button>
                  </div>
                </form>
              ) : (
                <p className="banner">This role is read-only for comments on the current task.</p>
              )}

              <div className="timeline-list">
                {task.comments.map((comment) => (
                  <article key={comment.id} className="timeline-item">
                    <header className="timeline-header">
                      <strong>{comment.authorDisplayName}</strong>
                      <small>{formatDateTime(comment.updatedAt)}</small>
                    </header>
                    {editingCommentId === comment.id ? (
                      <form className="entity-form" onSubmit={(event: FormEvent<HTMLFormElement>) => {
                        event.preventDefault()
                        void runTaskAction(`edit-comment-${comment.id}`, async () => {
                          await fetchJson(`/api/tasks/${task.id}/comments/${comment.id}`, {
                            method: 'PUT',
                            headers: { 'Content-Type': 'application/json' },
                            body: JSON.stringify({ body: editingCommentBody }),
                          })
                          setEditingCommentId(null)
                          setEditingCommentBody('')
                        })
                      }}>
                        <textarea value={editingCommentBody} onChange={(event) => setEditingCommentBody(event.target.value)} />
                        <div className="form-actions">
                          <button className="primary-button" type="submit" disabled={busyAction !== null || !editingCommentBody.trim()}>
                            Save comment
                          </button>
                          <button
                            className="secondary-button"
                            type="button"
                            onClick={() => {
                              setEditingCommentId(null)
                              setEditingCommentBody('')
                            }}
                          >
                            Cancel
                          </button>
                        </div>
                      </form>
                    ) : (
                      <>
                        <p>{comment.body}</p>
                        {comment.canEdit ? (
                          <button
                            className="secondary-button"
                            onClick={() => {
                              setEditingCommentId(comment.id)
                              setEditingCommentBody(comment.body)
                            }}
                          >
                            Edit
                          </button>
                        ) : null}
                      </>
                    )}
                  </article>
                ))}
                {task.comments.length === 0 ? <p className="empty-state">No comments have been added yet.</p> : null}
              </div>
            </article>
          </section>

          <section className="panel-grid panel-grid--single">
            <article className="panel">
              <div className="panel-heading">
                <div>
                  <p className="eyebrow">Activity</p>
                  <h3>Workflow timeline</h3>
                </div>
                <span className="pill">{task.activity.length} entries</span>
              </div>
              <div className="timeline-list">
                {task.activity.map((entry) => (
                  <article key={entry.id} className="timeline-item">
                    <header className="timeline-header">
                      <strong>{entry.actorDisplayName}</strong>
                      <small>{formatDateTime(entry.createdAt)}</small>
                    </header>
                    <p>{entry.summary}</p>
                    <span className="pill">{entry.type}</span>
                  </article>
                ))}
              </div>
            </article>
          </section>
        </>
      ) : null}
    </section>
  )
}

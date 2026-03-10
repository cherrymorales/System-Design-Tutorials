import { isApiError } from '../api'
import type { TaskFiltersState } from '../models'

export function navClassName(isActive: boolean) {
  return isActive ? 'nav-link nav-link--active' : 'nav-link'
}

export function handlePageError(
  error: unknown,
  onSessionExpired: () => void,
  setError: (value: string | null) => void,
  fallbackMessage: string,
) {
  if (isApiError(error) && error.status === 401) {
    onSessionExpired()
    return
  }

  setError(error instanceof Error ? error.message : fallbackMessage)
}

export function buildTaskQuery(filters: TaskFiltersState) {
  const params = new URLSearchParams()
  if (filters.projectId) params.set('projectId', filters.projectId)
  if (filters.assigneeUserId) params.set('assigneeUserId', filters.assigneeUserId)
  if (filters.status) params.set('status', filters.status)
  if (filters.overdueOnly) params.set('overdueOnly', 'true')
  return params.toString()
}

export function getProjectActions(status: string) {
  switch (status) {
    case 'Planned':
      return [{ key: 'activate', label: 'Activate', primary: true }]
    case 'Active':
      return [
        { key: 'mark-at-risk', label: 'Mark at risk', primary: false },
        { key: 'complete', label: 'Complete', primary: true },
      ]
    case 'AtRisk':
      return [
        { key: 'activate', label: 'Return active', primary: false },
        { key: 'complete', label: 'Complete', primary: true },
      ]
    case 'Completed':
      return [{ key: 'archive', label: 'Archive', primary: false }]
    default:
      return []
  }
}

export function formatTaskActionLabel(action: string) {
  switch (action) {
    case 'submit-review':
      return 'Submit for review'
    case 'complete':
      return 'Complete'
    case 'cancel':
      return 'Cancel'
    case 'start':
      return 'Start'
    case 'block':
      return 'Block'
    default:
      return action
  }
}

export function formatDateValue(value: string | null | undefined) {
  if (!value) return 'No date'

  const parsed = new Date(value)
  if (Number.isNaN(parsed.getTime())) {
    return value
  }

  return parsed.toLocaleDateString()
}

export function formatDateTime(value: string) {
  return new Date(value).toLocaleString()
}

export function statusBadgeClass(status: string) {
  const normalized = status.toLowerCase()
  if (normalized.includes('risk') || normalized.includes('blocked')) return 'pill pill--warning'
  if (normalized.includes('done') || normalized.includes('completed') || normalized.includes('active') || normalized.includes('review')) return 'pill pill--active'
  if (normalized.includes('archived') || normalized.includes('cancelled')) return 'pill pill--archived'
  return 'pill'
}

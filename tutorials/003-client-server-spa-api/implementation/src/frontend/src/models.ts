export type HealthResponse = {
  status: string
  service: string
  timestamp: string
}

export type UserSession = {
  displayName: string
  email: string
  roles: string[]
}

export type DashboardSummary = {
  activeProjectCount: number
  atRiskProjectCount: number
  overdueTaskCount: number
  tasksInReviewCount: number
  myOpenTaskCount: number
}

export type ProjectSummary = {
  id: string
  name: string
  code: string
  description: string
  status: string
  ownerUserId: string
  ownerDisplayName: string
  startDate: string
  targetDate: string | null
  memberCount: number
  totalTaskCount: number
  openTaskCount: number
  overdueTaskCount: number
}

export type ProjectMember = {
  id: string
  userId: string
  displayName: string
  email: string
  roleInProject: string
  joinedAt: string
}

export type ProjectTaskSummary = {
  id: string
  projectId: string
  projectName: string
  projectCode: string
  title: string
  status: string
  priority: string
  assigneeUserId: string
  assigneeDisplayName: string
  dueDate: string | null
  isOverdue: boolean
  availableActions: string[]
  updatedAt: string
}

export type ProjectDetail = {
  id: string
  name: string
  code: string
  description: string
  status: string
  ownerUserId: string
  ownerDisplayName: string
  startDate: string
  targetDate: string | null
  completedAt: string | null
  canManageProject: boolean
  canManageMembership: boolean
  canManageTasks: boolean
  members: ProjectMember[]
  tasks: ProjectTaskSummary[]
}

export type TaskComment = {
  id: string
  taskId: string
  authorUserId: string
  authorDisplayName: string
  body: string
  createdAt: string
  updatedAt: string
  canEdit: boolean
}

export type TaskActivity = {
  id: string
  type: string
  actorDisplayName: string
  summary: string
  createdAt: string
}

export type ProjectTaskDetail = {
  id: string
  projectId: string
  projectName: string
  projectCode: string
  projectStatus: string
  title: string
  description: string
  status: string
  priority: string
  assigneeUserId: string
  assigneeDisplayName: string
  createdByUserId: string
  createdByDisplayName: string
  blockerNote: string | null
  dueDate: string | null
  completedAt: string | null
  updatedAt: string
  canEditTask: boolean
  canComment: boolean
  canUpdateWorkflow: boolean
  availableActions: string[]
  comments: TaskComment[]
  activity: TaskActivity[]
}

export type UserOption = {
  id: string
  displayName: string
  email: string
  roleInProject: string
}

export type WorkspaceUser = {
  id: string
  displayName: string
  email: string
  roles: string[]
}

export type LoginFormState = {
  email: string
  password: string
}

export type ProjectFormState = {
  name: string
  code: string
  description: string
  startDate: string
  targetDate: string
}

export type ProjectMemberFormState = {
  userId: string
  roleInProject: string
}

export type TaskFormState = {
  title: string
  description: string
  assigneeUserId: string
  priority: string
  dueDate: string
}

export type TaskFiltersState = {
  projectId: string
  assigneeUserId: string
  status: string
  overdueOnly: boolean
}

export const seededUsers = [
  { email: 'admin@clientserverspa.local', label: 'Workspace Admin', role: 'WorkspaceAdmin' },
  { email: 'manager@clientserverspa.local', label: 'Project Manager', role: 'ProjectManager' },
  { email: 'alex@clientserverspa.local', label: 'Alex Contributor', role: 'Contributor' },
  { email: 'viewer@clientserverspa.local', label: 'Project Viewer', role: 'Viewer' },
] as const

export const projectMemberRoleOptions = ['ProjectManager', 'Contributor', 'Viewer'] as const
export const taskPriorityOptions = ['Low', 'Medium', 'High', 'Critical'] as const
export const taskStatusOptions = ['Backlog', 'InProgress', 'Blocked', 'InReview', 'Done', 'Cancelled'] as const

export const defaultLoginForm: LoginFormState = {
  email: seededUsers[1].email,
  password: 'Password123!',
}

export const emptyProjectForm = (): ProjectFormState => ({
  name: '',
  code: '',
  description: '',
  startDate: '',
  targetDate: '',
})

export const emptyProjectMemberForm = (): ProjectMemberFormState => ({
  userId: '',
  roleInProject: 'Contributor',
})

export const emptyTaskForm = (): TaskFormState => ({
  title: '',
  description: '',
  assigneeUserId: '',
  priority: 'Medium',
  dueDate: '',
})

export const emptyTaskFilters = (): TaskFiltersState => ({
  projectId: '',
  assigneeUserId: '',
  status: '',
  overdueOnly: false,
})

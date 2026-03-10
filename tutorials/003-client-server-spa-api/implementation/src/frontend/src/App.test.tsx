import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { vi } from 'vitest'
import App from './App'

describe('App', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
  })

  it('renders the login page when session restoration fails', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(null, { status: 401 }))

    renderWithRoute('/login')

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /project delivery workspace/i })).toBeInTheDocument()
    })
  })

  it('updates the email field when a seeded account card is selected', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(null, { status: 401 }))

    renderWithRoute('/login')

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /project delivery workspace/i })).toBeInTheDocument()
    })

    fireEvent.click(screen.getByRole('button', { name: /alex contributor/i }))

    expect(screen.getByLabelText(/email/i)).toHaveValue('alex@clientserverspa.local')
  })

  it('renders the dashboard route with summary and my work data', async () => {
    mockFetch((input) => {
      const url = toPath(input)

      if (url === '/api/auth/me') {
        return jsonResponse({
          displayName: 'Project Manager',
          email: 'manager@clientserverspa.local',
          roles: ['ProjectManager'],
        })
      }

      if (url === '/api/health') {
        return jsonResponse({
          status: 'ok',
          service: '003-client-server-spa-api',
          timestamp: '2026-03-10T08:00:00Z',
        })
      }

      if (url === '/api/dashboard/summary') {
        return jsonResponse({
          activeProjectCount: 1,
          atRiskProjectCount: 1,
          overdueTaskCount: 1,
          tasksInReviewCount: 1,
          myOpenTaskCount: 2,
        })
      }

      if (url === '/api/dashboard/my-work') {
        return jsonResponse([
          {
            id: 'task-1',
            projectId: 'project-1',
            projectName: 'Apollo Client Portal',
            projectCode: 'APOLLO-PORTAL',
            title: 'Implement dashboard filters',
            status: 'InReview',
            priority: 'High',
            assigneeUserId: 'user-1',
            assigneeDisplayName: 'Alex Contributor',
            dueDate: '2026-03-17',
            isOverdue: false,
            availableActions: ['complete'],
            updatedAt: '2026-03-10T08:00:00Z',
          },
        ])
      }

      return new Response(null, { status: 404 })
    })

    renderWithRoute('/')

    await waitFor(() => {
      expect(screen.getByText('Implement dashboard filters')).toBeInTheDocument()
    })

    expect(screen.getByText('Active projects')).toBeInTheDocument()
  })

  it('renders project detail and task detail routes from API data', async () => {
    mockFetch((input) => {
      const url = toPath(input)

      if (url === '/api/auth/me') {
        return jsonResponse({
          displayName: 'Project Manager',
          email: 'manager@clientserverspa.local',
          roles: ['ProjectManager'],
        })
      }

      if (url === '/api/health') {
        return jsonResponse({
          status: 'ok',
          service: '003-client-server-spa-api',
          timestamp: '2026-03-10T08:00:00Z',
        })
      }

      if (url === '/api/projects/project-1') {
        return jsonResponse({
          id: 'project-1',
          name: 'Apollo Client Portal',
          code: 'APOLLO-PORTAL',
          description: 'Client portal project.',
          status: 'Active',
          ownerUserId: 'owner-1',
          ownerDisplayName: 'Project Manager',
          startDate: '2026-03-01',
          targetDate: '2026-06-30',
          completedAt: null,
          canManageProject: true,
          canManageMembership: true,
          canManageTasks: true,
          members: [
            {
              id: 'member-1',
              userId: 'owner-1',
              displayName: 'Project Manager',
              email: 'manager@clientserverspa.local',
              roleInProject: 'ProjectManager',
              joinedAt: '2026-03-01T00:00:00Z',
            },
          ],
          tasks: [
            {
              id: 'task-1',
              projectId: 'project-1',
              projectName: 'Apollo Client Portal',
              projectCode: 'APOLLO-PORTAL',
              title: 'Implement dashboard filters',
              status: 'InReview',
              priority: 'High',
              assigneeUserId: 'user-1',
              assigneeDisplayName: 'Alex Contributor',
              dueDate: '2026-03-17',
              isOverdue: false,
              availableActions: ['complete'],
              updatedAt: '2026-03-10T08:00:00Z',
            },
          ],
        })
      }

      if (url === '/api/users/workspace') {
        return jsonResponse([
          {
            id: 'owner-1',
            displayName: 'Project Manager',
            email: 'manager@clientserverspa.local',
            roles: ['ProjectManager'],
          },
        ])
      }

      if (url === '/api/users/assignable?projectId=project-1') {
        return jsonResponse([
          {
            id: 'user-1',
            displayName: 'Alex Contributor',
            email: 'alex@clientserverspa.local',
            roleInProject: 'Contributor',
          },
        ])
      }

      if (url === '/api/tasks/task-1') {
        return jsonResponse({
          id: 'task-1',
          projectId: 'project-1',
          projectName: 'Apollo Client Portal',
          projectCode: 'APOLLO-PORTAL',
          projectStatus: 'Active',
          title: 'Implement dashboard filters',
          description: 'Add dashboard filters to the SPA.',
          status: 'InReview',
          priority: 'High',
          assigneeUserId: 'user-1',
          assigneeDisplayName: 'Alex Contributor',
          createdByUserId: 'owner-1',
          createdByDisplayName: 'Project Manager',
          blockerNote: null,
          dueDate: '2026-03-17',
          completedAt: null,
          updatedAt: '2026-03-10T08:00:00Z',
          canEditTask: false,
          canComment: false,
          canUpdateWorkflow: false,
          availableActions: [],
          comments: [],
          activity: [],
        })
      }

      return new Response(null, { status: 404 })
    })

    const { unmount } = render(
      <MemoryRouter initialEntries={['/projects/project-1']}>
        <App />
      </MemoryRouter>,
    )

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /apollo client portal/i })).toBeInTheDocument()
    })

    expect(screen.getByRole('heading', { name: /project access/i })).toBeInTheDocument()
    expect(screen.getByText(/implement dashboard filters/i)).toBeInTheDocument()

    unmount()

    renderWithRoute('/tasks/task-1')

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /implement dashboard filters/i })).toBeInTheDocument()
    })

    expect(screen.getByText(/read-only for comments/i)).toBeInTheDocument()
  })
})

function renderWithRoute(route: string) {
  return render(
    <MemoryRouter initialEntries={[route]}>
      <App />
    </MemoryRouter>,
  )
}

function mockFetch(handler: (input: RequestInfo | URL) => Response) {
  vi.spyOn(globalThis, 'fetch').mockImplementation((input) => Promise.resolve(handler(input)))
}

function toPath(input: RequestInfo | URL) {
  if (typeof input === 'string') {
    return input
  }

  if (input instanceof URL) {
    return `${input.pathname}${input.search}`
  }

  return input.url
}

function jsonResponse(payload: unknown) {
  return new Response(JSON.stringify(payload), {
    status: 200,
    headers: { 'Content-Type': 'application/json' },
  })
}

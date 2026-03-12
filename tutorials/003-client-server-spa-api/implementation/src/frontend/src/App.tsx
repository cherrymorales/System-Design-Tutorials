import { useEffect, useState } from 'react'
import { Navigate, Route, Routes } from 'react-router-dom'
import './App.css'
import { fetchJson } from './api'
import type { UserSession } from './models'
import { AppShell } from './layout/AppShell'
import { DashboardPage } from './pages/DashboardPage'
import { LoginPage } from './pages/LoginPage'
import { ProjectDetailPage } from './pages/ProjectDetailPage'
import { ProjectsPage } from './pages/ProjectsPage'
import { TaskDetailPage } from './pages/TaskDetailPage'
import { TasksPage } from './pages/TasksPage'

function App() {
  const [session, setSession] = useState<UserSession | null>(null)
  const [isRestoringSession, setIsRestoringSession] = useState(true)

  useEffect(() => {
    void restoreSession()
  }, [])

  async function restoreSession() {
    setIsRestoringSession(true)
    try {
      setSession(await fetchJson<UserSession>('/api/auth/me'))
    } catch {
      setSession(null)
    } finally {
      setIsRestoringSession(false)
    }
  }

  async function handleLogout(callApi = true) {
    if (callApi) {
      try {
        await fetchJson<void>('/api/auth/logout', { method: 'POST' })
      } catch {
        // Best effort sign-out.
      }
    }

    setSession(null)
  }

  if (isRestoringSession) {
    return (
      <main className="page-shell">
        <p className="banner">Restoring session...</p>
      </main>
    )
  }

  const onSessionExpired = () => void handleLogout(false)

  return (
    <Routes>
      <Route
        path="/login"
        element={session ? <Navigate to="/" replace /> : <LoginPage onLoggedIn={setSession} />}
      />
      {session ? (
        <Route element={<AppShell session={session} onLogout={handleLogout} />}>
          <Route index element={<DashboardPage onSessionExpired={onSessionExpired} />} />
          <Route path="/projects" element={<ProjectsPage session={session} onSessionExpired={onSessionExpired} />} />
          <Route path="/projects/:projectId" element={<ProjectDetailPage session={session} onSessionExpired={onSessionExpired} />} />
          <Route path="/tasks" element={<TasksPage onSessionExpired={onSessionExpired} />} />
          <Route path="/tasks/:taskId" element={<TaskDetailPage onSessionExpired={onSessionExpired} />} />
        </Route>
      ) : null}
      <Route path="*" element={<Navigate to={session ? '/' : '/login'} replace />} />
    </Routes>
  )
}

export default App

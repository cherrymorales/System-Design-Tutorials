import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import './App.css'
import { api } from './api'
import type { AssetDetail, AssetSummary, CurrentUser, DashboardSummary, Notification, SeedUser } from './models'

const emptyDashboard: DashboardSummary = {
  totalAssets: 0,
  processingAssets: 0,
  readyAssets: 0,
  failedAssets: 0,
  pendingUploads: 0,
  notificationsSent: 0,
}

function App() {
  const [seedUsers, setSeedUsers] = useState<SeedUser[]>([])
  const [currentUser, setCurrentUser] = useState<CurrentUser | null>(null)
  const [dashboard, setDashboard] = useState(emptyDashboard)
  const [assets, setAssets] = useState<AssetSummary[]>([])
  const [selectedAssetId, setSelectedAssetId] = useState<string | null>(null)
  const [assetDetail, setAssetDetail] = useState<AssetDetail | null>(null)
  const [notifications, setNotifications] = useState<Notification[]>([])
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('Password123!')
  const [assetKey, setAssetKey] = useState(`ASSET-${new Date().getTime().toString().slice(-6)}`)
  const [title, setTitle] = useState('Spring campaign hero video')
  const [simulateFailure, setSimulateFailure] = useState(false)
  const [error, setError] = useState('')
  const [isBusy, setIsBusy] = useState(false)

  useEffect(() => {
    void bootstrap()
  }, [])

  useEffect(() => {
    if (!currentUser) {
      return
    }

    const intervalId = window.setInterval(() => {
      void loadWorkspace()
    }, 1500)

    return () => window.clearInterval(intervalId)
  }, [currentUser, selectedAssetId])

  async function bootstrap() {
    setError('')
    try {
      const users = await api.getSeedUsers()
      setSeedUsers(users)
      if (users.length > 0) {
        setEmail(users[0].email)
      }

      try {
        const user = await api.getCurrentUser()
        setCurrentUser(user)
        await loadWorkspace()
      } catch {
        setCurrentUser(null)
      }
    } catch (requestError) {
      setError((requestError as Error).message)
    }
  }

  async function loadWorkspace() {
    const [dashboardSummary, assetList, notificationList] = await Promise.all([
      api.getDashboard(),
      api.getAssets(),
      api.getNotifications(),
    ])

    setDashboard(dashboardSummary)
    setAssets(assetList)
    setNotifications(notificationList)

    const nextSelected = selectedAssetId ?? assetList[0]?.assetId ?? null
    setSelectedAssetId(nextSelected)

    if (nextSelected) {
      try {
        setAssetDetail(await api.getAsset(nextSelected))
      } catch {
        setAssetDetail(null)
      }
    } else {
      setAssetDetail(null)
    }
  }

  async function handleLogin(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError('')
    setIsBusy(true)
    try {
      const user = await api.login(email, password)
      setCurrentUser(user)
      await loadWorkspace()
    } catch (requestError) {
      setError((requestError as Error).message)
    } finally {
      setIsBusy(false)
    }
  }

  async function handleCreateAsset(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError('')
    setIsBusy(true)
    try {
      const created = await api.createAsset({
        assetKey,
        title,
        simulateFailure,
      })
      setSelectedAssetId(created.assetId)
      setAssetDetail(created)
      setAssetKey(`ASSET-${Math.floor(Math.random() * 900000) + 100000}`)
      setTitle('New promo cut')
      setSimulateFailure(false)
      await loadWorkspace()
    } catch (requestError) {
      setError((requestError as Error).message)
    } finally {
      setIsBusy(false)
    }
  }

  async function handleUploadComplete() {
    if (!assetDetail) {
      return
    }

    setError('')
    setIsBusy(true)
    try {
      const updated = await api.markUploadComplete(assetDetail.assetId)
      setAssetDetail(updated)
      await loadWorkspace()
    } catch (requestError) {
      setError((requestError as Error).message)
    } finally {
      setIsBusy(false)
    }
  }

  async function handleLogout() {
    await api.logout()
    setCurrentUser(null)
    setDashboard(emptyDashboard)
    setAssets([])
    setNotifications([])
    setAssetDetail(null)
  }

  if (!currentUser) {
    return (
      <main className="login-shell">
        <section className="login-panel">
          <p className="eyebrow">005 Event-Driven Architecture</p>
          <h1>Digital asset processing console</h1>
          <p className="lede">
            Register uploads, trigger asynchronous processing, and monitor projection state while workers react through
            RabbitMQ. Seeded accounts all use <code>Password123!</code>.
          </p>

          <form className="login-form" onSubmit={handleLogin}>
            <label>
              Email
              <input value={email} onChange={(event) => setEmail(event.target.value)} />
            </label>
            <label>
              Password
              <input type="password" value={password} onChange={(event) => setPassword(event.target.value)} />
            </label>
            <button type="submit" className="primary-button" disabled={isBusy}>
              Sign in
            </button>
          </form>

          {error ? <p className="error-banner">{error}</p> : null}

          <div className="seed-user-grid">
            {seedUsers.map((user) => (
              <button type="button" key={user.email} className="seed-user-card" onClick={() => setEmail(user.email)}>
                <strong>{user.displayName}</strong>
                <span>{user.email}</span>
                <span>{user.role}</span>
              </button>
            ))}
          </div>
        </section>
      </main>
    )
  }

  const canOperate = currentUser.role === 'ContentOperationsCoordinator' || currentUser.role === 'OperationsManager'

  return (
    <main className="app-shell">
      <header className="app-header">
        <div>
          <p className="eyebrow">005 Event-Driven Architecture</p>
          <h1>Asset ingestion operations</h1>
        </div>
        <div className="header-actions">
          <span className="user-pill">
            {currentUser.displayName}
            <small>{currentUser.role}</small>
          </span>
          <button type="button" className="ghost-button" onClick={() => void loadWorkspace()}>
            Refresh
          </button>
          <button type="button" className="ghost-button" onClick={handleLogout}>
            Sign out
          </button>
        </div>
      </header>

      {error ? <p className="error-banner">{error}</p> : null}

      <section className="summary-grid">
        <SummaryCard label="Total assets" value={dashboard.totalAssets} />
        <SummaryCard label="Processing" value={dashboard.processingAssets} />
        <SummaryCard label="Ready" value={dashboard.readyAssets} />
        <SummaryCard label="Failed" value={dashboard.failedAssets} />
        <SummaryCard label="Pending upload" value={dashboard.pendingUploads} />
        <SummaryCard label="Notifications" value={dashboard.notificationsSent} />
      </section>

      <section className="workspace-grid">
        <section className="panel">
          <div className="panel-heading">
            <div>
              <p className="eyebrow">Asset intake</p>
              <h2>Register and submit uploads</h2>
            </div>
            <span className="status-pill neutral">{assets.length} tracked</span>
          </div>

          <form className="asset-form" onSubmit={handleCreateAsset}>
            <label>
              Asset key
              <input value={assetKey} onChange={(event) => setAssetKey(event.target.value)} />
            </label>
            <label>
              Title
              <input value={title} onChange={(event) => setTitle(event.target.value)} />
            </label>
            <label className="checkbox-row">
              <input
                type="checkbox"
                checked={simulateFailure}
                onChange={(event) => setSimulateFailure(event.target.checked)}
              />
              Simulate transcode failure
            </label>
            <div className="form-actions">
              <button type="submit" className="primary-button" disabled={!canOperate || isBusy}>
                Register asset
              </button>
            </div>
          </form>

          <div className="table-panel">
            <table>
              <thead>
                <tr>
                  <th>Asset</th>
                  <th>Lifecycle</th>
                  <th>Updated</th>
                </tr>
              </thead>
              <tbody>
                {assets.map((asset) => (
                  <tr
                    key={asset.assetId}
                    className={asset.assetId === selectedAssetId ? 'selected-row' : ''}
                    onClick={() => {
                      setSelectedAssetId(asset.assetId)
                      void api.getAsset(asset.assetId).then(setAssetDetail)
                    }}
                  >
                    <td>
                      <strong>{asset.title}</strong>
                      <span>{asset.assetKey}</span>
                    </td>
                    <td>
                      <span className={`status-pill ${statusTone(asset.lifecycleState)}`}>{formatStatus(asset.lifecycleState)}</span>
                    </td>
                    <td>{new Date(asset.updatedAt).toLocaleTimeString()}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>

        <section className="panel inspector-panel">
          <div className="panel-heading">
            <div>
              <p className="eyebrow">Projection inspector</p>
              <h2>{assetDetail?.title ?? 'Select an asset'}</h2>
            </div>
            {assetDetail ? (
              <span className={`status-pill ${statusTone(assetDetail.lifecycleState)}`}>{formatStatus(assetDetail.lifecycleState)}</span>
            ) : null}
          </div>

          {assetDetail ? (
            <div className="detail-stack">
              <section className="detail-card">
                <dl>
                  <div>
                    <dt>Asset key</dt>
                    <dd>{assetDetail.assetKey}</dd>
                  </div>
                  <div>
                    <dt>Submitted by</dt>
                    <dd>{assetDetail.submittedBy}</dd>
                  </div>
                  <div>
                    <dt>Created</dt>
                    <dd>{new Date(assetDetail.createdAt).toLocaleString()}</dd>
                  </div>
                  <div>
                    <dt>Ready at</dt>
                    <dd>{assetDetail.readyAt ? new Date(assetDetail.readyAt).toLocaleString() : 'Not ready yet'}</dd>
                  </div>
                </dl>
                {assetDetail.failureReason ? <p className="failure-copy">{assetDetail.failureReason}</p> : null}
                {assetDetail.lifecycleState === 'Registered' ? (
                  <button type="button" className="primary-button" disabled={!canOperate || isBusy} onClick={handleUploadComplete}>
                    Mark upload complete
                  </button>
                ) : null}
              </section>

              <section className="detail-card">
                <h3>Processing stages</h3>
                <div className="step-grid">
                  <StageCard label="Scan" status={assetDetail.scanStatus} />
                  <StageCard label="Metadata" status={assetDetail.metadataStatus} />
                  <StageCard label="Thumbnail" status={assetDetail.thumbnailStatus} />
                  <StageCard label="Transcode" status={assetDetail.transcodeStatus} />
                </div>
              </section>
            </div>
          ) : (
            <p className="empty-state">Select an asset to inspect the projected event-driven state.</p>
          )}
        </section>
      </section>

      <section className="panel notifications-panel">
        <div className="panel-heading">
          <div>
            <p className="eyebrow">Notifications</p>
            <h2>Ready-state messages</h2>
          </div>
        </div>
        <div className="notification-list">
          {notifications.length === 0 ? (
            <p className="empty-state">Notifications appear when an asset reaches the ready state.</p>
          ) : (
            notifications.map((notification) => (
              <article className="notification-card" key={notification.notificationId}>
                <strong>{notification.assetTitle}</strong>
                <p>{notification.message}</p>
                <span>{new Date(notification.sentAt).toLocaleString()}</span>
              </article>
            ))
          )}
        </div>
      </section>
    </main>
  )
}

function SummaryCard({ label, value }: { label: string; value: number }) {
  return (
    <article className="summary-card">
      <span>{label}</span>
      <strong>{value}</strong>
    </article>
  )
}

function StageCard({ label, status }: { label: string; status: string }) {
  return (
    <article className="step-card">
      <span>{label}</span>
      <strong className={`status-pill ${statusTone(status)}`}>{formatStatus(status)}</strong>
    </article>
  )
}

function statusTone(status: string | null) {
  if (!status) {
    return 'neutral'
  }

  const normalized = status.toLowerCase()
  if (normalized.includes('failed')) {
    return 'danger'
  }
  if (normalized.includes('ready') || normalized.includes('completed')) {
    return 'success'
  }
  if (normalized.includes('processing') || normalized.includes('uploaded')) {
    return 'warning'
  }
  return 'neutral'
}

function formatStatus(status: string) {
  return status.replace(/([a-z])([A-Z])/g, '$1 $2')
}

export default App

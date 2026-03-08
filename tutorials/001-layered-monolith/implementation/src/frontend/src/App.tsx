import { useEffect, useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import { Navigate, Route, Routes, useNavigate } from 'react-router-dom'
import './App.css'
import { fetchJson, isApiError } from './api'
import {
  createEmptyAdjustmentForm,
  createEmptyReceiptForm,
  createEmptyTransferForm,
  defaultLoginForm,
  emptyProductForm,
  emptyWarehouseForm,
  seededUsers,
  type AdjustmentFormState,
  type HealthResponse,
  type InventoryAdjustment,
  type InventoryReceipt,
  type InventorySummaryItem,
  type LoginFormState,
  type LowStockItem,
  type Product,
  type ProductFormState,
  type ReceiptFormState,
  type StockTransfer,
  type TransferFormState,
  type UserSession,
  type Warehouse,
  type WarehouseFormState,
} from './models'

const ROLE_MANAGER = 'OperationsManager'
const ROLE_PLANNER = 'InventoryPlanner'
const ROLE_PURCHASING = 'PurchasingOfficer'
const ROLE_OPERATOR = 'WarehouseOperator'

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
        // Ignore logout failures and clear local state.
      }
    }

    setSession(null)
  }

  if (isRestoringSession) {
    return <main className="page-shell"><p className="banner">Restoring session...</p></main>
  }

  return (
    <Routes>
      <Route path="/login" element={session ? <Navigate to="/" replace /> : <LoginPage onLoggedIn={setSession} />} />
      <Route path="/" element={session ? <Dashboard session={session} onLogout={handleLogout} onSessionExpired={() => void handleLogout(false)} /> : <Navigate to="/login" replace />} />
      <Route path="*" element={<Navigate to={session ? '/' : '/login'} replace />} />
    </Routes>
  )
}

type LoginPageProps = {
  onLoggedIn: (session: UserSession) => void
}

function LoginPage({ onLoggedIn }: LoginPageProps) {
  const navigate = useNavigate()
  const [form, setForm] = useState<LoginFormState>(defaultLoginForm)
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSubmitting(true)
    setError(null)

    try {
      const session = await fetchJson<UserSession>('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(form),
      })
      onLoggedIn(session)
      navigate('/', { replace: true })
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : 'Login failed.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="page-shell page-shell--auth">
      <section className="auth-card">
        <p className="eyebrow">001 Layered Monolith</p>
        <h1>Identity-driven inventory console</h1>
        <p className="intro">Phase 4 uses seeded ASP.NET Core Identity users and cookie-based authorization. Password for every seeded account is `Password123!`.</p>
        <form className="entity-form auth-form" onSubmit={handleSubmit}>
          <div className="form-grid">
            <label><span>Email</span><input value={form.email} onChange={(event) => setForm((current) => ({ ...current, email: event.target.value }))} /></label>
            <label><span>Password</span><input type="password" value={form.password} onChange={(event) => setForm((current) => ({ ...current, password: event.target.value }))} /></label>
          </div>
          <div className="form-actions"><button className="primary-button" type="submit" disabled={isSubmitting}>{isSubmitting ? 'Signing in...' : 'Sign in'}</button></div>
        </form>
        {error ? <p className="banner banner--error">{error}</p> : null}
        <div className="account-grid">
          {seededUsers.map((user) => (
            <button key={user.email} type="button" className="account-card" onClick={() => setForm({ email: user.email, password: 'Password123!' })}>
              <strong>{user.label}</strong>
              <span>{user.email}</span>
              <small>{user.role}</small>
            </button>
          ))}
        </div>
      </section>
    </main>
  )
}

type DashboardProps = {
  session: UserSession
  onLogout: (callApi?: boolean) => Promise<void>
  onSessionExpired: () => void
}

function Dashboard({ session, onLogout, onSessionExpired }: DashboardProps) {
  const [health, setHealth] = useState<HealthResponse | null>(null)
  const [products, setProducts] = useState<Product[]>([])
  const [warehouses, setWarehouses] = useState<Warehouse[]>([])
  const [inventory, setInventory] = useState<InventorySummaryItem[]>([])
  const [receipts, setReceipts] = useState<InventoryReceipt[]>([])
  const [transfers, setTransfers] = useState<StockTransfer[]>([])
  const [adjustments, setAdjustments] = useState<InventoryAdjustment[]>([])
  const [lowStock, setLowStock] = useState<LowStockItem[]>([])
  const [dashboardError, setDashboardError] = useState<string | null>(null)
  const [catalogError, setCatalogError] = useState<string | null>(null)
  const [workflowError, setWorkflowError] = useState<string | null>(null)
  const [busyAction, setBusyAction] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isRefreshing, setIsRefreshing] = useState(false)
  const [selectedProductId, setSelectedProductId] = useState<string | null>(null)
  const [selectedWarehouseId, setSelectedWarehouseId] = useState<string | null>(null)
  const [productForm, setProductForm] = useState<ProductFormState>(emptyProductForm)
  const [warehouseForm, setWarehouseForm] = useState<WarehouseFormState>(emptyWarehouseForm)
  const [receiptForm, setReceiptForm] = useState<ReceiptFormState>(createEmptyReceiptForm())
  const [transferForm, setTransferForm] = useState<TransferFormState>(createEmptyTransferForm())
  const [adjustmentForm, setAdjustmentForm] = useState<AdjustmentFormState>(createEmptyAdjustmentForm())

  const assignedWarehouseIds = useMemo(() => new Set(session.assignedWarehouseIds), [session.assignedWarehouseIds])
  const selectedProduct = useMemo(() => products.find((item) => item.id === selectedProductId) ?? null, [products, selectedProductId])
  const selectedWarehouse = useMemo(() => warehouses.find((item) => item.id === selectedWarehouseId) ?? null, [warehouses, selectedWarehouseId])
  const activeProducts = useMemo(() => products.filter((item) => item.status === 'Active').length, [products])
  const activeWarehouses = useMemo(() => warehouses.filter((item) => item.status === 'Active').length, [warehouses])
  const pendingAdjustments = useMemo(() => adjustments.filter((item) => item.status === 'PendingApproval').length, [adjustments])
  const openTransfers = useMemo(() => transfers.filter((item) => !['Received', 'Cancelled'].includes(item.status)).length, [transfers])

  const isManager = session.roles.includes(ROLE_MANAGER)
  const isPlanner = session.roles.includes(ROLE_PLANNER)
  const isPurchasing = session.roles.includes(ROLE_PURCHASING)
  const isOperator = session.roles.includes(ROLE_OPERATOR)
  const canManageProducts = isPurchasing || isManager
  const canManageWarehouses = isManager
  const canRecordReceipts = isOperator || isPurchasing || isManager
  const canCreateTransfers = isPlanner || isManager
  const canApproveTransfers = isPlanner || isManager
  const canDispatchTransfers = isOperator || isManager
  const canReceiveTransfers = isOperator || isManager
  const canCancelTransfers = isManager
  const canCreateAdjustments = isOperator || isManager
  const canReviewAdjustments = isManager

  useEffect(() => {
    void loadDashboard()
  }, [])

  useEffect(() => {
    if (selectedProduct) {
      setProductForm({ sku: selectedProduct.sku, name: selectedProduct.name, category: selectedProduct.category, supplierCode: selectedProduct.supplierCode, unitCost: selectedProduct.unitCost.toString() })
    }
  }, [selectedProduct])

  useEffect(() => {
    if (selectedWarehouse) {
      setWarehouseForm({ code: selectedWarehouse.code, name: selectedWarehouse.name, city: selectedWarehouse.city })
    }
  }, [selectedWarehouse])

  function canAccessWarehouse(warehouseId: string) {
    return !isOperator || assignedWarehouseIds.has(warehouseId)
  }

  async function loadDashboard(showRefreshState = false) {
    if (showRefreshState) setIsRefreshing(true)
    else setIsLoading(true)

    setDashboardError(null)

    try {
      const [healthPayload, productsPayload, warehousesPayload, inventoryPayload, receiptsPayload, transfersPayload, adjustmentsPayload, lowStockPayload] = await Promise.all([
        fetchJson<HealthResponse>('/api/health'),
        fetchJson<Product[]>('/api/products'),
        fetchJson<Warehouse[]>('/api/warehouses'),
        fetchJson<InventorySummaryItem[]>('/api/inventory/summary'),
        fetchJson<InventoryReceipt[]>('/api/inventory/receipts'),
        fetchJson<StockTransfer[]>('/api/transfers'),
        fetchJson<InventoryAdjustment[]>('/api/adjustments'),
        fetchJson<LowStockItem[]>('/api/reports/low-stock'),
      ])

      setHealth(healthPayload)
      setProducts(productsPayload)
      setWarehouses(warehousesPayload)
      setInventory(inventoryPayload)
      setReceipts(receiptsPayload)
      setTransfers(transfersPayload)
      setAdjustments(adjustmentsPayload)
      setLowStock(lowStockPayload)
    } catch (error) {
      if (isApiError(error) && error.status === 401) {
        onSessionExpired()
        return
      }
      setDashboardError(error instanceof Error ? error.message : 'Failed to load dashboard data.')
    } finally {
      setIsLoading(false)
      setIsRefreshing(false)
    }
  }

  async function runCatalogAction(action: string, work: () => Promise<void>) {
    setBusyAction(action)
    setCatalogError(null)
    try {
      await work()
      await loadDashboard(true)
    } catch (error) {
      if (isApiError(error) && error.status === 401) {
        onSessionExpired()
        return
      }
      setCatalogError(error instanceof Error ? error.message : 'Catalog request failed.')
    } finally {
      setBusyAction(null)
    }
  }

  async function runWorkflowAction(action: string, work: () => Promise<void>) {
    setBusyAction(action)
    setWorkflowError(null)
    try {
      await work()
      await loadDashboard(true)
    } catch (error) {
      if (isApiError(error) && error.status === 401) {
        onSessionExpired()
        return
      }
      setWorkflowError(error instanceof Error ? error.message : 'Workflow request failed.')
    } finally {
      setBusyAction(null)
    }
  }

  async function handleCreateProduct(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    await runCatalogAction('create-product', async () => {
      await fetchJson('/api/products', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ ...productForm, unitCost: Number(productForm.unitCost) }) })
      setProductForm(emptyProductForm)
    })
  }

  async function handleUpdateProduct(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!selectedProduct) return
    await runCatalogAction(`update-product-${selectedProduct.id}`, async () => {
      await fetchJson(`/api/products/${selectedProduct.id}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ name: productForm.name, category: productForm.category, supplierCode: productForm.supplierCode, unitCost: Number(productForm.unitCost) }) })
    })
  }

  async function handleCreateWarehouse(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    await runCatalogAction('create-warehouse', async () => {
      await fetchJson('/api/warehouses', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(warehouseForm) })
      setWarehouseForm(emptyWarehouseForm)
    })
  }

  async function handleUpdateWarehouse(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!selectedWarehouse) return
    await runCatalogAction(`update-warehouse-${selectedWarehouse.id}`, async () => {
      await fetchJson(`/api/warehouses/${selectedWarehouse.id}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ name: warehouseForm.name, city: warehouseForm.city }) })
    })
  }

  async function handleCreateReceipt(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    await runWorkflowAction('create-receipt', async () => {
      await fetchJson('/api/inventory/receipts', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ warehouseId: receiptForm.warehouseId, productId: receiptForm.productId, quantityReceived: Number(receiptForm.quantityReceived), supplierReference: receiptForm.supplierReference }) })
      setReceiptForm(createEmptyReceiptForm())
    })
  }

  async function handleCreateTransfer(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    await runWorkflowAction('create-transfer', async () => {
      await fetchJson('/api/transfers', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ sourceWarehouseId: transferForm.sourceWarehouseId, destinationWarehouseId: transferForm.destinationWarehouseId, productId: transferForm.productId, quantity: Number(transferForm.quantity), reason: transferForm.reason }) })
      setTransferForm(createEmptyTransferForm())
    })
  }

  async function handleCreateAdjustment(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    await runWorkflowAction('create-adjustment', async () => {
      await fetchJson('/api/adjustments', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ warehouseId: adjustmentForm.warehouseId, productId: adjustmentForm.productId, quantityDelta: Number(adjustmentForm.quantityDelta), reasonCode: adjustmentForm.reasonCode, notes: adjustmentForm.notes }) })
      setAdjustmentForm(createEmptyAdjustmentForm())
    })
  }

  async function archiveProduct(productId: string) {
    await runCatalogAction(`archive-${productId}`, async () => {
      await fetchJson(`/api/products/${productId}/archive`, { method: 'POST' })
      if (selectedProductId === productId) {
        setSelectedProductId(null)
        setProductForm(emptyProductForm)
      }
    })
  }

  async function deactivateWarehouse(warehouseId: string) {
    await runCatalogAction(`deactivate-${warehouseId}`, async () => {
      await fetchJson(`/api/warehouses/${warehouseId}/deactivate`, { method: 'POST' })
      if (selectedWarehouseId === warehouseId) {
        setSelectedWarehouseId(null)
        setWarehouseForm(emptyWarehouseForm)
      }
    })
  }

  async function advanceTransfer(transfer: StockTransfer, action: 'approve' | 'dispatch' | 'receive' | 'cancel') {
    const payload = action === 'cancel' ? { cancellationReason: `Cancelled by ${session.displayName}` } : undefined
    await runWorkflowAction(`${action}-${transfer.id}`, async () => {
      await fetchJson(`/api/transfers/${transfer.id}/${action}`, { method: 'POST', headers: payload ? { 'Content-Type': 'application/json' } : undefined, body: payload ? JSON.stringify(payload) : undefined })
    })
  }

  async function reviewAdjustment(adjustmentId: string, action: 'approve' | 'reject') {
    await runWorkflowAction(`${action}-${adjustmentId}`, async () => {
      await fetchJson(`/api/adjustments/${adjustmentId}/${action}`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ notes: `${action}d by ${session.displayName}` }) })
    })
  }

  return (
    <main className="page-shell">
      <section className="hero-panel">
        <div className="header-copy">
          <p className="eyebrow">001 Layered Monolith</p>
          <h1>Operational inventory workflows</h1>
          <p className="intro">Phase 4 removes client-supplied actors. The signed-in identity now drives all stock-changing actions and warehouse visibility.</p>
          <div className="session-meta">
            <span className="pill pill--active">{session.displayName}</span>
            <span className="pill">{session.email}</span>
            <span className="pill">{session.roles.join(', ')}</span>
          </div>
        </div>
        <div className="hero-actions">
          <button className="secondary-button" onClick={() => void loadDashboard(true)} disabled={isRefreshing}>{isRefreshing ? 'Refreshing...' : 'Refresh data'}</button>
          <button className="ghost-button" onClick={() => void onLogout()}>Sign out</button>
          <div className={`health-chip ${health ? 'health-chip--ok' : 'health-chip--pending'}`}>
            <span>{health?.status ?? 'loading'}</span>
            <small>{health ? new Date(health.timestamp).toLocaleString() : 'Waiting for API'}</small>
          </div>
        </div>
      </section>

      <section className="stats-grid">
        <article className="stat-card"><span className="stat-label">Active products</span><strong>{activeProducts}</strong><small>{products.length} visible products</small></article>
        <article className="stat-card"><span className="stat-label">Active warehouses</span><strong>{activeWarehouses}</strong><small>{warehouses.length} visible warehouses</small></article>
        <article className="stat-card"><span className="stat-label">Open transfers</span><strong>{openTransfers}</strong><small>requested, approved, or dispatched</small></article>
        <article className="stat-card"><span className="stat-label">Pending adjustments</span><strong>{pendingAdjustments}</strong><small>manager queue</small></article>
      </section>

      <section className="info-strip">
        <span className="pill">Cookie auth enabled</span>
        <span className="pill">Operator scope: {isOperator ? `${session.assignedWarehouseIds.length} assigned warehouse(s)` : 'global'}</span>
        <span className="pill">Writes attributed to {session.email}</span>
      </section>

      {dashboardError ? <p className="banner banner--error">{dashboardError}</p> : null}
      {catalogError ? <p className="banner banner--error">{catalogError}</p> : null}
      {workflowError ? <p className="banner banner--error">{workflowError}</p> : null}
      {isLoading ? <p className="banner">Loading dashboard data...</p> : null}

      <section className="workspace-grid workspace-grid--catalog">
        <article className="panel">
          <div className="panel-heading"><div><p className="eyebrow">Catalog</p><h2>Products</h2></div><span className="pill">{products.length} records</span></div>
          {canManageProducts ? (
            <form className="entity-form" onSubmit={selectedProduct ? handleUpdateProduct : handleCreateProduct}>
              <div className="form-grid">
                <label><span>SKU</span><input value={productForm.sku} disabled={Boolean(selectedProduct)} onChange={(event) => setProductForm((current) => ({ ...current, sku: event.target.value }))} /></label>
                <label><span>Name</span><input value={productForm.name} onChange={(event) => setProductForm((current) => ({ ...current, name: event.target.value }))} /></label>
                <label><span>Category</span><input value={productForm.category} onChange={(event) => setProductForm((current) => ({ ...current, category: event.target.value }))} /></label>
                <label><span>Supplier code</span><input value={productForm.supplierCode} onChange={(event) => setProductForm((current) => ({ ...current, supplierCode: event.target.value }))} /></label>
                <label><span>Unit cost</span><input type="number" min="0" step="0.01" value={productForm.unitCost} onChange={(event) => setProductForm((current) => ({ ...current, unitCost: event.target.value }))} /></label>
              </div>
              <div className="form-actions">
                <button className="primary-button" type="submit" disabled={busyAction !== null}>{selectedProduct ? 'Save product' : 'Create product'}</button>
                {selectedProduct ? <button className="secondary-button" type="button" onClick={() => { setSelectedProductId(null); setProductForm(emptyProductForm) }}>Cancel edit</button> : null}
              </div>
            </form>
          ) : <p className="banner">This account can view products but cannot modify the catalog.</p>}
          <div className="entity-list">{products.map((product) => <div key={product.id} className={`entity-card ${selectedProductId === product.id ? 'entity-card--selected' : ''}`}><div><strong>{product.name}</strong><p>{product.sku}</p><small>{product.category} · {product.supplierCode} · ${product.unitCost.toFixed(2)}</small></div><div className="card-actions">{canManageProducts ? <button className="secondary-button" onClick={() => setSelectedProductId(product.id)}>Edit</button> : null}{canManageProducts ? <button className="ghost-button" disabled={product.status !== 'Active' || busyAction !== null} onClick={() => void archiveProduct(product.id)}>Archive</button> : null}</div></div>)}</div>
        </article>

        <article className="panel">
          <div className="panel-heading"><div><p className="eyebrow">Network</p><h2>Warehouses</h2></div><span className="pill">{warehouses.length} sites</span></div>
          {canManageWarehouses ? (
            <form className="entity-form" onSubmit={selectedWarehouse ? handleUpdateWarehouse : handleCreateWarehouse}>
              <div className="form-grid">
                <label><span>Code</span><input value={warehouseForm.code} disabled={Boolean(selectedWarehouse)} onChange={(event) => setWarehouseForm((current) => ({ ...current, code: event.target.value }))} /></label>
                <label><span>Name</span><input value={warehouseForm.name} onChange={(event) => setWarehouseForm((current) => ({ ...current, name: event.target.value }))} /></label>
                <label><span>City</span><input value={warehouseForm.city} onChange={(event) => setWarehouseForm((current) => ({ ...current, city: event.target.value }))} /></label>
              </div>
              <div className="form-actions">
                <button className="primary-button" type="submit" disabled={busyAction !== null}>{selectedWarehouse ? 'Save warehouse' : 'Create warehouse'}</button>
                {selectedWarehouse ? <button className="secondary-button" type="button" onClick={() => { setSelectedWarehouseId(null); setWarehouseForm(emptyWarehouseForm) }}>Cancel edit</button> : null}
              </div>
            </form>
          ) : <p className="banner">This account can view warehouse availability but only managers can edit warehouse records.</p>}
          <div className="entity-list">{warehouses.map((warehouse) => <div key={warehouse.id} className={`entity-card ${selectedWarehouseId === warehouse.id ? 'entity-card--selected' : ''}`}><div><strong>{warehouse.name}</strong><p>{warehouse.code} · {warehouse.city}</p><small>{warehouse.totalSkuCount} SKU positions · {warehouse.lowStockSkuCount} low stock</small></div><div className="card-actions">{canManageWarehouses ? <button className="secondary-button" onClick={() => setSelectedWarehouseId(warehouse.id)}>Edit</button> : null}{canManageWarehouses ? <button className="ghost-button" disabled={warehouse.status !== 'Active' || busyAction !== null} onClick={() => void deactivateWarehouse(warehouse.id)}>Deactivate</button> : null}</div></div>)}</div>
        </article>
      </section>

      <section className="workspace-grid workspace-grid--operations">
        <article className="panel">
          <div className="panel-heading"><div><p className="eyebrow">Receipts</p><h2>Receive stock</h2></div><span className="pill">{receipts.length} receipts</span></div>
          {canRecordReceipts ? <form className="entity-form" onSubmit={handleCreateReceipt}><div className="form-grid"><label><span>Warehouse</span><select value={receiptForm.warehouseId} onChange={(event) => setReceiptForm((current) => ({ ...current, warehouseId: event.target.value }))}><option value="">Select warehouse</option>{warehouses.map((warehouse) => <option key={warehouse.id} value={warehouse.id}>{warehouse.code} · {warehouse.name}</option>)}</select></label><label><span>Product</span><select value={receiptForm.productId} onChange={(event) => setReceiptForm((current) => ({ ...current, productId: event.target.value }))}><option value="">Select product</option>{products.filter((product) => product.status === 'Active').map((product) => <option key={product.id} value={product.id}>{product.sku} · {product.name}</option>)}</select></label><label><span>Quantity</span><input type="number" min="1" value={receiptForm.quantityReceived} onChange={(event) => setReceiptForm((current) => ({ ...current, quantityReceived: event.target.value }))} /></label><label><span>Supplier reference</span><input value={receiptForm.supplierReference} onChange={(event) => setReceiptForm((current) => ({ ...current, supplierReference: event.target.value }))} /></label></div><div className="form-actions"><button className="primary-button" type="submit" disabled={busyAction !== null}>Record receipt</button></div></form> : <p className="banner">Receipt creation is not available for this role.</p>}
          <div className="compact-list">{receipts.slice(0, 4).map((receipt) => <div key={receipt.id} className="compact-item"><strong>{receipt.productSku}</strong><span>{receipt.warehouseCode} · +{receipt.quantityReceived}</span><small>{receipt.receivedBy}</small></div>)}</div>
        </article>

        <article className="panel">
          <div className="panel-heading"><div><p className="eyebrow">Transfers</p><h2>Move stock</h2></div><span className="pill">{openTransfers} open</span></div>
          {canCreateTransfers ? <form className="entity-form" onSubmit={handleCreateTransfer}><div className="form-grid"><label><span>Source</span><select value={transferForm.sourceWarehouseId} onChange={(event) => setTransferForm((current) => ({ ...current, sourceWarehouseId: event.target.value }))}><option value="">Select source</option>{warehouses.filter((warehouse) => warehouse.status === 'Active').map((warehouse) => <option key={warehouse.id} value={warehouse.id}>{warehouse.code} · {warehouse.name}</option>)}</select></label><label><span>Destination</span><select value={transferForm.destinationWarehouseId} onChange={(event) => setTransferForm((current) => ({ ...current, destinationWarehouseId: event.target.value }))}><option value="">Select destination</option>{warehouses.filter((warehouse) => warehouse.status === 'Active').map((warehouse) => <option key={warehouse.id} value={warehouse.id}>{warehouse.code} · {warehouse.name}</option>)}</select></label><label><span>Product</span><select value={transferForm.productId} onChange={(event) => setTransferForm((current) => ({ ...current, productId: event.target.value }))}><option value="">Select product</option>{products.filter((product) => product.status === 'Active').map((product) => <option key={product.id} value={product.id}>{product.sku} · {product.name}</option>)}</select></label><label><span>Quantity</span><input type="number" min="1" value={transferForm.quantity} onChange={(event) => setTransferForm((current) => ({ ...current, quantity: event.target.value }))} /></label><label className="form-grid__full"><span>Reason</span><input value={transferForm.reason} onChange={(event) => setTransferForm((current) => ({ ...current, reason: event.target.value }))} /></label></div><div className="form-actions"><button className="primary-button" type="submit" disabled={busyAction !== null}>Create transfer</button></div></form> : <p className="banner">Transfer creation is restricted to planners and managers.</p>}
          <div className="compact-list">{transfers.slice(0, 6).map((transfer) => <div key={transfer.id} className="compact-item"><strong>{transfer.productSku} · {transfer.quantity}</strong><span>{transfer.sourceWarehouseCode} to {transfer.destinationWarehouseCode} · {transfer.status}</span><small>{transfer.requestedBy}</small><div className="inline-actions">{canApproveTransfers && transfer.status === 'Requested' ? <button className="secondary-button" onClick={() => void advanceTransfer(transfer, 'approve')}>Approve</button> : null}{canDispatchTransfers && transfer.status === 'Approved' && canAccessWarehouse(transfer.sourceWarehouseId) ? <button className="secondary-button" onClick={() => void advanceTransfer(transfer, 'dispatch')}>Dispatch</button> : null}{canReceiveTransfers && transfer.status === 'Dispatched' && canAccessWarehouse(transfer.destinationWarehouseId) ? <button className="secondary-button" onClick={() => void advanceTransfer(transfer, 'receive')}>Receive</button> : null}{canCancelTransfers && ['Requested', 'Approved'].includes(transfer.status) ? <button className="ghost-button" onClick={() => void advanceTransfer(transfer, 'cancel')}>Cancel</button> : null}</div></div>)}</div>
        </article>

        <article className="panel">
          <div className="panel-heading"><div><p className="eyebrow">Adjustments</p><h2>Stock changes</h2></div><span className="pill">{adjustments.length} total</span></div>
          {canCreateAdjustments ? <form className="entity-form" onSubmit={handleCreateAdjustment}><div className="form-grid"><label><span>Warehouse</span><select value={adjustmentForm.warehouseId} onChange={(event) => setAdjustmentForm((current) => ({ ...current, warehouseId: event.target.value }))}><option value="">Select warehouse</option>{warehouses.filter((warehouse) => warehouse.status === 'Active').map((warehouse) => <option key={warehouse.id} value={warehouse.id}>{warehouse.code} · {warehouse.name}</option>)}</select></label><label><span>Product</span><select value={adjustmentForm.productId} onChange={(event) => setAdjustmentForm((current) => ({ ...current, productId: event.target.value }))}><option value="">Select product</option>{products.filter((product) => product.status === 'Active').map((product) => <option key={product.id} value={product.id}>{product.sku} · {product.name}</option>)}</select></label><label><span>Quantity delta</span><input type="number" value={adjustmentForm.quantityDelta} onChange={(event) => setAdjustmentForm((current) => ({ ...current, quantityDelta: event.target.value }))} /></label><label><span>Reason code</span><input value={adjustmentForm.reasonCode} onChange={(event) => setAdjustmentForm((current) => ({ ...current, reasonCode: event.target.value }))} /></label><label className="form-grid__full"><span>Notes</span><input value={adjustmentForm.notes} onChange={(event) => setAdjustmentForm((current) => ({ ...current, notes: event.target.value }))} /></label></div><div className="form-actions"><button className="primary-button" type="submit" disabled={busyAction !== null}>Submit adjustment</button></div></form> : <p className="banner">Adjustment creation is restricted to operators and managers.</p>}
          <div className="compact-list">{adjustments.slice(0, 6).map((adjustment) => <div key={adjustment.id} className="compact-item"><strong>{adjustment.productSku} · {adjustment.quantityDelta > 0 ? '+' : ''}{adjustment.quantityDelta}</strong><span>{adjustment.warehouseCode} · {adjustment.reasonCode} · {adjustment.status}</span><small>{adjustment.submittedBy}</small><div className="inline-actions">{canReviewAdjustments && adjustment.status === 'PendingApproval' ? <button className="secondary-button" onClick={() => void reviewAdjustment(adjustment.id, 'approve')}>Approve</button> : null}{canReviewAdjustments && adjustment.status === 'PendingApproval' ? <button className="ghost-button" onClick={() => void reviewAdjustment(adjustment.id, 'reject')}>Reject</button> : null}</div></div>)}</div>
        </article>
      </section>

      <section className="workspace-grid workspace-grid--reports">
        <article className="panel inventory-panel"><div className="panel-heading"><div><p className="eyebrow">Low stock</p><h2>Reorder watchlist</h2></div><span className="pill">{lowStock.length} items</span></div><div className="inventory-table-wrapper"><table className="inventory-table"><thead><tr><th>Warehouse</th><th>Product</th><th>Available</th><th>Threshold</th><th>Shortfall</th></tr></thead><tbody>{lowStock.map((item) => <tr key={item.inventoryItemId}><td><strong>{item.warehouseCode}</strong><span>{item.warehouseName}</span></td><td><strong>{item.productSku}</strong><span>{item.productName}</span></td><td>{item.availableQuantity}</td><td>{item.reorderThreshold}</td><td><span className="pill pill--warning">{item.shortfallQuantity}</span></td></tr>)}</tbody></table></div></article>
        <article className="panel inventory-panel"><div className="panel-heading"><div><p className="eyebrow">Inventory</p><h2>Current stock summary</h2></div><span className="pill">{inventory.length} rows</span></div><div className="inventory-table-wrapper"><table className="inventory-table"><thead><tr><th>Warehouse</th><th>Product</th><th>On hand</th><th>Reserved</th><th>Available</th><th>Status</th></tr></thead><tbody>{inventory.map((item) => <tr key={item.id}><td><strong>{item.warehouseCode}</strong><span>{item.warehouseName}</span></td><td><strong>{item.productSku}</strong><span>{item.productName}</span></td><td>{item.quantityOnHand}</td><td>{item.quantityReserved}</td><td>{item.availableQuantity}</td><td><span className={`pill ${item.isLowStock ? 'pill--warning' : 'pill--active'}`}>{item.isLowStock ? 'Low stock' : 'Healthy'}</span></td></tr>)}</tbody></table></div></article>
      </section>
    </main>
  )
}

export default App

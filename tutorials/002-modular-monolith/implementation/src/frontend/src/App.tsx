import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { Navigate, Route, Routes, useNavigate } from 'react-router-dom'
import './App.css'
import { fetchJson, isApiError } from './api'
import {
  defaultLoginForm,
  emptyCustomerForm,
  emptyOrderForm,
  emptyOrderLineForm,
  emptyProductForm,
  seededUsers,
  type Customer,
  type CustomerFormState,
  type HealthResponse,
  type Invoice,
  type LoginFormState,
  type OrderFormState,
  type OrderSummary,
  type Product,
  type ProductFormState,
  type ReportSummary,
  type StockItem,
  type UserSession,
  type Warehouse,
} from './models'
import { getOrderActions } from './workflows'

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
        // Ignore logout failures.
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

export function LoginPage({ onLoggedIn }: LoginPageProps) {
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
        <p className="eyebrow">002 Modular Monolith</p>
        <h1>B2B wholesale operations console</h1>
        <p className="intro">Seeded ASP.NET Core Identity accounts use cookie auth. Password for every seeded account is <code>Password123!</code>.</p>
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
  const [customers, setCustomers] = useState<Customer[]>([])
  const [products, setProducts] = useState<Product[]>([])
  const [warehouses, setWarehouses] = useState<Warehouse[]>([])
  const [stock, setStock] = useState<StockItem[]>([])
  const [orders, setOrders] = useState<OrderSummary[]>([])
  const [invoices, setInvoices] = useState<Invoice[]>([])
  const [report, setReport] = useState<ReportSummary | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isRefreshing, setIsRefreshing] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [busyAction, setBusyAction] = useState<string | null>(null)
  const [selectedCustomerId, setSelectedCustomerId] = useState<string | null>(null)
  const [selectedProductId, setSelectedProductId] = useState<string | null>(null)
  const [customerForm, setCustomerForm] = useState<CustomerFormState>(emptyCustomerForm())
  const [productForm, setProductForm] = useState<ProductFormState>(emptyProductForm())
  const [orderForm, setOrderForm] = useState<OrderFormState>(emptyOrderForm())

  const isSales = session.roles.includes('SalesCoordinator')
  const isFinance = session.roles.includes('FinanceOfficer')
  const isManager = session.roles.includes('OperationsManager')
  const canManageCustomers = isSales || isManager
  const canManageCatalog = isManager
  const canManageOrders = isSales || isManager
  const canManageBilling = isFinance || isManager
  const canViewReports = isManager

  useEffect(() => {
    void loadDashboard()
  }, [])

  useEffect(() => {
    const selected = customers.find((item) => item.id === selectedCustomerId)
    if (selected) {
      setCustomerForm({
        accountCode: selected.accountCode,
        name: selected.name,
        billingContactName: selected.billingContactName,
        billingContactEmail: selected.billingContactEmail,
        shippingContactName: selected.shippingContactName,
        shippingContactEmail: selected.shippingContactEmail,
        isActive: selected.status === 'Active',
      })
    }
  }, [selectedCustomerId, customers])

  useEffect(() => {
    const selected = products.find((item) => item.id === selectedProductId)
    if (selected) {
      setProductForm({
        sku: selected.sku,
        name: selected.name,
        category: selected.category,
        unitPrice: selected.unitPrice.toString(),
        isActive: selected.status === 'Active',
      })
    }
  }, [selectedProductId, products])

  async function loadDashboard(refresh = false) {
    if (refresh) setIsRefreshing(true)
    else setIsLoading(true)

    setError(null)

    try {
      const [healthPayload, customersPayload, productsPayload, warehousesPayload, stockPayload, ordersPayload, invoicesPayload, reportPayload] = await Promise.all([
        fetchJson<HealthResponse>('/api/health'),
        fetchJson<Customer[]>('/api/customers'),
        fetchJson<Product[]>('/api/catalog/products'),
        fetchJson<Warehouse[]>('/api/inventory/warehouses'),
        fetchJson<StockItem[]>('/api/inventory/stock'),
        fetchJson<OrderSummary[]>('/api/orders'),
        fetchJson<Invoice[]>('/api/billing/invoices'),
        fetchOptional<ReportSummary>('/api/reports/summary'),
      ])

      setHealth(healthPayload)
      setCustomers(customersPayload)
      setProducts(productsPayload)
      setWarehouses(warehousesPayload)
      setStock(stockPayload)
      setOrders(ordersPayload)
      setInvoices(invoicesPayload)
      setReport(reportPayload)
    } catch (loadError) {
      if (isApiError(loadError) && loadError.status === 401) {
        onSessionExpired()
        return
      }

      setError(loadError instanceof Error ? loadError.message : 'Failed to load dashboard data.')
    } finally {
      setIsLoading(false)
      setIsRefreshing(false)
    }
  }

  async function fetchOptional<T>(path: string): Promise<T | null> {
    try {
      return await fetchJson<T>(path)
    } catch (loadError) {
      if (isApiError(loadError) && loadError.status === 403) {
        return null
      }
      throw loadError
    }
  }

  async function runAction(key: string, action: () => Promise<void>) {
    setBusyAction(key)
    setError(null)
    try {
      await action()
      await loadDashboard(true)
    } catch (actionError) {
      if (isApiError(actionError) && actionError.status === 401) {
        onSessionExpired()
        return
      }

      setError(actionError instanceof Error ? actionError.message : 'Request failed.')
    } finally {
      setBusyAction(null)
    }
  }

  async function handleCustomerSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    await runAction(selectedCustomerId ?? 'create-customer', async () => {
      if (selectedCustomerId) {
        await fetchJson(`/api/customers/${selectedCustomerId}`, {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(customerForm),
        })
      } else {
        await fetchJson('/api/customers', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(customerForm),
        })
      }
      setSelectedCustomerId(null)
      setCustomerForm(emptyCustomerForm())
    })
  }

  async function handleProductSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    await runAction(selectedProductId ?? 'create-product', async () => {
      const payload = {
        ...productForm,
        unitPrice: Number(productForm.unitPrice),
      }
      if (selectedProductId) {
        await fetchJson(`/api/catalog/products/${selectedProductId}`, {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(payload),
        })
      } else {
        await fetchJson('/api/catalog/products', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(payload),
        })
      }
      setSelectedProductId(null)
      setProductForm(emptyProductForm())
    })
  }

  async function handleOrderSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    await runAction('create-order', async () => {
      await fetchJson('/api/orders', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          customerId: orderForm.customerId,
          lines: orderForm.lines.map((line) => ({ productId: line.productId, quantity: Number(line.quantity) })),
        }),
      })
      setOrderForm(emptyOrderForm())
    })
  }

  async function performOrderAction(orderId: string, action: 'submit' | 'reserve' | 'ready-for-invoicing' | 'complete' | 'cancel') {
    await runAction(`${action}-${orderId}`, async () => {
      await fetchJson(`/api/orders/${orderId}/${action}`, { method: 'POST' })
    })
  }

  async function createInvoiceDraft(orderId: string) {
    await runAction(`draft-${orderId}`, async () => {
      await fetchJson('/api/billing/invoices', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ orderId }),
      })
    })
  }

  async function issueInvoice(invoiceId: string) {
    await runAction(`issue-${invoiceId}`, async () => {
      await fetchJson(`/api/billing/invoices/${invoiceId}/issue`, { method: 'POST' })
    })
  }

  async function markInvoicePaid(invoiceId: string) {
    await runAction(`paid-${invoiceId}`, async () => {
      await fetchJson(`/api/billing/invoices/${invoiceId}/mark-paid`, { method: 'POST' })
    })
  }

  function updateOrderLine(index: number, field: 'productId' | 'quantity', value: string) {
    setOrderForm((current) => ({
      ...current,
      lines: current.lines.map((line, lineIndex) => lineIndex === index ? { ...line, [field]: value } : line),
    }))
  }

  function addOrderLine() {
    setOrderForm((current) => ({ ...current, lines: [...current.lines, emptyOrderLineForm()] }))
  }

  function removeOrderLine(index: number) {
    setOrderForm((current) => ({ ...current, lines: current.lines.filter((_, lineIndex) => lineIndex !== index) }))
  }

  const invoiceByOrderId = Object.fromEntries(invoices.map((invoice) => [invoice.orderId, invoice])) as Record<string, Invoice | undefined>

  return (
    <main className="page-shell">
      <section className="hero-panel">
        <div className="header-copy">
          <p className="eyebrow">002 Modular Monolith</p>
          <h1>Wholesale operations dashboard</h1>
          <p className="intro">One deployable application, explicit module boundaries, and end-to-end business workflows across customers, catalog, orders, inventory, billing, and reporting.</p>
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
        <article className="stat-card"><span className="stat-label">Customers</span><strong>{customers.length}</strong><small>Managed in Customers module</small></article>
        <article className="stat-card"><span className="stat-label">Products</span><strong>{products.filter((item) => item.status === 'Active').length}</strong><small>Active catalog records</small></article>
        <article className="stat-card"><span className="stat-label">Orders</span><strong>{orders.length}</strong><small>All workflow states</small></article>
        <article className="stat-card"><span className="stat-label">Invoices</span><strong>{invoices.length}</strong><small>Billing-owned records</small></article>
      </section>

      {error ? <p className="banner banner--error">{error}</p> : null}
      {isLoading ? <p className="banner">Loading dashboard data...</p> : null}

      <section className="workspace-grid">
        <article className="panel">
          <div className="panel-heading"><div><p className="eyebrow">Customers</p><h2>Account management</h2></div><span className="pill">{customers.length} records</span></div>
          {canManageCustomers ? (
            <form className="entity-form" onSubmit={handleCustomerSubmit}>
              <div className="form-grid">
                <label><span>Account code</span><input value={customerForm.accountCode} disabled={Boolean(selectedCustomerId)} onChange={(event) => setCustomerForm((current) => ({ ...current, accountCode: event.target.value }))} /></label>
                <label><span>Name</span><input value={customerForm.name} onChange={(event) => setCustomerForm((current) => ({ ...current, name: event.target.value }))} /></label>
                <label><span>Billing contact</span><input value={customerForm.billingContactName} onChange={(event) => setCustomerForm((current) => ({ ...current, billingContactName: event.target.value }))} /></label>
                <label><span>Billing email</span><input value={customerForm.billingContactEmail} onChange={(event) => setCustomerForm((current) => ({ ...current, billingContactEmail: event.target.value }))} /></label>
                <label><span>Shipping contact</span><input value={customerForm.shippingContactName} onChange={(event) => setCustomerForm((current) => ({ ...current, shippingContactName: event.target.value }))} /></label>
                <label><span>Shipping email</span><input value={customerForm.shippingContactEmail} onChange={(event) => setCustomerForm((current) => ({ ...current, shippingContactEmail: event.target.value }))} /></label>
                <label className="checkbox-row"><input type="checkbox" checked={customerForm.isActive} onChange={(event) => setCustomerForm((current) => ({ ...current, isActive: event.target.checked }))} /><span>Active customer</span></label>
              </div>
              <div className="form-actions">
                <button className="primary-button" type="submit" disabled={busyAction !== null}>{selectedCustomerId ? 'Save customer' : 'Create customer'}</button>
                {selectedCustomerId ? <button className="secondary-button" type="button" onClick={() => { setSelectedCustomerId(null); setCustomerForm(emptyCustomerForm()) }}>Cancel edit</button> : null}
              </div>
            </form>
          ) : <p className="banner">This role can view workflow results but cannot manage customer records.</p>}
          <div className="entity-list">
            {customers.map((customer) => (
              <div key={customer.id} className={`entity-card ${selectedCustomerId === customer.id ? 'entity-card--selected' : ''}`}>
                <div>
                  <strong>{customer.name}</strong>
                  <p>{customer.accountCode} · {customer.status}</p>
                  <small>{customer.billingContactName} · {customer.billingContactEmail}</small>
                </div>
                {canManageCustomers ? <button className="secondary-button" onClick={() => setSelectedCustomerId(customer.id)}>Edit</button> : null}
              </div>
            ))}
          </div>
        </article>

        <article className="panel">
          <div className="panel-heading"><div><p className="eyebrow">Catalog</p><h2>Product ownership</h2></div><span className="pill">{products.length} records</span></div>
          {canManageCatalog ? (
            <form className="entity-form" onSubmit={handleProductSubmit}>
              <div className="form-grid">
                <label><span>SKU</span><input value={productForm.sku} disabled={Boolean(selectedProductId)} onChange={(event) => setProductForm((current) => ({ ...current, sku: event.target.value }))} /></label>
                <label><span>Name</span><input value={productForm.name} onChange={(event) => setProductForm((current) => ({ ...current, name: event.target.value }))} /></label>
                <label><span>Category</span><input value={productForm.category} onChange={(event) => setProductForm((current) => ({ ...current, category: event.target.value }))} /></label>
                <label><span>Unit price</span><input type="number" min="0" step="0.01" value={productForm.unitPrice} onChange={(event) => setProductForm((current) => ({ ...current, unitPrice: event.target.value }))} /></label>
                <label className="checkbox-row"><input type="checkbox" checked={productForm.isActive} onChange={(event) => setProductForm((current) => ({ ...current, isActive: event.target.checked }))} /><span>Active product</span></label>
              </div>
              <div className="form-actions">
                <button className="primary-button" type="submit" disabled={busyAction !== null}>{selectedProductId ? 'Save product' : 'Create product'}</button>
                {selectedProductId ? <button className="secondary-button" type="button" onClick={() => { setSelectedProductId(null); setProductForm(emptyProductForm()) }}>Cancel edit</button> : null}
              </div>
            </form>
          ) : <p className="banner">Catalog writes are restricted to managers in this tutorial.</p>}
          <div className="entity-list">
            {products.map((product) => (
              <div key={product.id} className={`entity-card ${selectedProductId === product.id ? 'entity-card--selected' : ''}`}>
                <div>
                  <strong>{product.name}</strong>
                  <p>{product.sku} · {product.category} · {product.status}</p>
                  <small>${product.unitPrice.toFixed(2)}</small>
                </div>
                {canManageCatalog ? <button className="secondary-button" onClick={() => setSelectedProductId(product.id)}>Edit</button> : null}
              </div>
            ))}
          </div>
        </article>
      </section>

      <section className="workspace-grid">
        <article className="panel">
          <div className="panel-heading"><div><p className="eyebrow">Orders</p><h2>Create draft order</h2></div><span className="pill">{orders.length} total</span></div>
          {canManageOrders ? (
            <form className="entity-form" onSubmit={handleOrderSubmit}>
              <div className="form-grid">
                <label className="form-grid__full"><span>Customer</span><select value={orderForm.customerId} onChange={(event) => setOrderForm((current) => ({ ...current, customerId: event.target.value }))}><option value="">Select customer</option>{customers.filter((customer) => customer.status === 'Active').map((customer) => <option key={customer.id} value={customer.id}>{customer.accountCode} · {customer.name}</option>)}</select></label>
              </div>
              <div className="line-items">
                {orderForm.lines.map((line, index) => (
                  <div key={`${index}-${line.productId}`} className="line-item-row">
                    <select value={line.productId} onChange={(event) => updateOrderLine(index, 'productId', event.target.value)}>
                      <option value="">Select product</option>
                      {products.filter((product) => product.status === 'Active').map((product) => <option key={product.id} value={product.id}>{product.sku} · {product.name}</option>)}
                    </select>
                    <input type="number" min="1" value={line.quantity} onChange={(event) => updateOrderLine(index, 'quantity', event.target.value)} />
                    {orderForm.lines.length > 1 ? <button className="ghost-button" type="button" onClick={() => removeOrderLine(index)}>Remove</button> : null}
                  </div>
                ))}
              </div>
              <div className="form-actions">
                <button className="secondary-button" type="button" onClick={addOrderLine}>Add line</button>
                <button className="primary-button" type="submit" disabled={busyAction !== null}>Create order</button>
              </div>
            </form>
          ) : <p className="banner">This role cannot create draft orders.</p>}
        </article>

        <article className="panel panel--wide">
          <div className="panel-heading"><div><p className="eyebrow">Workflow</p><h2>Order lifecycle</h2></div><span className="pill">{orders.length} orders</span></div>
          <div className="entity-list">
            {orders.map((order) => {
              const linkedInvoice = invoiceByOrderId[order.id]
              const actions = getOrderActions(session, order, linkedInvoice)
              return (
                <div key={order.id} className="entity-card entity-card--stacked">
                  <div>
                    <strong>{order.customerName}</strong>
                    <p>{order.customerAccountCode} · {order.status}</p>
                    <small>{order.lines.map((line) => `${line.productSku} x${line.quantity}`).join(' | ')}</small>
                  </div>
                  <div className="card-summary">
                    <span>Total: ${order.totalAmount.toFixed(2)}</span>
                    <span>Reservation: {order.reservationId ? 'linked' : 'pending'}</span>
                    <span>Invoice: {linkedInvoice ? `${linkedInvoice.invoiceNumber} · ${linkedInvoice.status}` : 'none'}</span>
                  </div>
                  <div className="inline-actions">
                    {actions.includes('submit') ? <button className="secondary-button" onClick={() => void performOrderAction(order.id, 'submit')}>Submit</button> : null}
                    {actions.includes('reserve') ? <button className="secondary-button" onClick={() => void performOrderAction(order.id, 'reserve')}>Reserve</button> : null}
                    {actions.includes('ready') ? <button className="secondary-button" onClick={() => void performOrderAction(order.id, 'ready-for-invoicing')}>Ready for invoicing</button> : null}
                    {actions.includes('create-invoice') ? <button className="secondary-button" onClick={() => void createInvoiceDraft(order.id)}>Create draft invoice</button> : null}
                    {actions.includes('complete') ? <button className="primary-button" onClick={() => void performOrderAction(order.id, 'complete')}>Complete</button> : null}
                    {actions.includes('cancel') ? <button className="ghost-button" onClick={() => void performOrderAction(order.id, 'cancel')}>Cancel</button> : null}
                  </div>
                </div>
              )
            })}
          </div>
        </article>
      </section>

      <section className="workspace-grid">
        <article className="panel">
          <div className="panel-heading"><div><p className="eyebrow">Inventory</p><h2>Stock by warehouse</h2></div><span className="pill">{warehouses.length} warehouse(s)</span></div>
          <div className="inventory-table-wrapper">
            <table className="inventory-table">
              <thead><tr><th>Warehouse</th><th>Product</th><th>On hand</th><th>Reserved</th><th>Available</th></tr></thead>
              <tbody>
                {stock.map((item) => (
                  <tr key={item.id}>
                    <td><strong>{item.warehouseCode}</strong><span>{item.warehouseName}</span></td>
                    <td><strong>{item.productSku}</strong><span>{item.productName}</span></td>
                    <td>{item.quantityOnHand}</td>
                    <td>{item.quantityReserved}</td>
                    <td>{item.availableQuantity}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </article>

        <article className="panel">
          <div className="panel-heading"><div><p className="eyebrow">Billing</p><h2>Invoices</h2></div><span className="pill">{invoices.length} records</span></div>
          <div className="entity-list">
            {invoices.map((invoice) => (
              <div key={invoice.id} className="entity-card entity-card--stacked">
                <div>
                  <strong>{invoice.invoiceNumber}</strong>
                  <p>{invoice.customerName} · {invoice.status}</p>
                  <small>Order {invoice.orderId.slice(0, 8)} · ${invoice.totalAmount.toFixed(2)}</small>
                </div>
                <div className="inline-actions">
                  {canManageBilling && invoice.status === 'Draft' ? <button className="secondary-button" onClick={() => void issueInvoice(invoice.id)}>Issue</button> : null}
                  {canManageBilling && invoice.status === 'Issued' ? <button className="primary-button" onClick={() => void markInvoicePaid(invoice.id)}>Mark paid</button> : null}
                </div>
              </div>
            ))}
          </div>
        </article>
      </section>

      <section className="workspace-grid">
        <article className="panel panel--wide">
          <div className="panel-heading"><div><p className="eyebrow">Reporting</p><h2>Module health</h2></div><span className="pill">{canViewReports ? 'Manager view' : 'Restricted'}</span></div>
          {report ? (
            <>
              <div className="stats-grid stats-grid--compact">
                <article className="stat-card"><span className="stat-label">Reserved value</span><strong>${report.totalReservedValue.toFixed(2)}</strong></article>
                <article className="stat-card"><span className="stat-label">Paid value</span><strong>${report.totalPaidValue.toFixed(2)}</strong></article>
                <article className="stat-card"><span className="stat-label">Ready orders</span><strong>{report.readyForInvoicingOrders}</strong></article>
                <article className="stat-card"><span className="stat-label">Paid invoices</span><strong>{report.paidInvoices}</strong></article>
              </div>
              <div className="entity-list">
                {report.moduleHealth.map((module) => (
                  <div key={module.moduleName} className="entity-card">
                    <div>
                      <strong>{module.moduleName}</strong>
                      <p>{module.status}</p>
                      <small>{module.summary}</small>
                    </div>
                  </div>
                ))}
              </div>
            </>
          ) : <p className="banner">Reporting is available only for the manager role.</p>}
        </article>
      </section>
    </main>
  )
}

export default App




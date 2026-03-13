import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import './App.css'
import { api } from './api'
import type {
  Availability,
  CurrentUser,
  DashboardSummary,
  OrderDetail,
  OrderSummary,
  Product,
  SeedUser,
  Shipment,
} from './models'

type DraftLine = {
  sku: string
  quantity: number
}

const emptyDashboard: DashboardSummary = {
  totalOrders: 0,
  awaitingDependencies: 0,
  readyForFulfillment: 0,
  fulfillmentInProgress: 0,
  failed: 0,
  completed: 0,
  recentOrders: [],
}

function App() {
  const [seedUsers, setSeedUsers] = useState<SeedUser[]>([])
  const [currentUser, setCurrentUser] = useState<CurrentUser | null>(null)
  const [products, setProducts] = useState<Product[]>([])
  const [availability, setAvailability] = useState<Availability | null>(null)
  const [dashboard, setDashboard] = useState<DashboardSummary>(emptyDashboard)
  const [orders, setOrders] = useState<OrderSummary[]>([])
  const [orderDetail, setOrderDetail] = useState<OrderDetail | null>(null)
  const [shipments, setShipments] = useState<Shipment[]>([])
  const [selectedSku, setSelectedSku] = useState('')
  const [selectedOrderId, setSelectedOrderId] = useState<string | null>(null)
  const [selectedShipmentId, setSelectedShipmentId] = useState<string | null>(null)
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('Password123!')
  const [customerReference, setCustomerReference] = useState('CSR-ORDER-10042')
  const [draftLines, setDraftLines] = useState<DraftLine[]>([{ sku: '', quantity: 1 }])
  const [isBusy, setIsBusy] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    void bootstrap()
  }, [])

  useEffect(() => {
    if (selectedSku && currentUser) {
      void loadAvailability(selectedSku)
    }
  }, [selectedSku, currentUser])

  useEffect(() => {
    if (selectedOrderId && currentUser) {
      void loadOrderDetail(selectedOrderId)
    }
  }, [selectedOrderId, currentUser])

  async function bootstrap() {
    setError('')
    try {
      const seededUsers = await api.getSeedUsers()
      setSeedUsers(seededUsers)
      if (seededUsers.length > 0) {
        setEmail((currentEmail) => currentEmail || seededUsers[0].email)
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
    const [productList, dashboardSummary, orderList, shipmentList] = await Promise.all([
      withRetry(() => api.getProducts()),
      withRetry(() => api.getDashboard()),
      withRetry(() => api.getOrders()),
      withRetry(() => api.getShipments()),
    ])

    setProducts(productList)
    setDashboard(dashboardSummary)
    setOrders(orderList)
    setShipments(shipmentList)

    if (!selectedSku && productList.length > 0) {
      setSelectedSku(productList.find((product) => product.isSellable)?.sku ?? productList[0].sku)
    }

    if (!selectedOrderId && orderList.length > 0) {
      setSelectedOrderId(orderList[0].orderId)
    }

    if (!selectedShipmentId && shipmentList.length > 0) {
      setSelectedShipmentId(shipmentList[0].shipmentId)
    }
  }

  async function loadAvailability(sku: string) {
    try {
      setAvailability(await withRetry(() => api.getAvailability(sku)))
    } catch {
      setAvailability(null)
    }
  }

  async function loadOrderDetail(orderId: string) {
    try {
      setOrderDetail(await withRetry(() => api.getOrder(orderId)))
    } catch {
      setOrderDetail(null)
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

  async function handleLogout() {
    await api.logout()
    setCurrentUser(null)
    setDashboard(emptyDashboard)
    setOrders([])
    setOrderDetail(null)
    setShipments([])
  }

  async function handleCreateOrder(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError('')
    setIsBusy(true)

    try {
      const payload = {
        customerReference,
        currency: 'AUD',
        lines: draftLines.filter((line) => line.sku.trim() !== ''),
      }
      const createdOrder = await api.createOrder(payload)
      await loadWorkspace()
      setSelectedOrderId(createdOrder.orderId)
      setOrderDetail(createdOrder)
      setDraftLines([{ sku: '', quantity: 1 }])
      setCustomerReference(`CSR-ORDER-${Math.floor(Math.random() * 90000) + 10000}`)
    } catch (requestError) {
      setError((requestError as Error).message)
    } finally {
      setIsBusy(false)
    }
  }

  async function handleSubmitOrder(orderId: string) {
    setError('')
    setIsBusy(true)
    try {
      const submitted = await api.submitOrder(orderId)
      setOrderDetail(submitted)
      await refreshLiveData(orderId)
    } catch (requestError) {
      setError((requestError as Error).message)
    } finally {
      setIsBusy(false)
    }
  }

  async function handleProgressShipment(command: string) {
    if (!selectedShipmentId) {
      return
    }

    setError('')
    setIsBusy(true)
    try {
      const shipment = await api.progressShipment(selectedShipmentId, command)
      setSelectedShipmentId(shipment.shipmentId)
      await refreshLiveData(shipment.orderId)
    } catch (requestError) {
      setError((requestError as Error).message)
    } finally {
      setIsBusy(false)
    }
  }

  async function refreshLiveData(orderId?: string) {
    await loadWorkspace()
    if (orderId) {
      setSelectedOrderId(orderId)
      await loadOrderDetail(orderId)
    }
  }

  function updateDraftLine(index: number, updates: Partial<DraftLine>) {
    setDraftLines((current) =>
      current.map((line, lineIndex) => (lineIndex === index ? { ...line, ...updates } : line)),
    )
  }

  function addDraftLine() {
    setDraftLines((current) => [...current, { sku: '', quantity: 1 }])
  }

  function removeDraftLine(index: number) {
    setDraftLines((current) => (current.length === 1 ? current : current.filter((_, lineIndex) => lineIndex !== index)))
  }

  const selectedShipment = shipments.find((shipment) => shipment.shipmentId === selectedShipmentId) ?? shipments[0] ?? null
  const canCreateOrders = currentUser?.role === 'OrderOpsAgent' || currentUser?.role === 'OperationsManager'
  const canProgressFulfillment = currentUser?.role === 'FulfillmentOperator' || currentUser?.role === 'OperationsManager'

  if (!currentUser) {
    return (
      <main className="login-shell">
        <section className="login-panel">
          <p className="eyebrow">004 Microservices</p>
          <h1>Omnichannel commerce operations network</h1>
          <p className="lede">
            Gateway-backed SPA, service-owned data, and RabbitMQ-driven workflow orchestration. Seeded users all use
            <code>Password123!</code>.
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
              <button
                type="button"
                key={user.email}
                className="seed-user-card"
                onClick={() => setEmail(user.email)}
              >
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

  return (
    <main className="app-shell">
      <header className="app-header">
        <div>
          <p className="eyebrow">004 Microservices</p>
          <h1>Operations control plane</h1>
        </div>
        <div className="header-actions">
          <span className="user-pill">
            {currentUser.displayName}
            <small>{currentUser.role}</small>
          </span>
          <button type="button" className="ghost-button" onClick={() => void refreshLiveData(selectedOrderId ?? undefined)}>
            Refresh
          </button>
          <button type="button" className="ghost-button" onClick={handleLogout}>
            Sign out
          </button>
        </div>
      </header>

      {error ? <p className="error-banner">{error}</p> : null}

      <section className="summary-grid">
        <SummaryCard label="Total orders" value={dashboard.totalOrders} />
        <SummaryCard label="Awaiting dependencies" value={dashboard.awaitingDependencies} />
        <SummaryCard label="Ready for fulfillment" value={dashboard.readyForFulfillment} />
        <SummaryCard label="In fulfillment" value={dashboard.fulfillmentInProgress} />
        <SummaryCard label="Failed" value={dashboard.failed} />
        <SummaryCard label="Completed" value={dashboard.completed} />
      </section>

      <section className="workspace-grid">
        <section className="panel">
          <div className="panel-heading">
            <div>
              <p className="eyebrow">Orders</p>
              <h2>Draft and submission flow</h2>
            </div>
            <span className="status-pill neutral">{orders.length} tracked</span>
          </div>

          <form className="order-form" onSubmit={handleCreateOrder}>
            <label>
              Customer reference
              <input value={customerReference} onChange={(event) => setCustomerReference(event.target.value)} />
            </label>

            <div className="line-items">
              {draftLines.map((line, index) => (
                <div className="line-item" key={`${index}-${line.sku}`}>
                  <select value={line.sku} onChange={(event) => updateDraftLine(index, { sku: event.target.value })}>
                    <option value="">Select product</option>
                    {products.filter((product) => product.isSellable).map((product) => (
                      <option key={product.sku} value={product.sku}>
                        {product.name}
                      </option>
                    ))}
                  </select>
                  <input
                    type="number"
                    min={1}
                    value={line.quantity}
                    onChange={(event) => updateDraftLine(index, { quantity: Number(event.target.value) || 1 })}
                  />
                  <button type="button" className="ghost-button compact-button" onClick={() => removeDraftLine(index)}>
                    Remove
                  </button>
                </div>
              ))}
            </div>

            <div className="form-actions">
              <button type="button" className="secondary-button" onClick={addDraftLine}>
                Add line
              </button>
              <button type="submit" className="primary-button" disabled={!canCreateOrders || isBusy}>
                Create draft order
              </button>
            </div>
          </form>

          <div className="table-panel">
            <table>
              <thead>
                <tr>
                  <th>Order</th>
                  <th>Status</th>
                  <th>Total</th>
                </tr>
              </thead>
              <tbody>
                {orders.map((order) => (
                  <tr
                    key={order.orderId}
                    className={order.orderId === selectedOrderId ? 'selected-row' : ''}
                    onClick={() => setSelectedOrderId(order.orderId)}
                  >
                    <td>
                      <strong>{order.orderNumber}</strong>
                      <span>{order.customerReference}</span>
                    </td>
                    <td>
                      <span className={`status-pill ${statusTone(order.status)}`}>{formatStatus(order.status)}</span>
                    </td>
                    <td>{formatCurrency(order.totalAmount)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>

        <section className="panel inspector-panel">
          <div className="panel-heading">
            <div>
              <p className="eyebrow">Order detail</p>
              <h2>{orderDetail?.orderNumber ?? 'Select an order'}</h2>
            </div>
            {orderDetail ? (
              <span className={`status-pill ${statusTone(orderDetail.status)}`}>{formatStatus(orderDetail.status)}</span>
            ) : null}
          </div>

          {orderDetail ? (
            <div className="detail-stack">
              <section className="detail-card">
                <dl>
                  <div>
                    <dt>Customer reference</dt>
                    <dd>{orderDetail.customerReference}</dd>
                  </div>
                  <div>
                    <dt>Created by</dt>
                    <dd>{orderDetail.createdByEmail}</dd>
                  </div>
                  <div>
                    <dt>Reservation</dt>
                    <dd>{formatStatus(orderDetail.reservationStatus)}</dd>
                  </div>
                  <div>
                    <dt>Payment</dt>
                    <dd>{formatStatus(orderDetail.paymentStatus)}</dd>
                  </div>
                  <div>
                    <dt>Shipment</dt>
                    <dd>{orderDetail.shipmentStatus ? formatStatus(orderDetail.shipmentStatus) : 'Not created'}</dd>
                  </div>
                  <div>
                    <dt>Total</dt>
                    <dd>{formatCurrency(orderDetail.totalAmount)}</dd>
                  </div>
                </dl>
                {orderDetail.failureReason ? <p className="failure-copy">{orderDetail.failureReason}</p> : null}
                {orderDetail.status === 'Draft' ? (
                  <button
                    type="button"
                    className="primary-button"
                    disabled={!canCreateOrders || isBusy}
                    onClick={() => void handleSubmitOrder(orderDetail.orderId)}
                  >
                    Submit order
                  </button>
                ) : null}
              </section>

              <section className="detail-card">
                <h3>Order lines</h3>
                <ul className="line-summary">
                  {orderDetail.lines.map((line) => (
                    <li key={`${line.sku}-${line.productName}`}>
                      <div>
                        <strong>{line.productName}</strong>
                        <span>{line.sku}</span>
                      </div>
                      <div>
                        <strong>{line.quantity} x</strong>
                        <span>{formatCurrency(line.lineTotal)}</span>
                      </div>
                    </li>
                  ))}
                </ul>
              </section>
            </div>
          ) : (
            <p className="empty-state">Select an order to inspect the distributed workflow state.</p>
          )}
        </section>
      </section>

      <section className="workspace-grid secondary-grid">
        <section className="panel">
          <div className="panel-heading">
            <div>
              <p className="eyebrow">Catalog and inventory</p>
              <h2>Sellable product lookup</h2>
            </div>
          </div>

          <div className="catalog-layout">
            <div className="catalog-list">
              {products.map((product) => (
                <button
                  type="button"
                  key={product.sku}
                  className={`catalog-card ${selectedSku === product.sku ? 'selected-card' : ''}`}
                  onClick={() => setSelectedSku(product.sku)}
                >
                  <strong>{product.name}</strong>
                  <span>{product.sku}</span>
                  <span>{formatCurrency(product.unitPrice)}</span>
                </button>
              ))}
            </div>

            <div className="availability-card">
              <h3>{selectedSku || 'No product selected'}</h3>
              {availability ? (
                <>
                  <p className="availability-total">
                    {availability.availableQuantity} available / {availability.reservedQuantity} reserved
                  </p>
                  <ul>
                    {availability.warehouses.map((warehouse) => (
                      <li key={warehouse.warehouseCode}>
                        <strong>{warehouse.warehouseCode}</strong>
                        <span>{warehouse.availableQuantity} available</span>
                        <span>{warehouse.reservedQuantity} reserved</span>
                      </li>
                    ))}
                  </ul>
                </>
              ) : (
                <p className="empty-state">Select a product to inspect warehouse availability.</p>
              )}
            </div>
          </div>
        </section>

        <section className="panel inspector-panel">
          <div className="panel-heading">
            <div>
              <p className="eyebrow">Fulfillment lane</p>
              <h2>{selectedShipment?.trackingReference ?? 'No shipment selected'}</h2>
            </div>
            {selectedShipment ? (
              <span className={`status-pill ${statusTone(selectedShipment.status)}`}>{formatStatus(selectedShipment.status)}</span>
            ) : null}
          </div>

          <div className="shipment-layout">
            <div className="shipment-list">
              {shipments.map((shipment) => (
                <button
                  type="button"
                  key={shipment.shipmentId}
                  className={`shipment-card ${shipment.shipmentId === selectedShipmentId ? 'selected-card' : ''}`}
                  onClick={() => setSelectedShipmentId(shipment.shipmentId)}
                >
                  <strong>{shipment.orderNumber}</strong>
                  <span>{shipment.trackingReference}</span>
                  <span>{formatStatus(shipment.status)}</span>
                </button>
              ))}
            </div>

            <div className="shipment-detail">
              {selectedShipment ? (
                <>
                  <dl>
                    <div>
                      <dt>Warehouse</dt>
                      <dd>{selectedShipment.warehouseCode}</dd>
                    </div>
                    <div>
                      <dt>Status</dt>
                      <dd>{formatStatus(selectedShipment.status)}</dd>
                    </div>
                    <div>
                      <dt>Updated</dt>
                      <dd>{new Date(selectedShipment.updatedAt).toLocaleString()}</dd>
                    </div>
                  </dl>
                  <div className="form-actions">
                    <button
                      type="button"
                      className="secondary-button"
                      disabled={!canProgressFulfillment || selectedShipment.status !== 'Pending'}
                      onClick={() => void handleProgressShipment('pick')}
                    >
                      Pick
                    </button>
                    <button
                      type="button"
                      className="secondary-button"
                      disabled={!canProgressFulfillment || selectedShipment.status !== 'Picking'}
                      onClick={() => void handleProgressShipment('pack')}
                    >
                      Pack
                    </button>
                    <button
                      type="button"
                      className="secondary-button"
                      disabled={!canProgressFulfillment || selectedShipment.status !== 'Packed'}
                      onClick={() => void handleProgressShipment('ship')}
                    >
                      Ship
                    </button>
                    <button
                      type="button"
                      className="primary-button"
                      disabled={!canProgressFulfillment || selectedShipment.status !== 'Shipped'}
                      onClick={() => void handleProgressShipment('deliver')}
                    >
                      Deliver
                    </button>
                  </div>
                </>
              ) : (
                <p className="empty-state">Fulfillment starts after an order reaches ready-for-fulfillment state.</p>
              )}
            </div>
          </div>
        </section>
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

function statusTone(status: string | null) {
  if (!status) {
    return 'neutral'
  }

  const normalized = status.toLowerCase()
  if (normalized.includes('failed') || normalized.includes('rejected') || normalized.includes('cancelled')) {
    return 'danger'
  }
  if (
    normalized.includes('ready') ||
    normalized.includes('authorized') ||
    normalized.includes('reserved') ||
    normalized.includes('completed') ||
    normalized.includes('delivered')
  ) {
    return 'success'
  }
  return 'neutral'
}

function formatStatus(status: string) {
  return status.replace(/([a-z])([A-Z])/g, '$1 $2')
}

function formatCurrency(value: number) {
  return new Intl.NumberFormat('en-AU', { style: 'currency', currency: 'AUD' }).format(value)
}

export default App

async function withRetry<T>(operation: () => Promise<T>, attempts = 4, delayMs = 1_500): Promise<T> {
  let lastError: unknown

  for (let attempt = 1; attempt <= attempts; attempt += 1) {
    try {
      return await operation()
    } catch (error) {
      lastError = error
      if (attempt < attempts) {
        await new Promise((resolve) => window.setTimeout(resolve, delayMs))
      }
    }
  }

  throw lastError
}

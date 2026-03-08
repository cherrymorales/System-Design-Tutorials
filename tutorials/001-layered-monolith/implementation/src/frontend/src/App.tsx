
import { useEffect, useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import './App.css'
import { fetchJson } from './api'
import {
  createEmptyAdjustmentForm,
  createEmptyReceiptForm,
  createEmptyTransferForm,
  demoUsers,
  emptyProductForm,
  emptyWarehouseForm,
  type AdjustmentFormState,
  type HealthResponse,
  type InventoryAdjustment,
  type InventoryReceipt,
  type InventorySummaryItem,
  type LowStockItem,
  type Product,
  type ProductFormState,
  type ReceiptFormState,
  type StockTransfer,
  type TransferFormState,
  type Warehouse,
  type WarehouseFormState,
} from './models'

function App() {
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

  const selectedProduct = useMemo(() => products.find((item) => item.id === selectedProductId) ?? null, [products, selectedProductId])
  const selectedWarehouse = useMemo(() => warehouses.find((item) => item.id === selectedWarehouseId) ?? null, [warehouses, selectedWarehouseId])
  const activeProducts = useMemo(() => products.filter((item) => item.status === 'Active').length, [products])
  const activeWarehouses = useMemo(() => warehouses.filter((item) => item.status === 'Active').length, [warehouses])
  const pendingAdjustments = useMemo(() => adjustments.filter((item) => item.status === 'PendingApproval'), [adjustments])
  const openTransfers = useMemo(() => transfers.filter((item) => !['Received', 'Cancelled'].includes(item.status)), [transfers])

  useEffect(() => {
    void loadDashboard()
  }, [])

  useEffect(() => {
    if (selectedProduct) {
      setProductForm({
        sku: selectedProduct.sku,
        name: selectedProduct.name,
        category: selectedProduct.category,
        supplierCode: selectedProduct.supplierCode,
        unitCost: selectedProduct.unitCost.toString(),
      })
    }
  }, [selectedProduct])

  useEffect(() => {
    if (selectedWarehouse) {
      setWarehouseForm({
        code: selectedWarehouse.code,
        name: selectedWarehouse.name,
        city: selectedWarehouse.city,
      })
    }
  }, [selectedWarehouse])

  async function loadDashboard(showRefreshState = false) {
    if (showRefreshState) {
      setIsRefreshing(true)
    } else {
      setIsLoading(true)
    }

    setDashboardError(null)

    try {
      const [healthPayload, productsPayload, warehousesPayload, inventoryPayload, receiptsPayload, transfersPayload, adjustmentsPayload, lowStockPayload] =
        await Promise.all([
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
      setDashboardError(error instanceof Error ? error.message : 'Failed to load dashboard data.')
    } finally {
      setIsLoading(false)
      setIsRefreshing(false)
    }
  }

  async function submitCatalogAction(action: string, work: () => Promise<void>) {
    setBusyAction(action)
    setCatalogError(null)

    try {
      await work()
      await loadDashboard(true)
    } catch (error) {
      setCatalogError(error instanceof Error ? error.message : 'Catalog request failed.')
    } finally {
      setBusyAction(null)
    }
  }

  async function submitWorkflowAction(action: string, work: () => Promise<void>) {
    setBusyAction(action)
    setWorkflowError(null)

    try {
      await work()
      await loadDashboard(true)
    } catch (error) {
      setWorkflowError(error instanceof Error ? error.message : 'Workflow request failed.')
    } finally {
      setBusyAction(null)
    }
  }

  async function handleCreateProduct(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    await submitCatalogAction('create-product', async () => {
      await fetchJson('/api/products', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ ...productForm, unitCost: Number(productForm.unitCost) }),
      })
      setProductForm(emptyProductForm)
    })
  }

  async function handleUpdateProduct(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!selectedProduct) return
    await submitCatalogAction(`update-product-${selectedProduct.id}`, async () => {
      await fetchJson(`/api/products/${selectedProduct.id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name: productForm.name,
          category: productForm.category,
          supplierCode: productForm.supplierCode,
          unitCost: Number(productForm.unitCost),
        }),
      })
    })
  }

  async function handleCreateWarehouse(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    await submitCatalogAction('create-warehouse', async () => {
      await fetchJson('/api/warehouses', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(warehouseForm),
      })
      setWarehouseForm(emptyWarehouseForm)
    })
  }

  async function handleUpdateWarehouse(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!selectedWarehouse) return
    await submitCatalogAction(`update-warehouse-${selectedWarehouse.id}`, async () => {
      await fetchJson(`/api/warehouses/${selectedWarehouse.id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name: warehouseForm.name, city: warehouseForm.city }),
      })
    })
  }

  async function handleArchiveProduct(product: Product) {
    await submitCatalogAction(`archive-product-${product.id}`, async () => {
      await fetchJson(`/api/products/${product.id}/archive`, { method: 'POST' })
      if (selectedProductId === product.id) {
        setSelectedProductId(null)
        setProductForm(emptyProductForm)
      }
    })
  }

  async function handleDeactivateWarehouse(warehouse: Warehouse) {
    await submitCatalogAction(`deactivate-warehouse-${warehouse.id}`, async () => {
      await fetchJson(`/api/warehouses/${warehouse.id}/deactivate`, { method: 'POST' })
      if (selectedWarehouseId === warehouse.id) {
        setSelectedWarehouseId(null)
        setWarehouseForm(emptyWarehouseForm)
      }
    })
  }
  async function handleCreateReceipt(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    await submitWorkflowAction('create-receipt', async () => {
      await fetchJson('/api/inventory/receipts', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          warehouseId: receiptForm.warehouseId,
          productId: receiptForm.productId,
          quantityReceived: Number(receiptForm.quantityReceived),
          supplierReference: receiptForm.supplierReference,
          receivedBy: receiptForm.receivedBy,
        }),
      })
      setReceiptForm(createEmptyReceiptForm())
    })
  }

  async function handleCreateTransfer(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    await submitWorkflowAction('create-transfer', async () => {
      await fetchJson('/api/transfers', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          sourceWarehouseId: transferForm.sourceWarehouseId,
          destinationWarehouseId: transferForm.destinationWarehouseId,
          productId: transferForm.productId,
          quantity: Number(transferForm.quantity),
          requestedBy: transferForm.requestedBy,
          reason: transferForm.reason,
        }),
      })
      setTransferForm(createEmptyTransferForm())
    })
  }

  async function handleCreateAdjustment(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    await submitWorkflowAction('create-adjustment', async () => {
      await fetchJson('/api/adjustments', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          warehouseId: adjustmentForm.warehouseId,
          productId: adjustmentForm.productId,
          quantityDelta: Number(adjustmentForm.quantityDelta),
          reasonCode: adjustmentForm.reasonCode,
          submittedBy: adjustmentForm.submittedBy,
          notes: adjustmentForm.notes,
        }),
      })
      setAdjustmentForm(createEmptyAdjustmentForm())
    })
  }

  async function advanceTransfer(transfer: StockTransfer, action: 'approve' | 'dispatch' | 'receive' | 'cancel') {
    const payload =
      action === 'approve'
        ? { approvedBy: demoUsers.manager }
        : action === 'dispatch'
          ? { dispatchedBy: demoUsers.operator }
          : action === 'receive'
            ? { receivedBy: demoUsers.operator }
            : { cancelledBy: demoUsers.manager, cancellationReason: 'Cancelled from Phase 3 console' }

    await submitWorkflowAction(`${action}-transfer-${transfer.id}`, async () => {
      await fetchJson(`/api/transfers/${transfer.id}/${action}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      })
    })
  }

  async function reviewAdjustment(adjustment: InventoryAdjustment, action: 'approve' | 'reject') {
    const payload = action === 'approve'
      ? { approvedBy: demoUsers.manager, notes: 'Reviewed in Phase 3 console' }
      : { rejectedBy: demoUsers.manager, notes: 'Rejected in Phase 3 console' }

    await submitWorkflowAction(`${action}-adjustment-${adjustment.id}`, async () => {
      await fetchJson(`/api/adjustments/${adjustment.id}/${action}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      })
    })
  }

  return (
    <main className="page-shell">
      <section className="hero-panel">
        <div>
          <p className="eyebrow">001 Layered Monolith</p>
          <h1>Operational inventory workflows</h1>
          <p className="intro">
            Phase 3 adds the stock-changing backbone: receiving inventory, moving stock between warehouses,
            and handling adjustment approvals while keeping the monolith transactional and easy to reason about.
          </p>
        </div>
        <div className="hero-actions">
          <button className="secondary-button" onClick={() => void loadDashboard(true)} disabled={isRefreshing}>
            {isRefreshing ? 'Refreshing...' : 'Refresh data'}
          </button>
          <div className={`health-chip ${health ? 'health-chip--ok' : 'health-chip--pending'}`}>
            <span>{health?.status ?? 'loading'}</span>
            <small>{health ? new Date(health.timestamp).toLocaleString() : 'Waiting for API'}</small>
          </div>
        </div>
      </section>

      {dashboardError ? <p className="banner banner--error">{dashboardError}</p> : null}
      {isLoading ? <p className="banner">Loading dashboard data...</p> : null}

      <section className="stats-grid">
        <article className="stat-card"><span className="stat-label">Active products</span><strong>{activeProducts}</strong><small>{products.length} total catalog records</small></article>
        <article className="stat-card"><span className="stat-label">Active warehouses</span><strong>{activeWarehouses}</strong><small>{warehouses.length} active sites in scope</small></article>
        <article className="stat-card"><span className="stat-label">Open transfers</span><strong>{openTransfers.length}</strong><small>requested, approved, or dispatched</small></article>
        <article className="stat-card"><span className="stat-label">Pending adjustments</span><strong>{pendingAdjustments.length}</strong><small>manager review queue</small></article>
      </section>

      <section className="workspace-grid workspace-grid--catalog">
        <article className="panel">
          <div className="panel-heading"><div><p className="eyebrow">Catalog</p><h2>Products</h2></div><span className="pill">{products.length} records</span></div>
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
          {catalogError ? <p className="banner banner--error">{catalogError}</p> : null}
          <div className="entity-list">
            {products.map((product) => (
              <div key={product.id} className={`entity-card ${selectedProductId === product.id ? 'entity-card--selected' : ''}`}>
                <div><div className="entity-row"><strong>{product.name}</strong><span className={`pill ${product.status === 'Active' ? 'pill--active' : 'pill--archived'}`}>{product.status}</span></div><p>{product.sku}</p><small>{product.category} · {product.supplierCode} · ${product.unitCost.toFixed(2)}</small></div>
                <div className="card-actions"><button className="secondary-button" onClick={() => setSelectedProductId(product.id)}>Edit</button><button className="ghost-button" disabled={product.status !== 'Active' || busyAction !== null} onClick={() => void handleArchiveProduct(product)}>Archive</button></div>
              </div>
            ))}
          </div>
        </article>

        <article className="panel">
          <div className="panel-heading"><div><p className="eyebrow">Network</p><h2>Warehouses</h2></div><span className="pill">{warehouses.length} sites</span></div>
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
          <div className="entity-list">
            {warehouses.map((warehouse) => (
              <div key={warehouse.id} className={`entity-card ${selectedWarehouseId === warehouse.id ? 'entity-card--selected' : ''}`}>
                <div><div className="entity-row"><strong>{warehouse.name}</strong><span className={`pill ${warehouse.status === 'Active' ? 'pill--active' : 'pill--archived'}`}>{warehouse.status}</span></div><p>{warehouse.code} · {warehouse.city}</p><small>{warehouse.totalSkuCount} SKU positions · {warehouse.lowStockSkuCount} low stock</small></div>
                <div className="card-actions"><button className="secondary-button" onClick={() => setSelectedWarehouseId(warehouse.id)}>Edit</button><button className="ghost-button" disabled={warehouse.status !== 'Active' || busyAction !== null} onClick={() => void handleDeactivateWarehouse(warehouse)}>Deactivate</button></div>
              </div>
            ))}
          </div>
        </article>
      </section>
      <section className="workspace-grid workspace-grid--operations">
        <article className="panel">
          <div className="panel-heading"><div><p className="eyebrow">Receipts</p><h2>Receive stock</h2></div><span className="pill">{receipts.length} receipts</span></div>
          <form className="entity-form" onSubmit={handleCreateReceipt}>
            <div className="form-grid">
              <label><span>Warehouse</span><select value={receiptForm.warehouseId} onChange={(event) => setReceiptForm((current) => ({ ...current, warehouseId: event.target.value }))}><option value="">Select warehouse</option>{warehouses.map((warehouse) => <option key={warehouse.id} value={warehouse.id}>{warehouse.code} · {warehouse.name}</option>)}</select></label>
              <label><span>Product</span><select value={receiptForm.productId} onChange={(event) => setReceiptForm((current) => ({ ...current, productId: event.target.value }))}><option value="">Select product</option>{products.filter((product) => product.status === 'Active').map((product) => <option key={product.id} value={product.id}>{product.sku} · {product.name}</option>)}</select></label>
              <label><span>Quantity</span><input type="number" min="1" value={receiptForm.quantityReceived} onChange={(event) => setReceiptForm((current) => ({ ...current, quantityReceived: event.target.value }))} /></label>
              <label><span>Supplier reference</span><input value={receiptForm.supplierReference} onChange={(event) => setReceiptForm((current) => ({ ...current, supplierReference: event.target.value }))} /></label>
              <label><span>Received by</span><select value={receiptForm.receivedBy} onChange={(event) => setReceiptForm((current) => ({ ...current, receivedBy: event.target.value }))}>{Object.values(demoUsers).map((user) => <option key={user} value={user}>{user}</option>)}</select></label>
            </div>
            <div className="form-actions"><button className="primary-button" type="submit" disabled={busyAction !== null}>Record receipt</button></div>
          </form>
          {workflowError ? <p className="banner banner--error">{workflowError}</p> : null}
          <div className="compact-list">{receipts.slice(0, 4).map((receipt) => <div key={receipt.id} className="compact-item"><strong>{receipt.productSku}</strong><span>{receipt.warehouseCode} · +{receipt.quantityReceived} · {new Date(receipt.receivedAt).toLocaleString()}</span></div>)}</div>
        </article>

        <article className="panel">
          <div className="panel-heading"><div><p className="eyebrow">Transfers</p><h2>Move stock</h2></div><span className="pill">{openTransfers.length} open</span></div>
          <form className="entity-form" onSubmit={handleCreateTransfer}>
            <div className="form-grid">
              <label><span>Source</span><select value={transferForm.sourceWarehouseId} onChange={(event) => setTransferForm((current) => ({ ...current, sourceWarehouseId: event.target.value }))}><option value="">Select source</option>{warehouses.filter((warehouse) => warehouse.status === 'Active').map((warehouse) => <option key={warehouse.id} value={warehouse.id}>{warehouse.code} · {warehouse.name}</option>)}</select></label>
              <label><span>Destination</span><select value={transferForm.destinationWarehouseId} onChange={(event) => setTransferForm((current) => ({ ...current, destinationWarehouseId: event.target.value }))}><option value="">Select destination</option>{warehouses.filter((warehouse) => warehouse.status === 'Active').map((warehouse) => <option key={warehouse.id} value={warehouse.id}>{warehouse.code} · {warehouse.name}</option>)}</select></label>
              <label><span>Product</span><select value={transferForm.productId} onChange={(event) => setTransferForm((current) => ({ ...current, productId: event.target.value }))}><option value="">Select product</option>{products.filter((product) => product.status === 'Active').map((product) => <option key={product.id} value={product.id}>{product.sku} · {product.name}</option>)}</select></label>
              <label><span>Quantity</span><input type="number" min="1" value={transferForm.quantity} onChange={(event) => setTransferForm((current) => ({ ...current, quantity: event.target.value }))} /></label>
              <label><span>Requested by</span><select value={transferForm.requestedBy} onChange={(event) => setTransferForm((current) => ({ ...current, requestedBy: event.target.value }))}>{Object.values(demoUsers).map((user) => <option key={user} value={user}>{user}</option>)}</select></label>
              <label><span>Reason</span><input value={transferForm.reason} onChange={(event) => setTransferForm((current) => ({ ...current, reason: event.target.value }))} /></label>
            </div>
            <div className="form-actions"><button className="primary-button" type="submit" disabled={busyAction !== null}>Create transfer</button></div>
          </form>
          <div className="compact-list">{transfers.slice(0, 5).map((transfer) => <div key={transfer.id} className="compact-item"><strong>{transfer.productSku} · {transfer.quantity}</strong><span>{transfer.sourceWarehouseCode} to {transfer.destinationWarehouseCode} · {transfer.status}</span><div className="inline-actions">{transfer.status === 'Requested' ? <button className="secondary-button" onClick={() => void advanceTransfer(transfer, 'approve')}>Approve</button> : null}{transfer.status === 'Approved' ? <button className="secondary-button" onClick={() => void advanceTransfer(transfer, 'dispatch')}>Dispatch</button> : null}{transfer.status === 'Dispatched' ? <button className="secondary-button" onClick={() => void advanceTransfer(transfer, 'receive')}>Receive</button> : null}{['Requested', 'Approved'].includes(transfer.status) ? <button className="ghost-button" onClick={() => void advanceTransfer(transfer, 'cancel')}>Cancel</button> : null}</div></div>)}</div>
        </article>

        <article className="panel">
          <div className="panel-heading"><div><p className="eyebrow">Adjustments</p><h2>Submit stock changes</h2></div><span className="pill">{adjustments.length} total</span></div>
          <form className="entity-form" onSubmit={handleCreateAdjustment}>
            <div className="form-grid">
              <label><span>Warehouse</span><select value={adjustmentForm.warehouseId} onChange={(event) => setAdjustmentForm((current) => ({ ...current, warehouseId: event.target.value }))}><option value="">Select warehouse</option>{warehouses.filter((warehouse) => warehouse.status === 'Active').map((warehouse) => <option key={warehouse.id} value={warehouse.id}>{warehouse.code} · {warehouse.name}</option>)}</select></label>
              <label><span>Product</span><select value={adjustmentForm.productId} onChange={(event) => setAdjustmentForm((current) => ({ ...current, productId: event.target.value }))}><option value="">Select product</option>{products.filter((product) => product.status === 'Active').map((product) => <option key={product.id} value={product.id}>{product.sku} · {product.name}</option>)}</select></label>
              <label><span>Quantity delta</span><input type="number" value={adjustmentForm.quantityDelta} onChange={(event) => setAdjustmentForm((current) => ({ ...current, quantityDelta: event.target.value }))} /></label>
              <label><span>Reason code</span><input value={adjustmentForm.reasonCode} onChange={(event) => setAdjustmentForm((current) => ({ ...current, reasonCode: event.target.value }))} /></label>
              <label><span>Submitted by</span><select value={adjustmentForm.submittedBy} onChange={(event) => setAdjustmentForm((current) => ({ ...current, submittedBy: event.target.value }))}>{Object.values(demoUsers).map((user) => <option key={user} value={user}>{user}</option>)}</select></label>
              <label><span>Notes</span><input value={adjustmentForm.notes} onChange={(event) => setAdjustmentForm((current) => ({ ...current, notes: event.target.value }))} /></label>
            </div>
            <div className="form-actions"><button className="primary-button" type="submit" disabled={busyAction !== null}>Submit adjustment</button></div>
          </form>
          <div className="compact-list">{adjustments.slice(0, 5).map((adjustment) => <div key={adjustment.id} className="compact-item"><strong>{adjustment.productSku} · {adjustment.quantityDelta > 0 ? '+' : ''}{adjustment.quantityDelta}</strong><span>{adjustment.warehouseCode} · {adjustment.reasonCode} · {adjustment.status}</span><div className="inline-actions">{adjustment.status === 'PendingApproval' ? <button className="secondary-button" onClick={() => void reviewAdjustment(adjustment, 'approve')}>Approve</button> : null}{adjustment.status === 'PendingApproval' ? <button className="ghost-button" onClick={() => void reviewAdjustment(adjustment, 'reject')}>Reject</button> : null}</div></div>)}</div>
        </article>
      </section>

      <section className="workspace-grid workspace-grid--reports">
        <article className="panel inventory-panel">
          <div className="panel-heading"><div><p className="eyebrow">Low stock</p><h2>Reorder watchlist</h2></div><span className="pill">{lowStock.length} items</span></div>
          <div className="inventory-table-wrapper"><table className="inventory-table"><thead><tr><th>Warehouse</th><th>Product</th><th>Available</th><th>Threshold</th><th>Shortfall</th></tr></thead><tbody>{lowStock.map((item) => <tr key={item.inventoryItemId}><td><strong>{item.warehouseCode}</strong><span>{item.warehouseName}</span></td><td><strong>{item.productSku}</strong><span>{item.productName}</span></td><td>{item.availableQuantity}</td><td>{item.reorderThreshold}</td><td><span className="pill pill--warning">{item.shortfallQuantity}</span></td></tr>)}</tbody></table></div>
        </article>

        <article className="panel inventory-panel">
          <div className="panel-heading"><div><p className="eyebrow">Inventory</p><h2>Current stock summary</h2></div><span className="pill">{inventory.length} rows</span></div>
          <div className="inventory-table-wrapper"><table className="inventory-table"><thead><tr><th>Warehouse</th><th>Product</th><th>On hand</th><th>Reserved</th><th>Available</th><th>Status</th></tr></thead><tbody>{inventory.map((item) => <tr key={item.id}><td><strong>{item.warehouseCode}</strong><span>{item.warehouseName}</span></td><td><strong>{item.productSku}</strong><span>{item.productName}</span></td><td>{item.quantityOnHand}</td><td>{item.quantityReserved}</td><td>{item.availableQuantity}</td><td><span className={`pill ${item.isLowStock ? 'pill--warning' : 'pill--active'}`}>{item.isLowStock ? 'Low stock' : 'Healthy'}</span></td></tr>)}</tbody></table></div>
        </article>
      </section>
    </main>
  )
}

export default App


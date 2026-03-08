import { useEffect, useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import './App.css'

type HealthResponse = {
  status: string
  service: string
  timestamp: string
}

type Product = {
  id: string
  sku: string
  name: string
  category: string
  supplierCode: string
  unitCost: number
  status: string
}

type Warehouse = {
  id: string
  code: string
  name: string
  city: string
  status: string
  totalSkuCount: number
  lowStockSkuCount: number
}

type InventoryItem = {
  id: string
  productId: string
  productSku: string
  productName: string
  warehouseId: string
  warehouseCode: string
  warehouseName: string
  quantityOnHand: number
  quantityReserved: number
  availableQuantity: number
  reorderThreshold: number
  isLowStock: boolean
  updatedAt: string
}

type ProductFormState = {
  sku: string
  name: string
  category: string
  supplierCode: string
  unitCost: string
}

type WarehouseFormState = {
  code: string
  name: string
  city: string
}

const emptyProductForm: ProductFormState = {
  sku: '',
  name: '',
  category: '',
  supplierCode: '',
  unitCost: '',
}

const emptyWarehouseForm: WarehouseFormState = {
  code: '',
  name: '',
  city: '',
}

function App() {
  const [health, setHealth] = useState<HealthResponse | null>(null)
  const [products, setProducts] = useState<Product[]>([])
  const [warehouses, setWarehouses] = useState<Warehouse[]>([])
  const [inventory, setInventory] = useState<InventoryItem[]>([])
  const [dashboardError, setDashboardError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isRefreshing, setIsRefreshing] = useState(false)
  const [busyAction, setBusyAction] = useState<string | null>(null)
  const [productError, setProductError] = useState<string | null>(null)
  const [warehouseError, setWarehouseError] = useState<string | null>(null)
  const [productForm, setProductForm] = useState<ProductFormState>(emptyProductForm)
  const [warehouseForm, setWarehouseForm] = useState<WarehouseFormState>(emptyWarehouseForm)
  const [selectedProductId, setSelectedProductId] = useState<string | null>(null)
  const [selectedWarehouseId, setSelectedWarehouseId] = useState<string | null>(null)

  const selectedProduct = useMemo(
    () => products.find((item) => item.id === selectedProductId) ?? null,
    [products, selectedProductId],
  )

  const selectedWarehouse = useMemo(
    () => warehouses.find((item) => item.id === selectedWarehouseId) ?? null,
    [warehouses, selectedWarehouseId],
  )

  useEffect(() => {
    void loadDashboard()
  }, [])

  useEffect(() => {
    if (!selectedProduct) {
      setProductForm((current) => (selectedProductId ? current : emptyProductForm))
      return
    }

    setProductForm({
      sku: selectedProduct.sku,
      name: selectedProduct.name,
      category: selectedProduct.category,
      supplierCode: selectedProduct.supplierCode,
      unitCost: selectedProduct.unitCost.toString(),
    })
  }, [selectedProduct, selectedProductId])

  useEffect(() => {
    if (!selectedWarehouse) {
      setWarehouseForm((current) => (selectedWarehouseId ? current : emptyWarehouseForm))
      return
    }

    setWarehouseForm({
      code: selectedWarehouse.code,
      name: selectedWarehouse.name,
      city: selectedWarehouse.city,
    })
  }, [selectedWarehouse, selectedWarehouseId])

  const lowStockItems = useMemo(() => inventory.filter((item) => item.isLowStock), [inventory])
  const activeProducts = useMemo(() => products.filter((item) => item.status === 'Active').length, [products])
  const activeWarehouses = useMemo(() => warehouses.filter((item) => item.status === 'Active').length, [warehouses])

  async function loadDashboard(showRefreshState = false) {
    if (showRefreshState) {
      setIsRefreshing(true)
    } else {
      setIsLoading(true)
    }

    setDashboardError(null)

    try {
      const [healthPayload, productsPayload, warehousesPayload, inventoryPayload] = await Promise.all([
        fetchJson<HealthResponse>('/api/health'),
        fetchJson<Product[]>('/api/products'),
        fetchJson<Warehouse[]>('/api/warehouses'),
        fetchJson<InventoryItem[]>('/api/inventory/summary'),
      ])

      setHealth(healthPayload)
      setProducts(productsPayload)
      setWarehouses(warehousesPayload)
      setInventory(inventoryPayload)
    } catch (error) {
      setDashboardError(error instanceof Error ? error.message : 'Failed to load dashboard data.')
    } finally {
      setIsLoading(false)
      setIsRefreshing(false)
    }
  }

  async function handleCreateProduct(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setBusyAction('create-product')
    setProductError(null)

    try {
      await fetchJson<Product>('/api/products', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          sku: productForm.sku,
          name: productForm.name,
          category: productForm.category,
          supplierCode: productForm.supplierCode,
          unitCost: Number(productForm.unitCost),
        }),
      })

      setProductForm(emptyProductForm)
      await loadDashboard(true)
    } catch (error) {
      setProductError(error instanceof Error ? error.message : 'Failed to create product.')
    } finally {
      setBusyAction(null)
    }
  }

  async function handleUpdateProduct(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!selectedProduct) {
      return
    }

    setBusyAction(`update-product-${selectedProduct.id}`)
    setProductError(null)

    try {
      await fetchJson<Product>(`/api/products/${selectedProduct.id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name: productForm.name,
          category: productForm.category,
          supplierCode: productForm.supplierCode,
          unitCost: Number(productForm.unitCost),
        }),
      })

      await loadDashboard(true)
    } catch (error) {
      setProductError(error instanceof Error ? error.message : 'Failed to update product.')
    } finally {
      setBusyAction(null)
    }
  }

  async function handleArchiveProduct(product: Product) {
    if (!window.confirm(`Archive product ${product.sku}?`)) {
      return
    }

    setBusyAction(`archive-product-${product.id}`)
    setProductError(null)

    try {
      await fetchJson<Product>(`/api/products/${product.id}/archive`, {
        method: 'POST',
      })

      if (selectedProductId === product.id) {
        setSelectedProductId(null)
      }

      await loadDashboard(true)
    } catch (error) {
      setProductError(error instanceof Error ? error.message : 'Failed to archive product.')
    } finally {
      setBusyAction(null)
    }
  }

  async function handleCreateWarehouse(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setBusyAction('create-warehouse')
    setWarehouseError(null)

    try {
      await fetchJson<Warehouse>('/api/warehouses', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          code: warehouseForm.code,
          name: warehouseForm.name,
          city: warehouseForm.city,
        }),
      })

      setWarehouseForm(emptyWarehouseForm)
      await loadDashboard(true)
    } catch (error) {
      setWarehouseError(error instanceof Error ? error.message : 'Failed to create warehouse.')
    } finally {
      setBusyAction(null)
    }
  }

  async function handleUpdateWarehouse(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!selectedWarehouse) {
      return
    }

    setBusyAction(`update-warehouse-${selectedWarehouse.id}`)
    setWarehouseError(null)

    try {
      await fetchJson<Warehouse>(`/api/warehouses/${selectedWarehouse.id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name: warehouseForm.name,
          city: warehouseForm.city,
        }),
      })

      await loadDashboard(true)
    } catch (error) {
      setWarehouseError(error instanceof Error ? error.message : 'Failed to update warehouse.')
    } finally {
      setBusyAction(null)
    }
  }

  async function handleDeactivateWarehouse(warehouse: Warehouse) {
    if (!window.confirm(`Deactivate warehouse ${warehouse.code}?`)) {
      return
    }

    setBusyAction(`deactivate-warehouse-${warehouse.id}`)
    setWarehouseError(null)

    try {
      await fetchJson<Warehouse>(`/api/warehouses/${warehouse.id}/deactivate`, {
        method: 'POST',
      })

      if (selectedWarehouseId === warehouse.id) {
        setSelectedWarehouseId(null)
      }

      await loadDashboard(true)
    } catch (error) {
      setWarehouseError(error instanceof Error ? error.message : 'Failed to deactivate warehouse.')
    } finally {
      setBusyAction(null)
    }
  }

  return (
    <main className="page-shell">
      <section className="hero-panel">
        <div>
          <p className="eyebrow">001 Layered Monolith</p>
          <h1>Inventory and warehouse operations console</h1>
          <p className="intro">
            Phase 2 adds seeded reference data plus concrete product and warehouse management over the
            layered monolith backend. This screen now exercises the same APIs the later receipt, transfer,
            and adjustment workflows will build on.
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

      <section className="stats-grid">
        <article className="stat-card">
          <span className="stat-label">Active products</span>
          <strong>{activeProducts}</strong>
          <small>{products.length} total products seeded and managed here</small>
        </article>
        <article className="stat-card">
          <span className="stat-label">Active warehouses</span>
          <strong>{activeWarehouses}</strong>
          <small>{warehouses.length} warehouses currently tracked</small>
        </article>
        <article className="stat-card">
          <span className="stat-label">Low stock positions</span>
          <strong>{lowStockItems.length}</strong>
          <small>Driven from current inventory summary rows</small>
        </article>
        <article className="stat-card stat-card--credentials">
          <span className="stat-label">Seeded users</span>
          <strong>4 demo accounts</strong>
          <small>manager, planner, purchasing, and Brisbane operator</small>
        </article>
      </section>

      <section className="workspace-grid">
        <article className="panel">
          <div className="panel-heading">
            <div>
              <p className="eyebrow">Catalog</p>
              <h2>Products</h2>
            </div>
            <span className="pill">{products.length} records</span>
          </div>

          <form className="entity-form" onSubmit={selectedProduct ? handleUpdateProduct : handleCreateProduct}>
            <div className="form-grid">
              <label>
                <span>SKU</span>
                <input
                  value={productForm.sku}
                  onChange={(event) => setProductForm((current) => ({ ...current, sku: event.target.value }))}
                  disabled={Boolean(selectedProduct)}
                  placeholder="LAP-14-BLK"
                />
              </label>
              <label>
                <span>Name</span>
                <input
                  value={productForm.name}
                  onChange={(event) => setProductForm((current) => ({ ...current, name: event.target.value }))}
                  placeholder="14 Inch Laptop"
                />
              </label>
              <label>
                <span>Category</span>
                <input
                  value={productForm.category}
                  onChange={(event) => setProductForm((current) => ({ ...current, category: event.target.value }))}
                  placeholder="Computers"
                />
              </label>
              <label>
                <span>Supplier code</span>
                <input
                  value={productForm.supplierCode}
                  onChange={(event) => setProductForm((current) => ({ ...current, supplierCode: event.target.value }))}
                  placeholder="SUP-TECH"
                />
              </label>
              <label>
                <span>Unit cost</span>
                <input
                  type="number"
                  min="0"
                  step="0.01"
                  value={productForm.unitCost}
                  onChange={(event) => setProductForm((current) => ({ ...current, unitCost: event.target.value }))}
                  placeholder="1299.00"
                />
              </label>
            </div>

            <div className="form-actions">
              <button className="primary-button" type="submit" disabled={busyAction !== null}>
                {selectedProduct ? 'Save product' : 'Create product'}
              </button>
              {selectedProduct ? (
                <button
                  className="secondary-button"
                  type="button"
                  onClick={() => {
                    setSelectedProductId(null)
                    setProductForm(emptyProductForm)
                    setProductError(null)
                  }}>
                  Cancel edit
                </button>
              ) : null}
            </div>
          </form>

          {productError ? <p className="banner banner--error">{productError}</p> : null}

          <div className="entity-list">
            {isLoading ? <p className="empty-state">Loading products...</p> : null}
            {!isLoading && products.length === 0 ? <p className="empty-state">No products found.</p> : null}
            {products.map((product) => (
              <div key={product.id} className={`entity-card ${selectedProductId === product.id ? 'entity-card--selected' : ''}`}>
                <div>
                  <div className="entity-row">
                    <strong>{product.name}</strong>
                    <span className={`pill ${product.status === 'Active' ? 'pill--active' : 'pill--archived'}`}>
                      {product.status}
                    </span>
                  </div>
                  <p>{product.sku}</p>
                  <small>
                    {product.category} · {product.supplierCode} · ${product.unitCost.toFixed(2)}
                  </small>
                </div>
                <div className="card-actions">
                  <button className="secondary-button" onClick={() => setSelectedProductId(product.id)}>
                    Edit
                  </button>
                  <button
                    className="ghost-button"
                    onClick={() => void handleArchiveProduct(product)}
                    disabled={product.status !== 'Active' || busyAction !== null}>
                    Archive
                  </button>
                </div>
              </div>
            ))}
          </div>
        </article>

        <article className="panel">
          <div className="panel-heading">
            <div>
              <p className="eyebrow">Operations network</p>
              <h2>Warehouses</h2>
            </div>
            <span className="pill">{warehouses.length} sites</span>
          </div>

          <form className="entity-form" onSubmit={selectedWarehouse ? handleUpdateWarehouse : handleCreateWarehouse}>
            <div className="form-grid">
              <label>
                <span>Code</span>
                <input
                  value={warehouseForm.code}
                  onChange={(event) => setWarehouseForm((current) => ({ ...current, code: event.target.value }))}
                  disabled={Boolean(selectedWarehouse)}
                  placeholder="BNE"
                />
              </label>
              <label>
                <span>Name</span>
                <input
                  value={warehouseForm.name}
                  onChange={(event) => setWarehouseForm((current) => ({ ...current, name: event.target.value }))}
                  placeholder="Brisbane Warehouse"
                />
              </label>
              <label>
                <span>City</span>
                <input
                  value={warehouseForm.city}
                  onChange={(event) => setWarehouseForm((current) => ({ ...current, city: event.target.value }))}
                  placeholder="Brisbane"
                />
              </label>
            </div>

            <div className="form-actions">
              <button className="primary-button" type="submit" disabled={busyAction !== null}>
                {selectedWarehouse ? 'Save warehouse' : 'Create warehouse'}
              </button>
              {selectedWarehouse ? (
                <button
                  className="secondary-button"
                  type="button"
                  onClick={() => {
                    setSelectedWarehouseId(null)
                    setWarehouseForm(emptyWarehouseForm)
                    setWarehouseError(null)
                  }}>
                  Cancel edit
                </button>
              ) : null}
            </div>
          </form>

          {warehouseError ? <p className="banner banner--error">{warehouseError}</p> : null}

          <div className="entity-list">
            {isLoading ? <p className="empty-state">Loading warehouses...</p> : null}
            {!isLoading && warehouses.length === 0 ? <p className="empty-state">No warehouses found.</p> : null}
            {warehouses.map((warehouse) => (
              <div
                key={warehouse.id}
                className={`entity-card ${selectedWarehouseId === warehouse.id ? 'entity-card--selected' : ''}`}>
                <div>
                  <div className="entity-row">
                    <strong>{warehouse.name}</strong>
                    <span className={`pill ${warehouse.status === 'Active' ? 'pill--active' : 'pill--archived'}`}>
                      {warehouse.status}
                    </span>
                  </div>
                  <p>
                    {warehouse.code} · {warehouse.city}
                  </p>
                  <small>
                    {warehouse.totalSkuCount} SKU positions · {warehouse.lowStockSkuCount} low stock
                  </small>
                </div>
                <div className="card-actions">
                  <button className="secondary-button" onClick={() => setSelectedWarehouseId(warehouse.id)}>
                    Edit
                  </button>
                  <button
                    className="ghost-button"
                    onClick={() => void handleDeactivateWarehouse(warehouse)}
                    disabled={warehouse.status !== 'Active' || busyAction !== null}>
                    Deactivate
                  </button>
                </div>
              </div>
            ))}
          </div>
        </article>
      </section>

      <section className="panel inventory-panel">
        <div className="panel-heading">
          <div>
            <p className="eyebrow">Inventory view</p>
            <h2>Current stock summary</h2>
          </div>
          <span className="pill">{inventory.length} rows</span>
        </div>

        <div className="inventory-table-wrapper">
          <table className="inventory-table">
            <thead>
              <tr>
                <th>Warehouse</th>
                <th>Product</th>
                <th>On hand</th>
                <th>Reserved</th>
                <th>Available</th>
                <th>Reorder</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {inventory.map((item) => (
                <tr key={item.id}>
                  <td>
                    <strong>{item.warehouseCode}</strong>
                    <span>{item.warehouseName}</span>
                  </td>
                  <td>
                    <strong>{item.productSku}</strong>
                    <span>{item.productName}</span>
                  </td>
                  <td>{item.quantityOnHand}</td>
                  <td>{item.quantityReserved}</td>
                  <td>{item.availableQuantity}</td>
                  <td>{item.reorderThreshold}</td>
                  <td>
                    <span className={`pill ${item.isLowStock ? 'pill--warning' : 'pill--active'}`}>
                      {item.isLowStock ? 'Low stock' : 'Healthy'}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </main>
  )
}

async function fetchJson<T>(input: RequestInfo, init?: RequestInit): Promise<T> {
  const response = await fetch(input, init)

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  return (await response.json()) as T
}

async function readErrorMessage(response: Response): Promise<string> {
  try {
    const payload = (await response.json()) as {
      message?: string
      title?: string
      errors?: Record<string, string[]>
    }

    if (payload.message) {
      return payload.message
    }

    if (payload.errors) {
      return Object.entries(payload.errors)
        .flatMap(([field, messages]) => messages.map((message) => `${field}: ${message}`))
        .join(' | ')
    }

    if (payload.title) {
      return payload.title
    }
  } catch {
    // Ignore JSON parse errors and fall back to status text.
  }

  return `Request failed with status ${response.status}`
}

export default App


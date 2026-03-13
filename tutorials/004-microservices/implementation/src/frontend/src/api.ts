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

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    ...init,
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
      ...(init?.headers ?? {}),
    },
  })

  if (!response.ok) {
    const errorText = await response.text()
    throw new Error(errorText || `Request to ${path} failed with ${response.status}`)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return response.json() as Promise<T>
}

export const api = {
  getSeedUsers: () => request<SeedUser[]>('/api/auth/users'),
  login: (email: string, password: string) =>
    request<CurrentUser>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    }),
  logout: () => request<void>('/api/auth/logout', { method: 'POST' }),
  getCurrentUser: () => request<CurrentUser>('/api/auth/me'),
  getProducts: () => request<Product[]>('/api/catalog/products'),
  getAvailability: (sku: string) => request<Availability>(`/api/catalog/availability?sku=${encodeURIComponent(sku)}`),
  getOrders: () => request<OrderSummary[]>('/api/orders'),
  getOrder: (orderId: string) => request<OrderDetail>(`/api/orders/${orderId}`),
  createOrder: (payload: { customerReference: string; currency: string; lines: { sku: string; quantity: number }[] }) =>
    request<OrderDetail>('/api/orders', {
      method: 'POST',
      body: JSON.stringify(payload),
    }),
  submitOrder: (orderId: string) =>
    request<OrderDetail>(`/api/orders/${orderId}/submit`, { method: 'POST' }),
  getDashboard: () => request<DashboardSummary>('/api/operations/dashboard'),
  getProjectedOrders: () => request<OrderSummary[]>('/api/operations/orders'),
  getShipments: () => request<Shipment[]>('/api/fulfillment/shipments'),
  progressShipment: (shipmentId: string, command: string) =>
    request<Shipment>(`/api/fulfillment/shipments/${shipmentId}/${command}`, { method: 'POST' }),
}

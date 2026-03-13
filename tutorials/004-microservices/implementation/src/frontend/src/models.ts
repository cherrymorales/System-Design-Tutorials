export interface CurrentUser {
  userId: string
  email: string
  displayName: string
  role: string
}

export interface SeedUser {
  email: string
  displayName: string
  role: string
}

export interface Product {
  sku: string
  name: string
  category: string
  unitPrice: number
  isSellable: boolean
  operationalStatus: string
}

export interface AvailabilityByWarehouse {
  warehouseCode: string
  availableQuantity: number
  reservedQuantity: number
}

export interface Availability {
  sku: string
  availableQuantity: number
  reservedQuantity: number
  warehouses: AvailabilityByWarehouse[]
}

export interface OrderSummary {
  orderId: string
  orderNumber: string
  customerReference: string
  status: string
  reservationStatus: string
  paymentStatus: string
  shipmentStatus: string | null
  totalAmount: number
  updatedAt: string
}

export interface OrderLine {
  sku: string
  productName: string
  quantity: number
  unitPrice: number
  lineTotal: number
}

export interface OrderDetail extends OrderSummary {
  currency: string
  createdByEmail: string
  failureReason: string | null
  shipmentId: string | null
  createdAt: string
  lines: OrderLine[]
}

export interface Shipment {
  shipmentId: string
  orderId: string
  orderNumber: string
  status: string
  warehouseCode: string
  trackingReference: string
  updatedAt: string
}

export interface DashboardSummary {
  totalOrders: number
  awaitingDependencies: number
  readyForFulfillment: number
  fulfillmentInProgress: number
  failed: number
  completed: number
  recentOrders: OrderSummary[]
}

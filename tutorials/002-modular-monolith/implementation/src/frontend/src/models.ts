export type HealthResponse = {
  status: string
  service: string
  timestamp: string
}

export type UserSession = {
  displayName: string
  email: string
  roles: string[]
}

export type LoginFormState = {
  email: string
  password: string
}

export type Customer = {
  id: string
  accountCode: string
  name: string
  status: string
  billingContactName: string
  billingContactEmail: string
  shippingContactName: string
  shippingContactEmail: string
  createdAt: string
  updatedAt: string | null
}

export type Product = {
  id: string
  sku: string
  name: string
  category: string
  unitPrice: number
  status: string
  createdAt: string
  updatedAt: string | null
}

export type Warehouse = {
  id: string
  code: string
  name: string
  city: string
  status: string
}

export type StockItem = {
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
  updatedAt: string
}

export type OrderLine = {
  productId: string
  productSku: string
  productName: string
  quantity: number
  unitPrice: number
  lineTotal: number
}

export type OrderSummary = {
  id: string
  customerId: string
  customerName: string
  customerAccountCode: string
  status: string
  reservationId: string | null
  invoiceId: string | null
  totalAmount: number
  createdBy: string
  createdAt: string
  submittedAt: string | null
  readyForInvoicingAt: string | null
  completedAt: string | null
  cancelledAt: string | null
  lines: OrderLine[]
}

export type Invoice = {
  id: string
  orderId: string
  customerId: string
  customerName: string
  invoiceNumber: string
  status: string
  totalAmount: number
  createdBy: string
  createdAt: string
  issuedAt: string | null
  issuedBy: string | null
  paidAt: string | null
  paidBy: string | null
}

export type LowStockItem = {
  stockItemId: string
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
}

export type ModuleHealth = {
  moduleName: string
  summary: string
  status: string
}

export type ReportSummary = {
  totalCustomers: number
  activeProducts: number
  draftOrders: number
  reservedOrders: number
  readyForInvoicingOrders: number
  issuedInvoices: number
  paidInvoices: number
  totalReservedValue: number
  totalPaidValue: number
  lowStockItems: LowStockItem[]
  moduleHealth: ModuleHealth[]
}

export type CustomerFormState = {
  accountCode: string
  name: string
  billingContactName: string
  billingContactEmail: string
  shippingContactName: string
  shippingContactEmail: string
  isActive: boolean
}

export type ProductFormState = {
  sku: string
  name: string
  category: string
  unitPrice: string
  isActive: boolean
}

export type OrderLineFormState = {
  productId: string
  quantity: string
}

export type OrderFormState = {
  customerId: string
  lines: OrderLineFormState[]
}

export const seededUsers = [
  { email: 'sales@modularmonolith.local', label: 'Sales Coordinator', role: 'SalesCoordinator' },
  { email: 'warehouse@modularmonolith.local', label: 'Warehouse Operator', role: 'WarehouseOperator' },
  { email: 'finance@modularmonolith.local', label: 'Finance Officer', role: 'FinanceOfficer' },
  { email: 'manager@modularmonolith.local', label: 'Operations Manager', role: 'OperationsManager' },
] as const

export const defaultLoginForm: LoginFormState = {
  email: seededUsers[0].email,
  password: 'Password123!',
}

export const emptyCustomerForm = (): CustomerFormState => ({
  accountCode: '',
  name: '',
  billingContactName: '',
  billingContactEmail: '',
  shippingContactName: '',
  shippingContactEmail: '',
  isActive: true,
})

export const emptyProductForm = (): ProductFormState => ({
  sku: '',
  name: '',
  category: '',
  unitPrice: '',
  isActive: true,
})

export const emptyOrderLineForm = (): OrderLineFormState => ({
  productId: '',
  quantity: '1',
})

export const emptyOrderForm = (): OrderFormState => ({
  customerId: '',
  lines: [emptyOrderLineForm()],
})

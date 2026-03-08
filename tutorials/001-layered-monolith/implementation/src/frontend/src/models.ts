export type HealthResponse = {
  status: string
  service: string
  timestamp: string
}

export type Product = {
  id: string
  sku: string
  name: string
  category: string
  supplierCode: string
  unitCost: number
  status: string
}

export type Warehouse = {
  id: string
  code: string
  name: string
  city: string
  status: string
  totalSkuCount: number
  lowStockSkuCount: number
}

export type InventorySummaryItem = {
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

export type InventoryReceipt = {
  id: string
  warehouseId: string
  warehouseCode: string
  warehouseName: string
  productId: string
  productSku: string
  productName: string
  quantityReceived: number
  supplierReference: string
  receivedBy: string
  receivedAt: string
}

export type StockTransfer = {
  id: string
  productId: string
  productSku: string
  productName: string
  sourceWarehouseId: string
  sourceWarehouseCode: string
  sourceWarehouseName: string
  destinationWarehouseId: string
  destinationWarehouseCode: string
  destinationWarehouseName: string
  quantity: number
  status: string
  requestedBy: string
  approvedBy: string | null
  dispatchedBy: string | null
  receivedBy: string | null
  cancelledBy: string | null
  createdAt: string
  approvedAt: string | null
  dispatchedAt: string | null
  receivedAt: string | null
  cancelledAt: string | null
  reason: string
  cancellationReason: string | null
}

export type InventoryAdjustment = {
  id: string
  productId: string
  productSku: string
  productName: string
  warehouseId: string
  warehouseCode: string
  warehouseName: string
  quantityDelta: number
  estimatedValueImpact: number
  reasonCode: string
  status: string
  requiresApproval: boolean
  submittedBy: string
  submittedAt: string | null
  approvedBy: string | null
  approvedAt: string | null
  rejectedBy: string | null
  rejectedAt: string | null
  notes: string | null
  createdAt: string
}

export type LowStockItem = {
  inventoryItemId: string
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
  shortfallQuantity: number
  updatedAt: string
}

export type ProductFormState = {
  sku: string
  name: string
  category: string
  supplierCode: string
  unitCost: string
}

export type WarehouseFormState = {
  code: string
  name: string
  city: string
}

export type ReceiptFormState = {
  warehouseId: string
  productId: string
  quantityReceived: string
  supplierReference: string
  receivedBy: string
}

export type TransferFormState = {
  sourceWarehouseId: string
  destinationWarehouseId: string
  productId: string
  quantity: string
  requestedBy: string
  reason: string
}

export type AdjustmentFormState = {
  warehouseId: string
  productId: string
  quantityDelta: string
  reasonCode: string
  submittedBy: string
  notes: string
}

export const demoUsers = {
  manager: 'manager@layeredmonolith.local',
  planner: 'planner@layeredmonolith.local',
  purchasing: 'purchasing@layeredmonolith.local',
  operator: 'operator.brisbane@layeredmonolith.local',
}

export const emptyProductForm: ProductFormState = {
  sku: '',
  name: '',
  category: '',
  supplierCode: '',
  unitCost: '',
}

export const emptyWarehouseForm: WarehouseFormState = {
  code: '',
  name: '',
  city: '',
}

export const createEmptyReceiptForm = (): ReceiptFormState => ({
  warehouseId: '',
  productId: '',
  quantityReceived: '',
  supplierReference: '',
  receivedBy: demoUsers.operator,
})

export const createEmptyTransferForm = (): TransferFormState => ({
  sourceWarehouseId: '',
  destinationWarehouseId: '',
  productId: '',
  quantity: '',
  requestedBy: demoUsers.planner,
  reason: '',
})

export const createEmptyAdjustmentForm = (): AdjustmentFormState => ({
  warehouseId: '',
  productId: '',
  quantityDelta: '',
  reasonCode: '',
  submittedBy: demoUsers.operator,
  notes: '',
})

import type { Invoice, OrderSummary, UserSession } from './models'

const ROLE_SALES = 'SalesCoordinator'
const ROLE_WAREHOUSE = 'WarehouseOperator'
const ROLE_FINANCE = 'FinanceOfficer'
const ROLE_MANAGER = 'OperationsManager'

export function getOrderActions(session: UserSession, order: OrderSummary, invoice: Invoice | undefined): string[] {
  const roles = new Set(session.roles)
  const canSales = roles.has(ROLE_SALES) || roles.has(ROLE_MANAGER)
  const canWarehouse = roles.has(ROLE_WAREHOUSE) || roles.has(ROLE_MANAGER)
  const canFinance = roles.has(ROLE_FINANCE) || roles.has(ROLE_MANAGER)

  const actions: string[] = []

  if (order.status === 'Draft' && canSales) actions.push('submit')
  if (order.status === 'Submitted' && canSales) actions.push('reserve')
  if (order.status === 'Reserved' && canWarehouse) actions.push('ready')
  if (order.status === 'ReadyForInvoicing' && canFinance && !order.invoiceId) actions.push('create-invoice')
  if (order.status === 'ReadyForInvoicing' && canSales) actions.push('cancel')
  if (order.status === 'Reserved' && canSales) actions.push('cancel')
  if (order.status === 'Submitted' && canSales) actions.push('cancel')
  if (order.status === 'Invoiced' && canFinance && invoice?.status === 'Paid') actions.push('complete')

  return actions
}

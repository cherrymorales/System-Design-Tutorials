import { describe, expect, it } from 'vitest'
import type { Invoice, OrderSummary, UserSession } from './models'
import { getOrderActions } from './workflows'

const salesSession: UserSession = { displayName: 'Sales', email: 'sales@modularmonolith.local', roles: ['SalesCoordinator'] }
const financeSession: UserSession = { displayName: 'Finance', email: 'finance@modularmonolith.local', roles: ['FinanceOfficer'] }

const baseOrder: OrderSummary = {
  id: 'order-1',
  customerId: 'customer-1',
  customerName: 'Acme Office Group',
  customerAccountCode: 'CUST-1001',
  status: 'ReadyForInvoicing',
  reservationId: 'reservation-1',
  invoiceId: null,
  totalAmount: 1200,
  createdBy: 'sales@modularmonolith.local',
  createdAt: '2026-03-09T00:00:00Z',
  submittedAt: '2026-03-09T00:05:00Z',
  readyForInvoicingAt: '2026-03-09T00:10:00Z',
  completedAt: null,
  cancelledAt: null,
  lines: [],
}

describe('getOrderActions', () => {
  it('allows finance to create an invoice for ready orders without one', () => {
    expect(getOrderActions(financeSession, baseOrder, undefined)).toContain('create-invoice')
  })

  it('allows sales to cancel reserved orders', () => {
    expect(getOrderActions(salesSession, { ...baseOrder, status: 'Reserved' }, undefined)).toContain('cancel')
  })

  it('allows finance to complete invoiced orders only after payment', () => {
    const invoice: Invoice = {
      id: 'invoice-1',
      orderId: 'order-1',
      customerId: 'customer-1',
      customerName: 'Acme Office Group',
      invoiceNumber: 'INV-1',
      status: 'Paid',
      totalAmount: 1200,
      createdBy: 'finance@modularmonolith.local',
      createdAt: '2026-03-09T00:11:00Z',
      issuedAt: '2026-03-09T00:12:00Z',
      issuedBy: 'finance@modularmonolith.local',
      paidAt: '2026-03-09T00:13:00Z',
      paidBy: 'finance@modularmonolith.local',
    }

    expect(getOrderActions(financeSession, { ...baseOrder, status: 'Invoiced', invoiceId: invoice.id }, invoice)).toContain('complete')
  })
})

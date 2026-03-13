import { render, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, test, vi } from 'vitest'
import App from './App'

describe('App', () => {
  beforeEach(() => {
    vi.stubGlobal('fetch', vi.fn(async (input: RequestInfo | URL) => {
      const url = input.toString()

      if (url.endsWith('/api/auth/users')) {
        return new Response(JSON.stringify([
          { email: 'orders@microservices.local', displayName: 'Order Operations Agent', role: 'OrderOpsAgent' },
        ]), { status: 200 })
      }

      if (url.endsWith('/api/auth/me')) {
        return new Response('', { status: 401 })
      }

      return new Response('', { status: 404 })
    }))
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  test('shows login shell with seeded users when no session exists', async () => {
    render(<App />)

    await screen.findByRole('heading', { name: /omnichannel commerce operations network/i })
    expect(screen.getByText('Order Operations Agent')).toBeInTheDocument()
  })

  test('shows dashboard for authenticated user', async () => {
    vi.stubGlobal('fetch', vi.fn(async (input: RequestInfo | URL) => {
      const url = input.toString()

      if (url.endsWith('/api/auth/users')) {
        return new Response(JSON.stringify([
          { email: 'orders@microservices.local', displayName: 'Order Operations Agent', role: 'OrderOpsAgent' },
        ]), { status: 200 })
      }

      if (url.endsWith('/api/auth/me')) {
        return new Response(JSON.stringify({
          userId: '00000000-0000-0000-0000-000000000001',
          email: 'orders@microservices.local',
          displayName: 'Order Operations Agent',
          role: 'OrderOpsAgent',
        }), { status: 200 })
      }

      if (url.endsWith('/api/catalog/products')) {
        return new Response(JSON.stringify([
          { sku: 'SKU-HEADSET-001', name: 'Noise Cancelling Headset', category: 'Peripherals', unitPrice: 249, isSellable: true, operationalStatus: 'Active' },
        ]), { status: 200 })
      }

      if (url.endsWith('/api/operations/dashboard')) {
        return new Response(JSON.stringify({
          totalOrders: 1,
          awaitingDependencies: 1,
          readyForFulfillment: 0,
          fulfillmentInProgress: 0,
          failed: 0,
          completed: 0,
          recentOrders: [],
        }), { status: 200 })
      }

      if (url.endsWith('/api/orders')) {
        return new Response(JSON.stringify([
          {
            orderId: '00000000-0000-0000-0000-000000000111',
            orderNumber: 'ORD-10001',
            customerReference: 'CSR-10001',
            status: 'Draft',
            reservationStatus: 'Pending',
            paymentStatus: 'Pending',
            shipmentStatus: null,
            totalAmount: 249,
            updatedAt: '2026-03-12T00:00:00Z',
          },
        ]), { status: 200 })
      }

      if (url.endsWith('/api/fulfillment/shipments')) {
        return new Response(JSON.stringify([]), { status: 200 })
      }

      if (url.includes('/api/orders/00000000-0000-0000-0000-000000000111')) {
        return new Response(JSON.stringify({
          orderId: '00000000-0000-0000-0000-000000000111',
          orderNumber: 'ORD-10001',
          customerReference: 'CSR-10001',
          currency: 'AUD',
          status: 'Draft',
          reservationStatus: 'Pending',
          paymentStatus: 'Pending',
          shipmentStatus: null,
          totalAmount: 249,
          createdByEmail: 'orders@microservices.local',
          failureReason: null,
          shipmentId: null,
          createdAt: '2026-03-12T00:00:00Z',
          updatedAt: '2026-03-12T00:00:00Z',
          lines: [
            {
              sku: 'SKU-HEADSET-001',
              productName: 'Noise Cancelling Headset',
              quantity: 1,
              unitPrice: 249,
              lineTotal: 249,
            },
          ],
        }), { status: 200 })
      }

      if (url.includes('/api/catalog/availability')) {
        return new Response(JSON.stringify({
          sku: 'SKU-HEADSET-001',
          availableQuantity: 10,
          reservedQuantity: 0,
          warehouses: [{ warehouseCode: 'MEL-DC', availableQuantity: 10, reservedQuantity: 0 }],
        }), { status: 200 })
      }

      return new Response('', { status: 404 })
    }))

    render(<App />)

    await waitFor(() => expect(screen.getByRole('heading', { name: /operations control plane/i })).toBeInTheDocument())
    expect(screen.getByText('Total orders')).toBeInTheDocument()
    expect(screen.getByText('ORD-10001')).toBeInTheDocument()
  })
})

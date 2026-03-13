import { expect, test } from '@playwright/test'

test('order submission and fulfillment complete through the distributed flow', async ({ page }) => {
  const uniqueSuffix = Date.now().toString().slice(-6)

  await page.goto('/')
  await page.getByRole('button', { name: /order operations agent/i }).click()
  await page.getByRole('button', { name: /sign in/i }).click()
  await expect(page.getByRole('heading', { name: /operations control plane/i })).toBeVisible()
  await expect(page.locator('select option[value="SKU-HEADSET-001"]').first()).toBeAttached({ timeout: 30_000 })

  await page.getByLabel('Customer reference').fill(`CSR-${uniqueSuffix}`)
  await page.getByRole('combobox').first().selectOption({ value: 'SKU-HEADSET-001' })
  await page.getByRole('button', { name: /create draft order/i }).click()
  await expect(page.getByRole('button', { name: /submit order/i })).toBeVisible({ timeout: 15_000 })
  await page.getByRole('button', { name: /submit order/i }).click()

  const orderStatus = page.locator('.inspector-panel .status-pill').first()
  await expect
    .poll(
      async () => {
        await page.getByRole('button', { name: /refresh/i }).click()
        await page.waitForTimeout(2_000)
        return ((await orderStatus.textContent().catch(() => '')) ?? '').trim()
      },
      {
        timeout: 60_000,
        intervals: [2_000, 3_000, 3_000],
      },
    )
    .toMatch(/ready for fulfillment|fulfillment in progress/i)

  await page.getByRole('button', { name: /sign out/i }).click()
  await page.getByRole('button', { name: /fulfillment operator/i }).click()
  await page.getByRole('button', { name: /sign in/i }).click()

  await expect(page.getByText(/fulfillment lane/i)).toBeVisible()
  for (let attempt = 0; attempt < 12; attempt += 1) {
    const shipmentCard = page.locator('.shipment-card').first()
    if (await shipmentCard.isVisible().catch(() => false)) {
      break
    }

    await page.getByRole('button', { name: /refresh/i }).click()
    await page.waitForTimeout(2_000)
  }

  await expect(page.locator('.shipment-card').first()).toBeVisible({ timeout: 5_000 })
  await page.getByRole('button', { name: /^pick$/i }).click()
  await page.getByRole('button', { name: /^pack$/i }).click()
  await page.getByRole('button', { name: /^ship$/i }).click()
  await page.getByRole('button', { name: /^deliver$/i }).click()

  await page.getByRole('button', { name: /sign out/i }).click()
  await page.getByRole('button', { name: /operations manager/i }).click()
  await page.getByRole('button', { name: /sign in/i }).click()
  await page.getByRole('button', { name: /refresh/i }).click()

  await expect(page.locator('.status-pill.success', { hasText: 'Completed' }).first()).toBeVisible()
  await expect(page.getByText(/operations control plane/i)).toBeVisible()
})

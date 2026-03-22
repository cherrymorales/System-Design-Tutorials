import { expect, test } from '@playwright/test'

test('asset upload completes through the event-driven workflow', async ({ page }) => {
  const suffix = Date.now().toString().slice(-6)

  await page.goto('/')
  await page.getByRole('button', { name: /content operations coordinator/i }).click()
  await page.getByRole('button', { name: /^sign in$/i }).click()

  await expect(page.getByRole('heading', { name: /asset ingestion operations/i })).toBeVisible()

  await page.getByLabel('Asset key').fill(`ASSET-${suffix}`)
  await page.getByLabel('Title').fill(`Smoke asset ${suffix}`)
  await page.getByRole('button', { name: /register asset/i }).click()

  await expect(page.getByRole('button', { name: /mark upload complete/i })).toBeVisible()
  await page.getByRole('button', { name: /mark upload complete/i }).click()

  const lifecyclePill = page.locator('.inspector-panel .status-pill').first()
  await expect
    .poll(
      async () => {
        await page.getByRole('button', { name: /refresh/i }).click()
        await page.waitForTimeout(1500)
        return ((await lifecyclePill.textContent()) ?? '').trim()
      },
      { timeout: 60_000, intervals: [1500, 2000, 2500] },
    )
    .toMatch(/ready/i)

  await expect(page.getByText(/ready-state messages/i)).toBeVisible()
  await expect(page.getByText(/asset processing completed successfully/i)).toBeVisible({ timeout: 10_000 })
})

import { expect, test } from '@playwright/test'

test('manager can create a project, add a member, create a task, and comment on it', async ({ page }) => {
  const uniqueSuffix = Date.now().toString().slice(-6)
  const projectName = `Smoke Workspace ${uniqueSuffix}`
  const projectCode = `SMOKE-${uniqueSuffix}`
  const taskTitle = `Smoke task ${uniqueSuffix}`
  const commentText = `Smoke comment ${uniqueSuffix}`

  await page.goto('/login')
  await page.getByRole('button', { name: /project manager/i }).click()
  await page.getByRole('button', { name: /sign in/i }).click()

  await expect(page.getByRole('heading', { name: /workspace summary/i })).toBeVisible()

  await page.getByRole('link', { name: 'Projects' }).click()
  await expect(page.getByRole('heading', { name: /project portfolio/i })).toBeVisible()

  await page.getByLabel('Name').fill(projectName)
  await page.getByLabel('Code').fill(projectCode)
  await page.getByLabel('Description').fill('Smoke test project used to validate the main tutorial path.')
  await page.getByLabel('Start date').fill('2026-03-10')
  await page.getByLabel('Target date').fill('2026-06-10')
  await page.getByRole('button', { name: /create project/i }).click()

  await expect(page.getByRole('heading', { name: projectName })).toBeVisible()
  await page.getByRole('button', { name: 'Activate' }).click()
  await expect(page.getByText('Active', { exact: true }).first()).toBeVisible()

  const alexOption = page.getByLabel('Workspace user').locator('option', { hasText: 'Alex Contributor' })
  const alexUserId = await alexOption.getAttribute('value')
  expect(alexUserId).not.toBeNull()

  await page.getByLabel('Workspace user').selectOption(alexUserId!)
  await page.getByLabel('Role in project').selectOption('Contributor')
  await page.getByRole('button', { name: /add or update member/i }).click()
  const membershipPanel = page.locator('.panel').filter({ has: page.getByRole('heading', { name: /project access/i }) })
  await expect(membershipPanel.getByText('alex@clientserverspa.local', { exact: true })).toBeVisible()

  await page.getByLabel('Title').fill(taskTitle)
  const alexAssigneeOption = page.getByLabel('Assignee').locator('option', { hasText: 'Alex Contributor' })
  const alexAssigneeId = await alexAssigneeOption.getAttribute('value')
  expect(alexAssigneeId).not.toBeNull()
  await page.getByLabel('Assignee').selectOption(alexAssigneeId!)
  await page.getByLabel('Description').last().fill('Smoke test task for the route-based SPA.')
  await page.getByLabel('Priority').selectOption('High')
  await page.getByLabel('Due date').last().fill('2026-03-20')
  await page.getByRole('button', { name: /create task/i }).click()

  await expect(page.getByRole('heading', { name: taskTitle })).toBeVisible()
  await page.getByRole('button', { name: 'Start' }).click()
  await page.getByLabel('Add comment').fill(commentText)
  await page.getByRole('button', { name: /post comment/i }).click()

  await expect(page.getByText(commentText)).toBeVisible()
})

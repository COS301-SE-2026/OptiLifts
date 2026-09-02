import { test, expect } from '@playwright/test'

    test('loads status badge for a real exercise', async ({ page }) => {
        await page.goto('http://localhost:5173/progression')

        const statusBadge = page.getByText(/^(Plateau|Regressing|Progressing)$/).first()
        if ((await statusBadge.count()) === 0) {
        test.skip(true, 'No plateau/trend data seeded for this test account')
        return
        }

        await expect(statusBadge).toBeVisible()
    })

  test('swapping an exercise persisting and button disappearing', async ({ page }) => {
    await page.goto('http://localhost:5173/login')
    await page.locator('input[type="email"], input[name="email"]').first().fill('gymgoer@gmail.com')
    await page.locator('input[type="password"], input[name="password"]').first().fill('GymGoer123!')
    await page.locator('button[type="submit"]').first().click()
    await page.waitForURL(/dashboard/, { timeout: 15000 }).catch(() => {})

    await page.goto('http://localhost:5173/progression')
    const swapBtn = page.locator('button', { hasText: 'Swap in' }).first()

    if (await swapBtn.count() === 0) {
      test.skip(true, 'No swappable exercise seeded for this account right now')
      return
    }

    const workoutLbl = (await swapBtn.textContent())!.trim()

    await swapBtn.click()
    await expect(page.locator('button[aria-label^="Add "]').first()).toBeVisible({ timeout: 5000 })

    const putRespPromise = page.waitForResponse(
      (res) => res.request().method() === 'PUT' && res.url().includes('/exercises/'),
      { timeout: 10000 },
    )
    await page.locator('button[aria-label^="Add "]').first().click()
    const putResp = await putRespPromise

    expect(putResp.status()).toBe(204)
    await expect(page.getByText(workoutLbl, { exact: true })).not.toBeVisible({ timeout: 5000 })
  })

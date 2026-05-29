import { test, expect } from '@playwright/test';

test('launch ARCYN UI', async ({ page }) => {
  const execPath = process.env.APP_PATH; // set by CI step
  await page.goto('file://' + execPath);
  await expect(page.locator('text=ARCYN')).toBeVisible();
});

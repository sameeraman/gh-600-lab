import { test, expect } from '@playwright/test';

test.describe('Todo App E2E', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should display the app title', async ({ page }) => {
    await expect(page.getByText('📋 Todo App')).toBeVisible();
  });

  test('should add a new todo', async ({ page }) => {
    await page.getByLabel('Todo title').fill('E2E Test Todo');
    await page.getByLabel('Todo description').fill('Created by Playwright');
    await page.getByText('Add Todo').click();
    await expect(page.getByText('E2E Test Todo')).toBeVisible();
  });

  test('should toggle a todo complete', async ({ page }) => {
    await page.getByLabel('Todo title').fill('Toggle Test');
    await page.getByText('Add Todo').click();
    const toggle = page.getByLabel('Toggle Toggle Test');
    await expect(toggle).not.toBeChecked();
    await toggle.click();
    await expect(toggle).toBeChecked();
  });

  test('should delete a todo', async ({ page }) => {
    await page.getByLabel('Todo title').fill('Delete Me');
    await page.getByText('Add Todo').click();
    await expect(page.getByText('Delete Me')).toBeVisible();
    await page.getByLabel('Delete Delete Me').click();
    await expect(page.getByText('Delete Me')).not.toBeVisible();
  });

  test('should show error for empty title', async ({ page }) => {
    await page.getByText('Add Todo').click();
    await expect(page.getByRole('alert')).toContainText('Title is required');
  });
});

const { defineConfig } = require('@playwright/test');

const baseURL = process.env.PLAYWRIGHT_BASE_URL || 'http://127.0.0.1:3000';

// Set when something else already serves the app — the CI harness that points the built
// SPA at a deployed API, for example. Playwright then attaches instead of starting Vite.
const externalServer = process.env.PLAYWRIGHT_EXTERNAL_SERVER === 'true';

module.exports = defineConfig({
  testDir: './e2e',
  testIgnore: '**/test-harness.mjs',
  // The JSON report is what the CI job turns into a pull request comment.
  reporter: [
    ['html', { open: 'never' }],
    ['json', { outputFile: 'results.json' }],
    ['list']
  ],
  use: {
    baseURL,
    screenshot: 'on',
    video: 'retain-on-failure',
    trace: 'retain-on-failure'
  },
  webServer: externalServer
    ? undefined
    : {
        command: 'npm run dev -- --host 127.0.0.1 --port 3000',
        url: baseURL,
        reuseExistingServer: !process.env.CI
      }
});

import { defineConfig } from '@playwright/test'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const __filename = fileURLToPath(import.meta.url)
const __dirname = path.dirname(__filename)
const implementationRoot = path.resolve(__dirname, '..', '..')

export default defineConfig({
  testDir: './playwright',
  timeout: 90_000,
  use: {
    baseURL: 'http://127.0.0.1:5178',
    trace: 'retain-on-failure',
  },
  webServer: [
    {
      command: 'powershell -NoProfile -ExecutionPolicy Bypass -File tests/smoke/start-stack.ps1',
      cwd: implementationRoot,
      url: 'http://127.0.0.1:8084/api/health',
      reuseExistingServer: true,
      timeout: 180_000,
    },
    {
      command: 'npm run dev -- --host 127.0.0.1 --port 5178 --strictPort',
      cwd: path.resolve(implementationRoot, 'src/frontend'),
      env: {
        ...process.env,
        VITE_API_PROXY_TARGET: 'http://127.0.0.1:8084',
      },
      url: 'http://127.0.0.1:5178',
      reuseExistingServer: false,
      timeout: 60_000,
    },
  ],
  globalTeardown: './teardown.ts',
})

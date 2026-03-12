import { defineConfig } from '@playwright/test'
import os from 'node:os'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const __filename = fileURLToPath(import.meta.url)
const __dirname = path.dirname(__filename)
const implementationRoot = path.resolve(__dirname, '..', '..')
const sqlitePath = path.join(os.tmpdir(), 'client-server-spa-api-smoke.db')

export default defineConfig({
  testDir: './playwright',
  timeout: 60_000,
  fullyParallel: false,
  use: {
    baseURL: 'http://127.0.0.1:5178',
    trace: 'retain-on-failure',
  },
  webServer: [
    {
      command: 'dotnet run --project src/backend/SystemDesignTutorials.ClientServerSpaApi.Web/SystemDesignTutorials.ClientServerSpaApi.Web.csproj --no-launch-profile',
      cwd: implementationRoot,
      env: {
        ...process.env,
        ASPNETCORE_URLS: 'http://127.0.0.1:8084',
        ConnectionStrings__DefaultConnection: `Data Source=${sqlitePath}`,
      },
      url: 'http://127.0.0.1:8084/api/health',
      reuseExistingServer: false,
      timeout: 60_000,
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
})

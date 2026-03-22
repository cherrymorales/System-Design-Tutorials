import { execSync } from 'node:child_process'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const __filename = fileURLToPath(import.meta.url)
const __dirname = path.dirname(__filename)
const implementationRoot = path.resolve(__dirname, '..', '..')

export default async function teardown() {
  execSync('docker compose -f docker/docker-compose.yml down -v', {
    cwd: implementationRoot,
    stdio: 'inherit',
  })
}

import { useEffect, useState } from 'react'
import './App.css'

type HealthResponse = {
  status: string
  service: string
  timestamp: string
}

function App() {
  const [health, setHealth] = useState<HealthResponse | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const loadHealth = async () => {
      try {
        const response = await fetch('/api/health')

        if (!response.ok) {
          throw new Error(`Request failed with status ${response.status}`)
        }

        const payload = (await response.json()) as HealthResponse
        setHealth(payload)
      } catch (err) {
        const message = err instanceof Error ? err.message : 'Unknown error'
        setError(message)
      }
    }

    void loadHealth()
  }, [])

  return (
    <main className="page-shell">
      <section className="hero-card">
        <p className="eyebrow">001 Layered Monolith</p>
        <h1>Inventory and warehouse management</h1>
        <p className="intro">
          Phase 1 implementation scaffold for the layered monolith tutorial. This UI will evolve into the
          operational frontend for products, warehouses, receipts, transfers, adjustments, and reporting.
        </p>
      </section>

      <section className="grid">
        <article className="panel">
          <h2>Implementation focus</h2>
          <ul>
            <li>ASP.NET Core backend with layered project boundaries</li>
            <li>React frontend with feature-based structure</li>
            <li>PostgreSQL persistence for transactional workflows</li>
            <li>Reservation-based transfer workflow</li>
          </ul>
        </article>

        <article className="panel">
          <h2>API health</h2>
          {health ? (
            <div className="status success">
              <strong>{health.status}</strong>
              <span>{health.service}</span>
              <time>{new Date(health.timestamp).toLocaleString()}</time>
            </div>
          ) : error ? (
            <div className="status error">
              <strong>Unavailable</strong>
              <span>{error}</span>
            </div>
          ) : (
            <div className="status pending">
              <strong>Checking</strong>
              <span>Waiting for backend response</span>
            </div>
          )}
        </article>
      </section>
    </main>
  )
}

export default App

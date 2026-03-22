$ErrorActionPreference = "Stop"

docker compose -f docker/docker-compose.yml up -d --build | Out-Host

for ($attempt = 0; $attempt -lt 90; $attempt++) {
  try {
    Invoke-WebRequest -UseBasicParsing "http://127.0.0.1:8085/api/health" | Out-Null
    break
  }
  catch {
    Start-Sleep -Seconds 2
  }
}

if ($attempt -ge 90) {
  Write-Error "API health check did not pass within the startup window."
}

for ($attempt = 0; $attempt -lt 60; $attempt++) {
  $logs = docker compose -f docker/docker-compose.yml logs worker --no-color 2>$null
  if ($logs -match "Bus started: rabbitmq://rabbitmq/") {
    exit 0
  }

  Start-Sleep -Seconds 2
}

Write-Error "Worker bus did not report ready within the startup window."

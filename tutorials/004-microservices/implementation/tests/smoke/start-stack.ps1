$ErrorActionPreference = "Stop"

docker compose -f docker/docker-compose.yml up -d --build | Out-Host

for ($attempt = 0; $attempt -lt 90; $attempt++) {
  try {
    Invoke-WebRequest -UseBasicParsing "http://127.0.0.1:8084/api/health" | Out-Null
    break
  }
  catch {
    Start-Sleep -Seconds 2
  }
}

if ($attempt -ge 90) {
  Write-Error "Gateway health check did not pass within the startup window."
}

$busServices = @(
  "orders",
  "inventory",
  "payments",
  "fulfillment",
  "notifications",
  "operations-query"
)

for ($attempt = 0; $attempt -lt 60; $attempt++) {
  $readyServices = 0

  foreach ($service in $busServices) {
    $logs = docker compose -f docker/docker-compose.yml logs $service --no-color 2>$null
    if ($logs -match "Bus started: rabbitmq://rabbitmq/") {
      $readyServices += 1
    }
  }

  if ($readyServices -eq $busServices.Length) {
    exit 0
  }

  Start-Sleep -Seconds 2
}

Write-Error "One or more service buses did not report ready within the startup window."

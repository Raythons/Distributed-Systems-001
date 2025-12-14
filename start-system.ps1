# Start the distributed CQRS system

Write-Host "Starting the distributed CQRS system..." -ForegroundColor Green

# Start all services
Write-Host "Starting Docker containers..." -ForegroundColor Yellow
docker-compose up -d

Write-Host "Waiting for services to initialize..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

# Register the Debezium connector
Write-Host "Registering Debezium connector..." -ForegroundColor Yellow
Invoke-WebRequest -Uri "http://localhost:8083/connectors" -Method POST -ContentType "application/json" -InFile ".\debezium\register-postgres.json"

Write-Host "System started successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Services:" -ForegroundColor Cyan
Write-Host "- PostgreSQL Leader: localhost:5432" -ForegroundColor Cyan
Write-Host "- PostgreSQL Read Replica 1: localhost:5433" -ForegroundColor Cyan
Write-Host "- PostgreSQL Read Replica 2: localhost:5434" -ForegroundColor Cyan
Write-Host "- PostgreSQL Read Replica 3: localhost:5435" -ForegroundColor Cyan
Write-Host "- Redpanda: localhost:19092" -ForegroundColor Cyan
Write-Host "- Debezium Connect: localhost:8083" -ForegroundColor Cyan
Write-Host "- CQRS Write App: localhost:8080" -ForegroundColor Cyan
Write-Host ""
Write-Host "To simulate data changes, run: docker-compose exec cqrs-write-app dotnet run" -ForegroundColor Magenta
Write-Host "To monitor read replicas, connect to them with a PostgreSQL client" -ForegroundColor Magenta
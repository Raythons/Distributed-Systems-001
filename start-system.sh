#!/bin/bash

echo "Starting the distributed CQRS system..."

# Start all services
echo "Starting Docker containers..."
docker-compose up -d

echo "Waiting for services to initialize..."
sleep 30

# Register the Debezium connector
echo "Registering Debezium connector..."
curl -X POST \
  http://localhost:8083/connectors \
  -H 'Content-Type: application/json' \
  -H 'Accept: application/json' \
  -d @debezium/register-postgres.json

echo "System started successfully!"
echo ""
echo "Services:"
echo "- PostgreSQL Leader: localhost:5432"
echo "- PostgreSQL Read Replica 1: localhost:5433"
echo "- PostgreSQL Read Replica 2: localhost:5434"
echo "- PostgreSQL Read Replica 3: localhost:5435"
echo "- Redpanda: localhost:19092"
echo "- Debezium Connect: localhost:8083"
echo "- CQRS Write App: localhost:8080"
echo ""
echo "To simulate data changes, run: docker-compose exec cqrs-write-app dotnet run"
echo "To monitor read replicas, connect to them with a PostgreSQL client"
# Distributed CQRS System with CDC

This project demonstrates a distributed CQRS (Command Query Responsibility Segregation) system with Change Data Capture (CDC) using Docker containers.

## Architecture Overview

```
┌─────────────────┐    ┌──────────────┐    ┌──────────────┐    ┌──────────────────┐
│   Write App     │───▶│  PostgreSQL  │───▶│  Debezium    │───▶│   Redpanda       │
│ (ASP.NET Core)  │    │   (Leader)   │    │  Connector   │    │ (Event Stream)   │
└─────────────────┘    └──────────────┘    └──────────────┘    └─────────┬────────┘
                                                                         │
                                                                         ▼
                                                              ┌──────────────────┐
                                                              │  Consumer App    │
                                                              │ (Replicates to   │
                                                              │  Read Replicas)  │
                                                              └─────────┬────────┘
                                                                        │
                                                                        ▼
                                                               ┌─────────────────┐
                                                               │   Read          │
                                                               │   Replicas      │
                                                               │   (3 instances) │
                                                               └─────────────────┘
```

## Components

1. **PostgreSQL Leader Database** - Stores the source of truth for data
2. **PostgreSQL Read Replicas (3)** - Serve read queries for scalability
3. **Debezium Connector** - Captures changes from the leader database
4. **Redpanda** - Event streaming platform (Kafka API compatible)
5. **CQRS Write Application** - ASP.NET Core application that writes to the leader database
6. **Consumer Application** - Reads events from Redpanda and replicates to read replicas

## Prerequisites

- Docker
- Docker Compose

## Getting Started

1. Make the scripts executable:
   ```bash
   chmod +x start-system.sh simulate-workflow.sh
   ```

2. Start the system:
   ```bash
   ./start-system.sh
   ```


## How It Works

1. The CQRS Write Application makes changes to the PostgreSQL leader database
2. Debezium captures these changes using PostgreSQL's logical replication
3. Debezium sends the change events to Redpanda
4. The Consumer Application reads events from Redpanda
5. The Consumer Application replicates the changes to all 3 read replicas
6. Read applications can query any of the read replicas for improved performance

## Services and Ports

- PostgreSQL Leader: `localhost:5432`
- PostgreSQL Read Replica 1: `localhost:5433`
- PostgreSQL Read Replica 2: `localhost:5434`
- PostgreSQL Read Replica 3: `localhost:5435`
- Redpanda: `localhost:19092`
- Debezium Connect: `localhost:8083`
- CQRS Write App: `localhost:8080`

## Monitoring

You can monitor the system by:

1. Checking the logs of individual containers:
   ```bash
   docker-compose logs -f <service-name>
   ```

2. Connecting to the databases directly with a PostgreSQL client


3. Using Redpanda's console to view topics and messages

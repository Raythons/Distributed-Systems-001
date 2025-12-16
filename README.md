# Distributed CQRS System with CDC and Database Replication

This project is a **demonstration of a distributed system** that implements **CQRS (Command Query Responsibility Segregation)** with **Change Data Capture (CDC)** and **database replication** using Docker containers.

## Overview

This demo showcases how to build a scalable distributed system where:
- **Writes** go to a single leader database (source of truth)
- **Reads** are distributed across multiple read replicas for improved performance
- **Replication** happens automatically through event streaming (CDC)
- **Load balancing** distributes read queries across replicas using round-robin

### What is Database Replication?

Database replication is the process of copying and maintaining database objects (tables, data) in multiple databases to improve:
- **Performance**: Distribute read queries across multiple replicas
- **Availability**: If one replica fails, others can still serve requests
- **Scalability**: Add more replicas to handle increased read load
- **Geographic Distribution**: Place replicas closer to users

In this system:
- The **PostgreSQL Leader** is the single source of truth for all writes
- **3 Read Replicas** serve read queries, reducing load on the leader
- Changes are captured from the leader using **Debezium CDC**
- Changes are streamed through **Redpanda** (Kafka-compatible)
- A **Consumer App** replicates changes to all read replicas
- The **CQRS Write App** uses **round-robin load balancing** to distribute reads across replicas

## Architecture

```
┌─────────────────┐    ┌──────────────┐    ┌──────────────┐    ┌──────────────────┐
│   Write App     │───▶│  PostgreSQL  │───▶│  Debezium    │───▶│   Redpanda       │
│ (ASP.NET Core)  │    │   (Leader)   │    │  Connector   │    │ (Event Stream)   │
│                 │    │              │    │              │    └─────────┬────────┘
│ Reads (Round-   │    │              │    │              │              │
│ Robin) ─────────┼───▶│              │    │              │              ▼
└─────────────────┘    └──────────────┘    └──────────────┘    ┌──────────────────┐
         │                                                       │  Consumer App    │
         │                                                       │ (Replicates to   │
         │                                                       │  Read Replicas)  │
         │                                                       └─────────┬────────┘
         │                                                                 │
         │                                                                 ▼
         │                                                        ┌─────────────────┐
         └──────────────────────────────────────────────────────▶│   Read          │
                                                                  │   Replicas      │
                                                                  │   (3 instances) │
                                                                  └─────────────────┘
```

## Components

1. **PostgreSQL Leader Database** - Stores the source of truth for all data writes
2. **PostgreSQL Read Replicas (3)** - Serve read queries for improved performance and scalability
3. **Debezium Connector** - Captures changes from the leader database using CDC
4. **Redpanda** - Event streaming platform (Kafka API compatible) for change events
5. **CQRS Write Application** - ASP.NET Core MVC app that:
   - Writes to the leader database
   - Reads from replicas using round-robin load balancing
   - Includes a Demo page to visualize replica distribution
6. **Consumer Application** - Reads events from Redpanda and replicates changes to all 3 read replicas

## Prerequisites

- **Docker** (version 20.10 or later)
- **Docker Compose** (version 2.0 or later)

## Getting Started

### Step 1: Start the System

Start all Docker containers:

**On Windows (PowerShell):**
```powershell
docker-compose up -d
```

**On Linux/Mac:**
```bash
docker-compose up -d
```

Or use the provided startup scripts:

**Windows:**
```powershell
.\start-system.ps1
```

**Linux/Mac:**
```bash
chmod +x start-system.sh
./start-system.sh
```

### Step 2: Wait for Services to Initialize

Wait approximately 30 seconds for all services to start up and initialize. You can check the status with:

```bash
docker-compose ps
```

### Step 3: Register the Debezium Connector

After the services are up, register the Debezium connector to start capturing changes from the PostgreSQL leader:

**On Windows (PowerShell):**
```powershell
Invoke-WebRequest -Uri "http://localhost:8083/connectors" -Method POST -ContentType "application/json" -InFile ".\debezium\register-postgres.json"
```

**On Linux/Mac:**
```bash
curl -X POST \
  http://localhost:8083/connectors \
  -H 'Content-Type: application/json' \
  -H 'Accept: application/json' \
  -d @debezium/register-postgres.json
```

### Step 4: Verify the System

1. **Access the CQRS Write App**: Open your browser and navigate to `http://localhost:8080`
2. **Check Debezium Connector**: Verify the connector is registered:
   ```bash
   curl http://localhost:8083/connectors
   ```
3. **View Demo Page**: Navigate to `http://localhost:8080/Home/Demo` to see the round-robin load balancing in action

## How It Works

### Write Path
1. The **CQRS Write Application** receives write requests (create, update, delete operations)
2. Writes are sent to the **PostgreSQL Leader** database (single source of truth)
3. The leader database stores the changes

### Replication Path
1. **Debezium** monitors the PostgreSQL leader using logical replication
2. When changes occur, Debezium captures them and sends events to **Redpanda**
3. The **Consumer Application** reads events from Redpanda
4. The consumer replicates the changes to all **3 read replicas** simultaneously

### Read Path
1. The **CQRS Write Application** receives read requests
2. Reads are distributed across replicas using **round-robin load balancing**:
   - First read → Replica 1
   - Second read → Replica 2
   - Third read → Replica 3
   - Fourth read → Replica 1 (cycles back)
3. This distributes the read load evenly across all replicas

### Demo Page

The Demo page (`/Home/Demo`) allows you to:
- Make 1000 sequential read requests
- See which replica handled each request
- View statistics showing the distribution across replicas
- Observe round-robin load balancing in real-time

## Services and Ports

| Service | Port | Description |
|---------|------|-------------|
| PostgreSQL Leader | `localhost:5432` | Main database for writes |
| PostgreSQL Read Replica 1 | `localhost:5433` | First read replica |
| PostgreSQL Read Replica 2 | `localhost:5434` | Second read replica |
| PostgreSQL Read Replica 3 | `localhost:5435` | Third read replica |
| Redpanda | `localhost:19092` | Kafka-compatible event streaming |
| Debezium Connect | `localhost:8083` | CDC connector REST API |
| CQRS Write App | `localhost:8080` | Web application UI |

## Monitoring

### Check Container Status
```bash
docker-compose ps
```

### View Logs
View logs for a specific service:
```bash
docker-compose logs -f <service-name>
```

For example:
```bash
docker-compose logs -f consumer-app
docker-compose logs -f debezium-connect
docker-compose logs -f cqrs-write-app
```


### Check Debezium Connector Status
```bash
curl http://localhost:8083/connectors/postgres-connector/status
```

### View Redpanda Topics
```bash
docker-compose exec redpanda rpk topic list
```

### Using Redpanda Console
You can use Redpanda's console to view topics and messages in a web interface.

## Stopping the System

To stop all services:
```bash
docker-compose down
```

To stop and remove all volumes (⚠️ this will delete all data):
```bash
docker-compose down -v
```

## Troubleshooting

### Services Not Starting
- Ensure Docker and Docker Compose are running
- Check if ports are already in use: `netstat -an | grep <port>`
- View logs: `docker-compose logs <service-name>`

### Debezium Connector Not Working
- Verify Redpanda is running: `docker-compose ps redpanda`
- Check Debezium logs: `docker-compose logs debezium-connect`
- Ensure the connector was registered successfully

### Replicas Not Syncing
- Check Consumer App logs: `docker-compose logs consumer-app`
- Verify Redpanda has messages: `docker-compose exec redpanda rpk topic consume <topic-name>`
- Ensure Debezium is capturing changes from the leader

## Project Structure

```
.
├── cqrs-write-app/          # ASP.NET Core MVC application (write side + reads)
├── consumer-app/             # .NET consumer that replicates to read replicas
├── postgres/
│   ├── init-scripts/         # Leader database initialization
│   └── read-replica-scripts/ # Replica initialization
├── debezium/
│   └── register-postgres.json # Debezium connector configuration
├── redpanda/
│   └── redpanda.yaml        # Redpanda configuration
├── docker-compose.yml        # All services definition
├── start-system.ps1         # Windows startup script
├── start-system.sh          # Linux/Mac startup script
└── README.md                # This file
```


## License

This is a demonstration project for educational purposes.


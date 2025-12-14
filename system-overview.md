# Distributed CQRS System with CDC - Complete Overview

## System Components

This distributed system implements a CQRS (Command Query Responsibility Segregation) architecture with Change Data Capture (CDC) to synchronize data between a write database and multiple read replicas.

### 1. PostgreSQL Leader Database
- **Role**: Primary write database
- **Image**: postgres:16
- **Port**: 5432
- **Database**: cqrs_leader
- **Features**: 
  - Contains products table with sample data
  - Configured with Debezium user for CDC
  - Automatic timestamp updates on record changes

### 2. PostgreSQL Read Replicas (3 instances)
- **Role**: Serve read queries for scalability
- **Images**: postgres:16
- **Ports**: 5433, 5434, 5435
- **Database**: cqrs_read
- **Features**:
  - Simple products table schema
  - Receive replicated data from consumer application

### 3. Debezium Connector
- **Role**: Capture changes from leader database
- **Image**: debezium/connect:2.7.3.Final
- **Port**: 8083
- **Features**:
  - Monitors PostgreSQL leader for data changes
  - Publishes change events to Redpanda
  - Uses logical replication

### 4. Redpanda (Kafka-compatible Event Streaming)
- **Role**: Event streaming platform
- **Image**: redpandadata/redpanda:v25.2.12-fips
- **Ports**: 19092 (Kafka API), 18081 (Schema Registry), 18082 (HTTP Proxy), 9644 (Admin)
- **Features**:
  - Receives events from Debezium
  - Delivers events to consumer application
  - Kafka-compatible API

### 5. CQRS Write Application
- **Role**: Sample application that writes to leader database
- **Image**: mcr.microsoft.com/dotnet/aspnet:9.0
- **Port**: 8080
- **Technology**: ASP.NET Core with Npgsql
- **Features**:
  - Demonstrates adding new products
  - Demonstrates updating existing products
  - Shows current product data

### 6. Consumer Application
- **Role**: Reads events from Redpanda and replicates to read replicas
- **Image**: mcr.microsoft.com/dotnet/aspnet:9.0
- **Technology**: .NET Core with Confluent.Kafka and Npgsql
- **Features**:
  - Subscribes to product change events
  - Processes Create/Update/Delete operations
  - Replicates changes to all 3 read replicas

## Data Flow

1. **Write Operation**: 
   - CQRS Write App modifies data in PostgreSQL Leader
   - Changes are automatically captured by PostgreSQL's logical replication

2. **Change Capture**: 
   - Debezium Connector detects changes via logical replication
   - Debezium transforms changes into events and publishes to Redpanda

3. **Event Streaming**: 
   - Redpanda receives and stores events
   - Events are made available for consumption

4. **Data Replication**: 
   - Consumer App reads events from Redpanda
   - Consumer App applies the same changes to all 3 read replicas

5. **Read Operations**: 
   - Applications can query any of the 3 read replicas
   - Provides scalability for read-heavy workloads

## Benefits of This Architecture

1. **Scalability**: Read operations can scale horizontally by adding more read replicas
2. **Performance**: Write and read operations don't compete for database resources
3. **Fault Tolerance**: Failure of a read replica doesn't affect the system
4. **Real-time Synchronization**: Near real-time data consistency across all databases
5. **Technology Flexibility**: Different technologies can be used for write and read sides

## Running the System

1. Execute `start-system.ps1` (Windows) or `start-system.sh` (Linux/Mac)
2. Execute `simulate-workflow.ps1` (Windows) or `simulate-workflow.sh` (Linux/Mac) to see the data flow

## Monitoring and Debugging

- Check container logs: `docker-compose logs -f <service-name>`
- Connect directly to databases using any PostgreSQL client
- Monitor Redpanda topics using its admin interface
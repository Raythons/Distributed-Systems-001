
-- -- -- Create a dedicated user for Debezium with the necessary permissions
-- -- -- The 'REPLICATION' keyword here is what Debezium needs.
-- -- CREATE USER debezium WITH PASSWORD 'debezium' REPLICATION;
-- -- GRANT CONNECT ON DATABASE cqrs_leader TO debezium;
-- -- GRANT USAGE ON SCHEMA public TO debezium;
-- -- GRANT SELECT ON ALL TABLES IN SCHEMA public TO debezium;

-- -- -- -- Ensure future tables will also grant SELECT to debezium
-- -- -- ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT ON TABLES TO debezium;-- 1. Grant the REPLICATION role
-- -- ALTER USER debezium WITH REPLICATION;

-- --     -- 2. Grant CONNECT permission on the database
-- --     GRANT CONNECT ON DATABASE cqrs_leader TO debezium;

-- --     -- 3. Grant USAGE on the schema (usually 'public')
-- --     GRANT USAGE ON SCHEMA public TO debezium;

-- --     -- 4. Grant SELECT permission on the table(s) Debezium is monitoring
-- --     GRANT SELECT ON ALL TABLES IN SCHEMA public TO debezium;-- Switch to correct DB
-- \c cqrs_leader;

-- -- Create user with replication
-- CREATE USER debezium WITH PASSWORD 'debezium' REPLICATION;

-- -- Allow user to connect
-- GRANT CONNECT ON DATABASE cqrs_leader TO debezium;

-- -- Allow access to schema
-- GRANT USAGE ON SCHEMA public TO debezium;

-- -- Give SELECT on tables
-- GRANT SELECT ON ALL TABLES IN SCHEMA public TO debezium;

-- -- Ensure future tables are readable
-- ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT ON TABLES TO debezium;
-- IMPORTANT: Switch to the correct DB
\c cqrs_leader;

-- Create Debezium user with replication privileges
CREATE USER debezium WITH PASSWORD 'debezium' REPLICATION;

-- Allow Debezium to connect
GRANT CONNECT ON DATABASE cqrs_leader TO debezium;

-- Allow access to schema
GRANT USAGE ON SCHEMA public TO debezium;

-- Allow Debezium to select from all tables
GRANT SELECT ON ALL TABLES IN SCHEMA public TO debezium;

-- Future tables also readable
ALTER DEFAULT PRIVILEGES IN SCHEMA public
GRANT SELECT ON TABLES TO debezium;

// using System;
// using System.Collections.Generic;
// using System.Text.Json;
// using System.Threading;
// using Npgsql;
// using Confluent.Kafka;

// namespace ConsumerApp
// {
//     class Program
//     {
//         // Connection strings for read replicas
//         private static readonly string[] ReadReplicaConnections = {
//             "Host=postgres-replica-1;Port=5432;Database=cqrs_read;Username=admin;Password=password",
//             "Host=postgres-replica-2;Port=5432;Database=cqrs_read;Username=admin;Password=password",
//             "Host=postgres-replica-3;Port=5432;Database=cqrs_read;Username=admin;Password=password"
//         };

//         static void Main(string[] args)
//         {
//             Console.WriteLine("Consumer Application Started");
//             Console.WriteLine("Listening for events from Redpanda...");

//             // Kafka consumer configuration
//             var consumerConfig = new ConsumerConfig
//             {
//                 BootstrapServers = "redpanda:9092",
//                 GroupId = "cqrs-consumer-group",
//                 AutoOffsetReset = AutoOffsetReset.Earliest,
//                 EnableAutoCommit = true
//             };

//             using var consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
//             consumer.Subscribe("cqrs.public.products");


//             Console.CancelKeyPress += (_, e) =>
//             {
//                 e.Cancel = true;
//                 consumer.Close();
//             };

//             try
//             {
//                 while (true)
//                 {
//                     try
//                     {
//                         var consumeResult = consumer.Consume(CancellationToken.None);

//                         Console.WriteLine($"Received message: {consumeResult.Message.Value}");

//                         // Process the message and replicate to read replicas
//                         ProcessAndReplicate(consumeResult.Message.Value);
//                     }
//                     catch (ConsumeException e)
//                     {
//                         Console.WriteLine($"Consume error: {e.Error.Reason}");
//                     }
//                 }
//             }
//             catch (OperationCanceledException)
//             {
//                 Console.WriteLine("Closing consumer.");
//             }
//             finally
//             {
//                 consumer.Close();
//             }
//         }

//         static void ProcessAndReplicate(string message)
//         {
//             try
//             {
//                 // Parse the JSON message from Debezium
//                 var doc = JsonDocument.Parse(message);
//                 var root = doc.RootElement;

//                 // Extract operation type (c = create, u = update, d = delete)
//                 string operation = root.GetProperty("op").GetString();

//                 // Extract the after state (current values)
//                 if (!root.TryGetProperty("after", out var afterElement) || afterElement.ValueKind == JsonValueKind.Null)
//                 {
//                     Console.WriteLine("No 'after' data found in message");
//                     return;
//                 }

//                 var after = afterElement.Deserialize<Dictionary<string, object>>();

//                 // Extract product data
//                 int id = Convert.ToInt32(after["id"]);
//                 string name = after["name"].ToString();
//                 decimal price = Convert.ToDecimal(after["price"]);
//                 int quantity = Convert.ToInt32(after["quantity"]);

//                 Console.WriteLine($"Processing {operation} operation for product ID {id}");

//                 // Replicate to all read replicas
//                 foreach (var connectionString in ReadReplicaConnections)
//                 {
//                     ReplicateToReadReplica(connectionString, operation, id, name, price, quantity);
//                 }
//             }
//             catch (Exception ex)
//             {
//                 Console.WriteLine($"Error processing message: {ex.Message}");
//             }
//         }

//         static void ReplicateToReadReplica(string connectionString, string operation, int id, string name, decimal price, int quantity)
//         {
//             try
//             {
//                 using var connection = new NpgsqlConnection(connectionString);
//                 connection.Open();

//                 switch (operation)
//                 {
//                     case "c": // Create
//                     case "u": // Update
//                         using (var cmd = new NpgsqlCommand(
//                             @"INSERT INTO products (id, name, price, quantity) 
//                               VALUES (@id, @name, @price, @quantity)
//                               ON CONFLICT (id) 
//                               DO UPDATE SET name = @name, price = @price, quantity = @quantity", 
//                             connection))
//                         {
//                             cmd.Parameters.AddWithValue("id", id);
//                             cmd.Parameters.AddWithValue("name", name);
//                             cmd.Parameters.AddWithValue("price", price);
//                             cmd.Parameters.AddWithValue("quantity", quantity);
//                             cmd.ExecuteNonQuery();
//                         }
//                         break;

//                     case "d": // Delete
//                         using (var cmd = new NpgsqlCommand(
//                             "DELETE FROM products WHERE id = @id", 
//                             connection))
//                         {
//                             cmd.Parameters.AddWithValue("id", id);
//                             cmd.ExecuteNonQuery();
//                         }
//                         break;
//                 }

//                 Console.WriteLine($"Replicated {operation} operation for product ID {id} to read replica");
//             }
//             catch (Exception ex)
//             {
//                 Console.WriteLine($"Error replicating to read replica: {ex.Message}");
//             }
//         }
//     }
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Npgsql;
using Confluent.Kafka;
using System.Numerics;

namespace ConsumerApp
{
    class Program
    {
        // Read replicas
        private static readonly string[] ReadReplicaConnections =
        {
            "Host=postgres-replica-1;Port=5432;Database=cqrs_read;Username=admin;Password=password",
            "Host=postgres-replica-2;Port=5432;Database=cqrs_read;Username=admin;Password=password",
            "Host=postgres-replica-3;Port=5432;Database=cqrs_read;Username=admin;Password=password"
        };

        static void Main(string[] args)
        {
            Console.WriteLine("Consumer Application Started");
            Console.WriteLine("Listening for events from Redpanda...");

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = "redpanda:9092",
                GroupId = "cqrs-consumer-group",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = true
            };

            using var consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();

            // Wait for topic to be available (Debezium creates it when it captures first change)
            Console.WriteLine("Waiting for topic 'cqrs.public.products' to be available...");
            Console.WriteLine("Note: Topic will be created by Debezium when first change is captured from PostgreSQL.");

            int retryCount = 0;
            const int maxRetries = 18; // Wait up to 3 minutes (18 * 10 seconds = 180 seconds)
            bool subscribed = false;

            while (!subscribed && retryCount < maxRetries)
            {
                try
                {
                    consumer.Subscribe("cqrs.public.products");
                    // Try a quick consume to verify topic exists (with timeout)
                    var testResult = consumer.Consume(TimeSpan.FromSeconds(1));
                    subscribed = true;
                    Console.WriteLine("✓ Successfully subscribed to topic 'cqrs.public.products'");
                }
                catch (ConsumeException ex)
                {
                    if (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
                    {
                        retryCount++;
                        if (retryCount < maxRetries)
                        {
                            Console.WriteLine($"Topic not available yet (attempt {retryCount}/{maxRetries}). Waiting 10 seconds...");
                            Console.WriteLine("   Make sure Debezium connector is registered and there are changes in PostgreSQL.");
                            Thread.Sleep(10000); // 10 seconds
                        }
                        else
                        {
                            Console.WriteLine($"✗ Topic 'cqrs.public.products' not available after {maxRetries} attempts (3 minutes).");
                            Console.WriteLine("   Please ensure:");
                            Console.WriteLine("   1. Debezium connector is registered: curl http://localhost:8083/connectors");
                            Console.WriteLine("   2. There are changes in the PostgreSQL leader database");
                            Console.WriteLine("   3. Redpanda is running and accessible");
                            Environment.Exit(1);
                        }
                    }
                    else
                    {
                        // Other consume errors - might be partition EOF which is OK
                        subscribed = true;
                        Console.WriteLine("✓ Successfully subscribed to topic 'cqrs.public.products'");
                    }
                }
                catch (KafkaException)
                {
                    retryCount++;
                    if (retryCount < maxRetries)
                    {
                        Console.WriteLine($"Connection issue (attempt {retryCount}/{maxRetries}). Waiting 10 seconds...");
                        Thread.Sleep(10000); // 10 seconds
                    }
                    else
                    {
                        Console.WriteLine($"✗ Failed to connect after {maxRetries} attempts (3 minutes).");
                        throw;
                    }
                }
            }

            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                consumer.Close();
            };

            try
            {
                while (true)
                {
                    try
                    {
                        var consumeResult = consumer.Consume(TimeSpan.FromSeconds(1));
                        if (consumeResult != null)
                        {
                            Console.WriteLine($"Received message: {consumeResult.Message.Value}");
                            ProcessAndReplicate(consumeResult.Message.Value);
                        }
                    }
                    catch (ConsumeException ex)
                    {
                        if (ex.Error.Code == ErrorCode.Local_PartitionEOF)
                        {
                            // End of partition - this is normal, just continue
                            continue;
                        }
                        else
                        {
                            Console.WriteLine($"Consume error: {ex.Error.Reason}");
                            // Wait a bit before retrying
                            Thread.Sleep(1000);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Consumer stopped.");
            }
            finally
            {
                consumer.Close();
            }
        }

        static void ProcessAndReplicate(string message)
        {
            try
            {
                var root = JsonDocument.Parse(message).RootElement;

                int id = root.GetProperty("id").GetInt32();
                string name = root.GetProperty("name").GetString();
                int quantity = root.GetProperty("quantity").GetInt32();

                string priceBase64 = root.GetProperty("price").GetString();
                decimal price = DecodeDebeziumDecimal(priceBase64);

                foreach (var conn in ReadReplicaConnections)
                {
                    ReplicateToReadReplica(conn, id, name, price, quantity);
                }

                Console.WriteLine($"Replicated product ID {id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message: {ex.Message}");
            }
        }

        static void ReplicateToReadReplica(string connectionString, int id, string name, decimal price, int quantity)
        {
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();

            using var cmd = new NpgsqlCommand(
                @"INSERT INTO products (id, name, price, quantity)
                  VALUES (@id, @name, @price, @quantity)
                  ON CONFLICT (id)
                  DO UPDATE SET
                    name = EXCLUDED.name,
                    price = EXCLUDED.price,
                    quantity = EXCLUDED.quantity",
                connection);

            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("name", name);
            cmd.Parameters.AddWithValue("price", price);
            cmd.Parameters.AddWithValue("quantity", quantity);

            cmd.ExecuteNonQuery();
        }

        // Debezium DECIMAL decoder
        static decimal DecodeDebeziumDecimal(string base64)
        {
            var bytes = Convert.FromBase64String(base64);
            var unscaled = new BigInteger(bytes, isUnsigned: false, isBigEndian: true);

            const int scale = 2; // DECIMAL(10,2)
            return (decimal)unscaled / (decimal)Math.Pow(10, scale);
        }
    }
}

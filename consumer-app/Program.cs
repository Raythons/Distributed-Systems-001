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
// }using System;
using System.Text.Json;
using System.Threading;
using Npgsql;
using Confluent.Kafka;
using System.Numerics;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using Npgsql;
using Confluent.Kafka;

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
            consumer.Subscribe("cqrs.public.products");

            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                consumer.Close();
            };

            try
            {
                while (true)
                {
                    var consumeResult = consumer.Consume(CancellationToken.None);
                    Console.WriteLine($"Received message: {consumeResult.Message.Value}");
                    ProcessAndReplicate(consumeResult.Message.Value);
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

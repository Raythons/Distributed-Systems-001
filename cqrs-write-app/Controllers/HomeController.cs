using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using cqrs_write_app.Models;
using cqrs_write_app.Services;
using Npgsql;
using System.Data;
using System.Collections.Generic;
using System.Linq;

namespace cqrs_write_app.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly RoundRobinService _roundRobinService;
    private const string LeaderConnectionString = "Host=postgres-leader;Port=5432;Database=cqrs_leader;Username=admin;Password=password";
    private readonly string[] ReadReplicaConnections = {
        "Host=postgres-replica-1;Port=5432;Database=cqrs_read;Username=admin;Password=password",
        "Host=postgres-replica-2;Port=5432;Database=cqrs_read;Username=admin;Password=password",
        "Host=postgres-replica-3;Port=5432;Database=cqrs_read;Username=admin;Password=password"
    };

    public HomeController(ILogger<HomeController> logger, RoundRobinService roundRobinService)
    {
        _logger = logger;
        _roundRobinService = roundRobinService;
    }

    public IActionResult Index()
    {
        List<Product> products = new List<Product>();

        try
        {
            using var connection = new NpgsqlConnection(LeaderConnectionString);
            connection.Open();

            using var cmd = new NpgsqlCommand("SELECT id, name, price, quantity FROM products ORDER BY id", connection);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                products.Add(new Product
                {
                    Id = (int)reader["id"],
                    Name = reader["name"].ToString() ?? "",
                    Price = (decimal)reader["price"],
                    Quantity = (int)reader["quantity"]
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading products");
            TempData["Error"] = "Error loading products: " + ex.Message;
        }

        return View(products);
    }

    // New method to get sync status for a product
    public JsonResult GetSyncStatus(int productId)
    {
        var statuses = new List<object>();

        try
        {
            // Get the product from leader
            Product leaderProduct = null;
            using (var connection = new NpgsqlConnection(LeaderConnectionString))
            {
                connection.Open();
                using var cmd = new NpgsqlCommand("SELECT id, name, price, quantity FROM products WHERE id = @id", connection);
                cmd.Parameters.AddWithValue("id", productId);
                using var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    leaderProduct = new Product
                    {
                        Id = (int)reader["id"],
                        Name = reader["name"].ToString() ?? "",
                        Price = (decimal)reader["price"],
                        Quantity = (int)reader["quantity"]
                    };
                }
            }

            if (leaderProduct == null)
            {
                return Json(new { error = "Product not found in leader database" });
            }

            // Check each replica
            for (int i = 0; i < ReadReplicaConnections.Length; i++)
            {
                bool isSynced = false;
                string errorMessage = "";

                try
                {
                    using var connection = new NpgsqlConnection(ReadReplicaConnections[i]);
                    connection.Open();

                    using var cmd = new NpgsqlCommand("SELECT id, name, price, quantity FROM products WHERE id = @id", connection);
                    cmd.Parameters.AddWithValue("id", productId);
                    using var reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        var replicaProduct = new Product
                        {
                            Id = (int)reader["id"],
                            Name = reader["name"].ToString() ?? "",
                            Price = (decimal)reader["price"],
                            Quantity = (int)reader["quantity"]
                        };

                        // Compare with leader product
                        isSynced = (replicaProduct.Name == leaderProduct.Name &&
                                   replicaProduct.Price == leaderProduct.Price &&
                                   replicaProduct.Quantity == leaderProduct.Quantity);
                    }
                    else
                    {
                        // Product not found in replica
                        isSynced = false;
                    }
                }
                catch (Exception ex)
                {
                    errorMessage = ex.Message;
                }

                statuses.Add(new
                {
                    replicaId = i + 1,
                    isSynced = isSynced,
                    error = errorMessage
                });
            }
        }
        catch (Exception ex)
        {
            return Json(new { error = "Error checking sync status: " + ex.Message });
        }

        return Json(new { statuses = statuses });
    }

    // Method to get overall system status
    public JsonResult GetSystemStatus()
    {
        var status = new
        {
            leaderDb = CheckDatabaseConnection(LeaderConnectionString, "Leader"),
            replica1 = CheckDatabaseConnection(ReadReplicaConnections[0], "Replica 1"),
            replica2 = CheckDatabaseConnection(ReadReplicaConnections[1], "Replica 2"),
            replica3 = CheckDatabaseConnection(ReadReplicaConnections[2], "Replica 3")
        };

        return Json(status);
    }

    private object CheckDatabaseConnection(string connectionString, string dbName)
    {
        try
        {
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();

            using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM products", connection);
            var count = (long)cmd.ExecuteScalar();

            return new
            {
                name = dbName,
                status = "Connected",
                productCount = count,
                error = (string)null
            };
        }
        catch (Exception ex)
        {
            return new
            {
                name = dbName,
                status = "Disconnected",
                productCount = 0,
                error = ex.Message
            };
        }
    }

    // Action for System Status page
    public IActionResult SystemStatus()
    {
        return View();
    }

    // Action for Architecture Diagram page
    public IActionResult ArchitectureDiagram()
    {
        return View();
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Create(Product product)
    {
        if (ModelState.IsValid)
        {
            try
            {
                using var connection = new NpgsqlConnection(LeaderConnectionString);
                connection.Open();

                using var cmd = new NpgsqlCommand(
                    "INSERT INTO products (name, price, quantity) VALUES (@name, @price, @quantity)",
                    connection);
                cmd.Parameters.AddWithValue("name", product.Name);
                cmd.Parameters.AddWithValue("price", product.Price);
                cmd.Parameters.AddWithValue("quantity", product.Quantity);

                cmd.ExecuteNonQuery();
                TempData["Success"] = "Product added successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product");
                TempData["Error"] = "Error adding product: " + ex.Message;
            }
        }

        return View(product);
    }

    public IActionResult Edit(int id)
    {
        Product product = null;

        try
        {
            using var connection = new NpgsqlConnection(LeaderConnectionString);
            connection.Open();

            using var cmd = new NpgsqlCommand("SELECT id, name, price, quantity FROM products WHERE id = @id", connection);
            cmd.Parameters.AddWithValue("id", id);
            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                product = new Product
                {
                    Id = (int)reader["id"],
                    Name = reader["name"].ToString() ?? "",
                    Price = (decimal)reader["price"],
                    Quantity = (int)reader["quantity"]
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading product");
            TempData["Error"] = "Error loading product: " + ex.Message;
        }

        if (product == null)
        {
            return NotFound();
        }

        return View(product);
    }

    [HttpPost]
    public IActionResult Edit(Product product)
    {
        if (ModelState.IsValid)
        {
            try
            {
                using var connection = new NpgsqlConnection(LeaderConnectionString);
                connection.Open();

                using var cmd = new NpgsqlCommand(
                    "UPDATE products SET name = @name, price = @price, quantity = @quantity WHERE id = @id",
                    connection);
                cmd.Parameters.AddWithValue("id", product.Id);
                cmd.Parameters.AddWithValue("name", product.Name);
                cmd.Parameters.AddWithValue("price", product.Price);
                cmd.Parameters.AddWithValue("quantity", product.Quantity);

                cmd.ExecuteNonQuery();
                TempData["Success"] = "Product updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product");
                TempData["Error"] = "Error updating product: " + ex.Message;
            }
        }

        return View(product);
    }

    [HttpPost]
    public IActionResult Delete(int id)
    {
        try
        {
            using var connection = new NpgsqlConnection(LeaderConnectionString);
            connection.Open();

            using var cmd = new NpgsqlCommand("DELETE FROM products WHERE id = @id", connection);
            cmd.Parameters.AddWithValue("id", id);

            cmd.ExecuteNonQuery();
            TempData["Success"] = "Product deleted successfully!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product");
            TempData["Error"] = "Error deleting product: " + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    public IActionResult Privacy()
    {
        return View();
    }

    // Demo page action
    public IActionResult Demo()
    {
        return View();
    }

    // API endpoint to fetch data from replicas using round-robin
    [HttpGet]
    public JsonResult ReadFromReplica()
    {
        try
        {
            // Get next replica using round-robin
            var (connectionString, replicaNumber) = _roundRobinService.GetNextReplicaWithNumber();

            var products = new List<Product>();
            var timestamp = DateTime.UtcNow;

            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();

            using var cmd = new NpgsqlCommand("SELECT id, name, price, quantity FROM products ORDER BY id LIMIT 100", connection);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                products.Add(new Product
                {
                    Id = (int)reader["id"],
                    Name = reader["name"].ToString() ?? "",
                    Price = (decimal)reader["price"],
                    Quantity = (int)reader["quantity"]
                });
            }

            return Json(new
            {
                success = true,
                replicaNumber = replicaNumber,
                replicaConnection = connectionString,
                timestamp = timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                productCount = products.Count,
                products = products
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading from replica");
            return Json(new
            {
                success = false,
                error = ex.Message,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff")
            });
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
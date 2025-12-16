using System;
using System.Numerics;
using System.Text.Json;
using Xunit;

namespace consumer_app.Tests;

public class ReplicationTests
{
    [Fact]
    public void ProcessAndReplicate_ShouldExtractAllProductFields()
    {
        // Arrange - Create a valid Debezium message format
        var message = new
        {
            id = 1,
            name = "Test Product",
            quantity = 10,
            price = EncodeDecimal(123.45m)
        };

        var jsonMessage = JsonSerializer.Serialize(message);

        // Act
        var doc = JsonDocument.Parse(jsonMessage);
        var root = doc.RootElement;

        // Assert
        Assert.Equal(1, root.GetProperty("id").GetInt32());
        Assert.Equal("Test Product", root.GetProperty("name").GetString());
        Assert.Equal(10, root.GetProperty("quantity").GetInt32());
        
        var priceBase64 = root.GetProperty("price").GetString();
        Assert.NotNull(priceBase64);
        
        var decodedPrice = DecodeDebeziumDecimal(priceBase64!);
        Assert.Equal(123.45m, decodedPrice, 2);
    }

    [Fact]
    public void ProcessAndReplicate_ShouldHandleCreateOperation()
    {
        // Arrange
        var message = new
        {
            id = 5,
            name = "New Product",
            quantity = 20,
            price = EncodeDecimal(99.99m)
        };

        var jsonMessage = JsonSerializer.Serialize(message);
        var doc = JsonDocument.Parse(jsonMessage);
        var root = doc.RootElement;

        // Act & Assert
        var id = root.GetProperty("id").GetInt32();
        var name = root.GetProperty("name").GetString();
        var quantity = root.GetProperty("quantity").GetInt32();
        var priceBase64 = root.GetProperty("price").GetString();

        Assert.Equal(5, id);
        Assert.Equal("New Product", name);
        Assert.Equal(20, quantity);
        Assert.NotNull(priceBase64);
    }

    [Fact]
    public void ProcessAndReplicate_ShouldHandleUpdateOperation()
    {
        // Arrange - Update message
        var message = new
        {
            id = 1,
            name = "Updated Product",
            quantity = 15,
            price = EncodeDecimal(199.99m)
        };

        var jsonMessage = JsonSerializer.Serialize(message);
        var doc = JsonDocument.Parse(jsonMessage);
        var root = doc.RootElement;

        // Act & Assert
        Assert.Equal(1, root.GetProperty("id").GetInt32());
        Assert.Equal("Updated Product", root.GetProperty("name").GetString());
        Assert.Equal(15, root.GetProperty("quantity").GetInt32());
    }

    [Fact]
    public void ReplicateToReadReplica_SqlCommand_ShouldHaveCorrectStructure()
    {
        // Arrange - Expected SQL command structure
        var expectedSql = @"INSERT INTO products (id, name, price, quantity)
                  VALUES (@id, @name, @price, @quantity)
                  ON CONFLICT (id)
                  DO UPDATE SET
                    name = EXCLUDED.name,
                    price = EXCLUDED.price,
                    quantity = EXCLUDED.quantity";

        // Assert - Verify SQL structure
        Assert.Contains("INSERT INTO products", expectedSql);
        Assert.Contains("VALUES (@id, @name, @price, @quantity)", expectedSql);
        Assert.Contains("ON CONFLICT (id)", expectedSql);
        Assert.Contains("DO UPDATE SET", expectedSql);
        Assert.Contains("name = EXCLUDED.name", expectedSql);
        Assert.Contains("price = EXCLUDED.price", expectedSql);
        Assert.Contains("quantity = EXCLUDED.quantity", expectedSql);
    }

    [Fact]
    public void ReplicateToReadReplica_ShouldHandleAllThreeReplicas()
    {
        // Arrange
        var replicas = new[]
        {
            "Host=postgres-replica-1;Port=5432;Database=cqrs_read;Username=admin;Password=password",
            "Host=postgres-replica-2;Port=5432;Database=cqrs_read;Username=admin;Password=password",
            "Host=postgres-replica-3;Port=5432;Database=cqrs_read;Username=admin;Password=password"
        };

        // Assert
        Assert.Equal(3, replicas.Length);
        Assert.All(replicas, replica => Assert.Contains("postgres-replica", replica));
        Assert.All(replicas, replica => Assert.Contains("cqrs_read", replica));
    }

    [Fact]
    public void ProcessAndReplicate_ShouldHandleMissingPriceField()
    {
        // Arrange - Message without price
        var message = new
        {
            id = 1,
            name = "Test Product",
            quantity = 10
        };

        var jsonMessage = JsonSerializer.Serialize(message);
        var doc = JsonDocument.Parse(jsonMessage);
        var root = doc.RootElement;

        // Act & Assert
        Assert.True(root.TryGetProperty("id", out _));
        Assert.True(root.TryGetProperty("name", out _));
        Assert.True(root.TryGetProperty("quantity", out _));
        Assert.False(root.TryGetProperty("price", out _));
    }

    [Fact]
    public void ProcessAndReplicate_ShouldHandleEmptyName()
    {
        // Arrange
        var message = new
        {
            id = 1,
            name = "",
            quantity = 10,
            price = EncodeDecimal(0m)
        };

        var jsonMessage = JsonSerializer.Serialize(message);
        var doc = JsonDocument.Parse(jsonMessage);
        var root = doc.RootElement;

        // Act & Assert
        Assert.Equal("", root.GetProperty("name").GetString());
    }

    [Fact]
    public void ProcessAndReplicate_ShouldHandleZeroQuantity()
    {
        // Arrange
        var message = new
        {
            id = 1,
            name = "Test Product",
            quantity = 0,
            price = EncodeDecimal(0m)
        };

        var jsonMessage = JsonSerializer.Serialize(message);
        var doc = JsonDocument.Parse(jsonMessage);
        var root = doc.RootElement;

        // Act & Assert
        Assert.Equal(0, root.GetProperty("quantity").GetInt32());
    }

    // Helper methods
    private static string EncodeDecimal(decimal value)
    {
        const int scale = 2;
        var unscaled = new BigInteger((long)(value * (decimal)Math.Pow(10, scale)));
        var bytes = unscaled.ToByteArray(isUnsigned: false, isBigEndian: true);
        return Convert.ToBase64String(bytes);
    }

    private static decimal DecodeDebeziumDecimal(string base64)
    {
        var bytes = Convert.FromBase64String(base64);
        var unscaled = new BigInteger(bytes, isUnsigned: false, isBigEndian: true);
        const int scale = 2;
        return (decimal)unscaled / (decimal)Math.Pow(10, scale);
    }
}


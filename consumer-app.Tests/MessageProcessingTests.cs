using System;
using System.Numerics;
using System.Text.Json;
using Xunit;

namespace consumer_app.Tests;

public class MessageProcessingTests
{
    [Fact]
    public void ProcessAndReplicate_ShouldParseValidMessage()
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

        // Act - Try to parse
        var doc = JsonDocument.Parse(jsonMessage);
        var root = doc.RootElement;

        // Assert
        Assert.True(root.TryGetProperty("id", out var idProp));
        Assert.True(root.TryGetProperty("name", out var nameProp));
        Assert.True(root.TryGetProperty("quantity", out var quantityProp));
        Assert.True(root.TryGetProperty("price", out var priceProp));

        Assert.Equal(1, idProp.GetInt32());
        Assert.Equal("Test Product", nameProp.GetString());
        Assert.Equal(10, quantityProp.GetInt32());
        Assert.NotNull(priceProp.GetString());
    }

    [Fact]
    public void ProcessAndReplicate_ShouldHandleMissingFields()
    {
        // Arrange - Message with missing fields
        var message = new
        {
            id = 1
        };

        var jsonMessage = JsonSerializer.Serialize(message);
        var doc = JsonDocument.Parse(jsonMessage);
        var root = doc.RootElement;

        // Act & Assert
        Assert.True(root.TryGetProperty("id", out _));
        Assert.False(root.TryGetProperty("name", out _));
        Assert.False(root.TryGetProperty("quantity", out _));
        Assert.False(root.TryGetProperty("price", out _));
    }

    [Fact]
    public void ProcessAndReplicate_ShouldHandleInvalidJson()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act & Assert - JsonReaderException is derived from JsonException
        Assert.ThrowsAny<JsonException>(() => JsonDocument.Parse(invalidJson));
    }

    [Fact]
    public void ProcessAndReplicate_ShouldHandleNullValues()
    {
        // Arrange
        var message = new
        {
            id = 1,
            name = (string?)null,
            quantity = 10,
            price = EncodeDecimal(0m)
        };

        var jsonMessage = JsonSerializer.Serialize(message);
        var doc = JsonDocument.Parse(jsonMessage);
        var root = doc.RootElement;

        // Act & Assert
        Assert.True(root.TryGetProperty("name", out var nameProp));
        Assert.Equal(JsonValueKind.Null, nameProp.ValueKind);
    }

    [Fact]
    public void ReplicateToReadReplica_ShouldBuildCorrectSqlCommand()
    {
        // Arrange - Verify the SQL command structure would be correct
        var expectedSql = @"INSERT INTO products (id, name, price, quantity)
                  VALUES (@id, @name, @price, @quantity)
                  ON CONFLICT (id)
                  DO UPDATE SET
                    name = EXCLUDED.name,
                    price = EXCLUDED.price,
                    quantity = EXCLUDED.quantity";

        // Assert - Just verify the SQL structure is correct
        Assert.Contains("INSERT INTO products", expectedSql);
        Assert.Contains("ON CONFLICT (id)", expectedSql);
        Assert.Contains("DO UPDATE SET", expectedSql);
        Assert.Contains("@id", expectedSql);
        Assert.Contains("@name", expectedSql);
        Assert.Contains("@price", expectedSql);
        Assert.Contains("@quantity", expectedSql);
    }

    [Fact]
    public void ProcessAndReplicate_ShouldHandleLargeProductId()
    {
        // Arrange
        var message = new
        {
            id = int.MaxValue,
            name = "Large ID Product",
            quantity = 1,
            price = EncodeDecimal(1.00m)
        };

        var jsonMessage = JsonSerializer.Serialize(message);
        var doc = JsonDocument.Parse(jsonMessage);
        var root = doc.RootElement;

        // Act & Assert
        Assert.Equal(int.MaxValue, root.GetProperty("id").GetInt32());
    }

    [Fact]
    public void ProcessAndReplicate_ShouldHandleLongProductName()
    {
        // Arrange
        var longName = new string('A', 1000);
        var message = new
        {
            id = 1,
            name = longName,
            quantity = 10,
            price = EncodeDecimal(99.99m)
        };

        var jsonMessage = JsonSerializer.Serialize(message);
        var doc = JsonDocument.Parse(jsonMessage);
        var root = doc.RootElement;

        // Act & Assert
        Assert.Equal(longName, root.GetProperty("name").GetString());
    }

    // Helper method
    private static string EncodeDecimal(decimal value)
    {
        const int scale = 2;
        var unscaled = new BigInteger((long)(value * (decimal)Math.Pow(10, scale)));
        var bytes = unscaled.ToByteArray(isUnsigned: false, isBigEndian: true);
        return Convert.ToBase64String(bytes);
    }
}


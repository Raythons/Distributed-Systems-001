using System;
using System.Numerics;
using System.Reflection;
using Xunit;

namespace consumer_app.Tests;

public class ProgramTests
{
    private static decimal DecodeDebeziumDecimal(string base64)
    {
        var bytes = Convert.FromBase64String(base64);
        var unscaled = new BigInteger(bytes, isUnsigned: false, isBigEndian: true);
        const int scale = 2; // DECIMAL(10,2)
        return (decimal)unscaled / (decimal)Math.Pow(10, scale);
    }

    [Fact]
    public void DecodeDebeziumDecimal_ShouldDecodeCorrectly()
    {
        // Arrange - Create a test decimal value
        decimal originalValue = 123.45m;
        
        // Convert to BigInteger (simulating Debezium encoding)
        var unscaled = new BigInteger((long)(originalValue * 100));
        var bytes = unscaled.ToByteArray(isUnsigned: false, isBigEndian: true);
        var base64 = Convert.ToBase64String(bytes);

        // Act
        var result = DecodeDebeziumDecimal(base64);

        // Assert
        Assert.Equal(originalValue, result, 2); // Allow 2 decimal places precision
    }

    [Fact]
    public void DecodeDebeziumDecimal_ShouldHandleZero()
    {
        // Arrange
        var unscaled = new BigInteger(0);
        var bytes = unscaled.ToByteArray(isUnsigned: false, isBigEndian: true);
        var base64 = Convert.ToBase64String(bytes);

        // Act
        var result = DecodeDebeziumDecimal(base64);

        // Assert
        Assert.Equal(0m, result);
    }

    [Fact]
    public void DecodeDebeziumDecimal_ShouldHandleNegativeValues()
    {
        // Arrange
        decimal originalValue = -99.99m;
        var unscaled = new BigInteger((long)(originalValue * 100));
        var bytes = unscaled.ToByteArray(isUnsigned: false, isBigEndian: true);
        var base64 = Convert.ToBase64String(bytes);

        // Act
        var result = DecodeDebeziumDecimal(base64);

        // Assert
        Assert.Equal(originalValue, result, 2);
    }

    [Fact]
    public void DecodeDebeziumDecimal_ShouldHandleLargeValues()
    {
        // Arrange
        decimal originalValue = 999999.99m;
        var unscaled = new BigInteger((long)(originalValue * 100));
        var bytes = unscaled.ToByteArray(isUnsigned: false, isBigEndian: true);
        var base64 = Convert.ToBase64String(bytes);

        // Act
        var result = DecodeDebeziumDecimal(base64);

        // Assert
        Assert.Equal(originalValue, result, 2);
    }
}


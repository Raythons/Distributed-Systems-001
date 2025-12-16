using cqrs_write_app.Services;
using Xunit;

namespace cqrs_write_app.Tests.Services;

public class RoundRobinServiceTests
{
    [Fact]
    public void GetNextReplica_ShouldCycleThroughReplicas()
    {
        // Arrange
        var replicas = new[] { "replica1", "replica2", "replica3" };
        var service = new RoundRobinService(replicas);

        // Act & Assert
        Assert.Equal("replica1", service.GetNextReplica());
        Assert.Equal("replica2", service.GetNextReplica());
        Assert.Equal("replica3", service.GetNextReplica());
        Assert.Equal("replica1", service.GetNextReplica()); // Should cycle back
    }

    [Fact]
    public void GetNextReplicaWithNumber_ShouldReturnConnectionStringAndNumber()
    {
        // Arrange
        var replicas = new[] { "replica1", "replica2", "replica3" };
        var service = new RoundRobinService(replicas);

        // Act
        var (connection1, number1) = service.GetNextReplicaWithNumber();
        var (connection2, number2) = service.GetNextReplicaWithNumber();
        var (connection3, number3) = service.GetNextReplicaWithNumber();

        // Assert
        Assert.Equal("replica1", connection1);
        Assert.Equal(1, number1);
        Assert.Equal("replica2", connection2);
        Assert.Equal(2, number2);
        Assert.Equal("replica3", connection3);
        Assert.Equal(3, number3);
    }

    [Fact]
    public void GetNextReplicaWithNumber_ShouldCycleCorrectly()
    {
        // Arrange
        var replicas = new[] { "replica1", "replica2", "replica3" };
        var service = new RoundRobinService(replicas);

        // Act - Get all 3, then get first one again
        var first = service.GetNextReplicaWithNumber();
        service.GetNextReplicaWithNumber();
        service.GetNextReplicaWithNumber();
        var fourth = service.GetNextReplicaWithNumber();

        // Assert
        Assert.Equal("replica1", first.ConnectionString);
        Assert.Equal(1, first.ReplicaNumber);
        Assert.Equal("replica1", fourth.ConnectionString);
        Assert.Equal(1, fourth.ReplicaNumber);
    }

    [Fact]
    public async Task RoundRobinService_ShouldBeThreadSafe()
    {
        // Arrange
        var replicas = new[] { "replica1", "replica2", "replica3" };
        var service = new RoundRobinService(replicas);
        var results = new List<string>();
        var tasks = new List<Task>();

        // Act - Simulate concurrent access
        for (int i = 0; i < 30; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                var replica = service.GetNextReplica();
                lock (results)
                {
                    results.Add(replica);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - All replicas should be accessed
        Assert.Equal(30, results.Count);
        var replica1Count = results.Count(r => r == "replica1");
        var replica2Count = results.Count(r => r == "replica2");
        var replica3Count = results.Count(r => r == "replica3");

        // Should be roughly evenly distributed (10 each, but may vary due to timing)
        Assert.True(replica1Count >= 8 && replica1Count <= 12);
        Assert.True(replica2Count >= 8 && replica2Count <= 12);
        Assert.True(replica3Count >= 8 && replica3Count <= 12);
    }

    [Fact]
    public void RoundRobinService_Constructor_ShouldThrowOnNullReplicas()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RoundRobinService(null!));
    }

    [Fact]
    public void GetNextReplica_WithSingleReplica_ShouldAlwaysReturnSame()
    {
        // Arrange
        var replicas = new[] { "single-replica" };
        var service = new RoundRobinService(replicas);

        // Act & Assert
        Assert.Equal("single-replica", service.GetNextReplica());
        Assert.Equal("single-replica", service.GetNextReplica());
        Assert.Equal("single-replica", service.GetNextReplica());
    }
}


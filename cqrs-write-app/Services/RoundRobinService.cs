namespace cqrs_write_app.Services;

public class RoundRobinService
{
    private readonly string[] _replicas;
    private int _currentIndex = 0;
    private readonly object _lock = new object();

    public RoundRobinService(string[] replicas)
    {
        _replicas = replicas ?? throw new ArgumentNullException(nameof(replicas));
    }

    public string GetNextReplica()
    {
        lock (_lock)
        {
            var replica = _replicas[_currentIndex];
            _currentIndex = (_currentIndex + 1) % _replicas.Length;
            return replica;
        }
    }

    public (string ConnectionString, int ReplicaNumber) GetNextReplicaWithNumber()
    {
        lock (_lock)
        {
            var replicaIndex = _currentIndex;
            var replica = _replicas[_currentIndex];
            _currentIndex = (_currentIndex + 1) % _replicas.Length;
            return (replica, replicaIndex + 1); // Return 1-based replica number
        }
    }
}


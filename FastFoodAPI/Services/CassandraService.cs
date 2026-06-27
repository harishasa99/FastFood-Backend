namespace FastFoodApi.Services;

public class CassandraService : IDisposable
{
    private readonly Cassandra.ICluster _cluster;
    private readonly Cassandra.ISession _session;

    public CassandraService(IConfiguration config)
    {
        var cfg = config.GetSection("Cassandra");

        _cluster = Cassandra.Cluster.Builder()
            .WithCloudSecureConnectionBundle(cfg["SecureBundlePath"]!)
            .WithCredentials(cfg["ClientId"]!, cfg["ClientSecret"]!)
            .Build();

        _session = _cluster.Connect(cfg["Keyspace"]!);
    }

    public Cassandra.ISession Session => _session;

    public void Dispose()
    {
        _session?.Dispose();
        _cluster?.Dispose();
    }
}
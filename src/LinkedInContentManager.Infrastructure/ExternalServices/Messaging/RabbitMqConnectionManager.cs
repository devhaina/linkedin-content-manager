using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace LinkedInContentManager.Infrastructure.ExternalServices.Messaging;

/// <summary>
/// One lazily-created, shared <see cref="IConnection"/> per process, reused by both the
/// publisher (API/Worker enqueue side) and the consumer (Worker dequeue side). Channels are
/// cheap and NOT thread-safe for concurrent use, so callers create their own <see cref="IChannel"/>
/// from this connection per publish/consume rather than sharing one.
/// </summary>
public sealed class RabbitMqConnectionManager(IOptions<RabbitMqOptions> options) : IAsyncDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;

    public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true })
            return _connection;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { IsOpen: true })
                return _connection;

            var o = options.Value;
            var factory = new ConnectionFactory
            {
                HostName = o.HostName,
                Port = o.Port,
                UserName = o.UserName,
                Password = o.Password,
                VirtualHost = o.VirtualHost,
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);
            return _connection;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();
        _lock.Dispose();
    }
}

using RabbitMQ.Client;
using EasyNetQ;
namespace Intellimix_Template.Messaging
{

    public interface IEventBus
    {
        public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
            where T : class;

        public Task SubscribeAsync<T>(Func<T, Task> handler, CancellationToken cancellationToken = default)
            where T : class;
    }

    public class InMemoryEventBus : IEventBus
    {
        private readonly Dictionary<Type, List<Func<object, Task>>> _handlers = new();
        public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
        {
            var eventType = typeof(T);
            if (_handlers.TryGetValue(eventType, out var handlers))
            {
                var tasks = handlers.Select(handler => handler(@event));
                return Task.WhenAll(tasks);
            }
            return Task.CompletedTask;
        }
        public Task SubscribeAsync<T>(Func<T, Task> handler, CancellationToken cancellationToken = default) where T : class
        {
            var eventType = typeof(T);
            if (!_handlers.ContainsKey(eventType))
            {
                _handlers[eventType] = new List<Func<object, Task>>();
            }
            _handlers[eventType].Add(e => handler((T)e));
            return Task.CompletedTask;
        }
    }

    public class RabbitMqEventBus : IEventBus, IDisposable
    {
        private readonly string _connectionString;
        private IBus? _bus;
        private readonly ILogger<RabbitMqEventBus> _logger;
        private readonly SemaphoreSlim _connectionLock = new(1, 1);

        public RabbitMqEventBus(string connectionString, ILogger<RabbitMqEventBus> logger)
        {
            _connectionString = connectionString;
            _logger = logger;
        }
        private async Task<bool> EnsureConnectedAsync()
        {
            if (_bus != null)
                return true;
            await _connectionLock.WaitAsync();
            try
            {
                if (_bus != null)
                    return true;

                int retryCount = 0;
                while (_bus == null)
                {
                    try
                    {
                        _bus = RabbitHutch.CreateBus(_connectionString);
                        _logger.LogInformation("Connected to RabbitMQ successfully.");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        retryCount++;
                        _logger.LogError(ex, $"Failed to connect to RabbitMQ. Attempt {retryCount}");
                        if (retryCount >= 5)
                            break; // Stop retrying after 5 attempts
                        await Task.Delay(TimeSpan.FromSeconds(Math.Min(5 * retryCount, 30)));
                    }
                }
                return false;
            }
            finally
            {
                _connectionLock.Release();
            }
        }
        public RabbitMqEventBus(string connectionString)
        {
            _bus = RabbitHutch.CreateBus(connectionString);
        }
        public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
        {
            // return _bus.PubSub.PublishAsync(@event, cancellationToken);
            if (!await EnsureConnectedAsync())
            {
                _logger.LogWarning("Publish failed: RabbitMQ connection is not available.");
                return; // or optionally throw or buffer event for later retry
            }
            await _bus!.PubSub.PublishAsync(@event, cancellationToken);
        }
        public async Task SubscribeAsync<T>(Func<T, Task> handler, CancellationToken cancellationToken = default) where T : class
        {
            // return _bus.PubSub.SubscribeAsync(Guid.NewGuid().ToString(), handler, cancellationToken);
            if (!await EnsureConnectedAsync())
            {
                _logger.LogWarning("Subscribe failed: RabbitMQ connection is not available.");
                return; // or throw, depending on your app needs
            }
            await _bus!.PubSub.SubscribeAsync(Guid.NewGuid().ToString(), handler, cancellationToken);
        }
        public void Dispose()
        {
            _bus?.Dispose();
        }
    }
}


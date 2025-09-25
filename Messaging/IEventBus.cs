using RabbitMQ.Client;
using EasyNetQ;
namespace Intellimix_Template.Messaging
{

    public interface IEventBus
    {
        Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
            where T : class;

        Task SubscribeAsync<T>(Func<T, Task> handler, CancellationToken cancellationToken = default)
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
        private readonly IBus _bus;
        public RabbitMqEventBus(string connectionString)
        {
            _bus = RabbitHutch.CreateBus(connectionString);
        }
        public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
        {
            return _bus.PubSub.PublishAsync(@event, cancellationToken);
        }
        public Task SubscribeAsync<T>(Func<T, Task> handler, CancellationToken cancellationToken = default) where T : class
        {
            return _bus.PubSub.SubscribeAsync(Guid.NewGuid().ToString(), handler, cancellationToken);
        }
        public void Dispose()
        {
            _bus.Dispose();
        }
    }
}


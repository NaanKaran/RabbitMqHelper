using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMqHelper.Config;
using RabbitMqHelper.Interface;
using System.Text;

namespace RabbitMqHelper.Producer
{
    public class RabbitMqProducer : IRabbitMqProducer, IDisposable
    {
        private readonly RabbitMqConfig _config;
        private readonly ConnectionFactory _factory;
        private IConnection? _connection;
        private IChannel? _channel;
        private bool _disposed;
        private BasicProperties _basicProperties;

        public RabbitMqProducer(IOptions<RabbitMqConfig> config)
        {
            _config = config.Value;
            _factory = new ConnectionFactory
            {
                HostName = _config.HostName,
                Port = _config.Port,
                UserName = _config.UserName,
                Password = _config.Password,
                VirtualHost = _config.VirtualHost
            };
            _basicProperties = new BasicProperties
            {
                Persistent = true,
                MessageId = Guid.NewGuid().ToString(),
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                ContentType = "application/json",
                CorrelationId = Guid.NewGuid().ToString()
            };

        }

        private async Task EnsureConnectionAsync()
        {
            if (_connection is null || !_connection.IsOpen)
                _connection = await _factory.CreateConnectionAsync();

            if (_channel is null || !_channel.IsOpen)
                _channel = await _connection.CreateChannelAsync();
        }

        private static byte[] EncodeMessage(string message) => Encoding.UTF8.GetBytes(message);

        public async Task PublishMessageToQueueAsync(string queueName, string message, bool durable = true, bool exclusive = false, bool autoDelete = false)
        {
            await EnsureConnectionAsync();
            // Create message properties and set persistent = true

            await _channel!.QueueDeclareAsync(queueName, durable, exclusive, autoDelete);


            var body = EncodeMessage(message);

            await _channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queueName,
                mandatory: false,
                basicProperties: _basicProperties,
                body: body
            );
        }

        public async Task PublishMessagesWithBatchAsync(string queueName, IEnumerable<string> messages, int batchSize = 100, bool durable = true, bool exclusive = false, bool autoDelete = false, CancellationToken cancellationToken = default)
        {
            if (messages is null) throw new ArgumentNullException(nameof(messages));

            await EnsureConnectionAsync();
            await _channel!.QueueDeclareAsync(queueName, durable, exclusive, autoDelete, cancellationToken: cancellationToken);

            foreach (var batch in messages.Chunk(batchSize))
            {
                foreach (var message in batch)
                {    
                    await _channel.BasicPublishAsync(string.Empty, queueName, EncodeMessage(message), cancellationToken: cancellationToken);
                }
            }
        }

        public async Task PublishMessageToExchangeAsync(string exchangeName, string routingKey, string message, bool durable = true)
        {
            await EnsureConnectionAsync();

            await _channel!.ExchangeDeclareAsync(exchangeName, ExchangeType.Direct, durable);
            await _channel!.BasicPublishAsync(exchangeName, routingKey, mandatory: false, basicProperties: _basicProperties, EncodeMessage(message));
        }

        public async Task PublishMessageToDeadLetterQueueAsync(string queueName, string message, string? deadLetterExchange = null, string? deadLetterRoutingKey = null, bool durable = true, bool exclusive = false, bool autoDelete = false)
        {
            await EnsureConnectionAsync();

            var arguments = new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = deadLetterExchange,
                ["x-dead-letter-routing-key"] = deadLetterRoutingKey
            };

            await _channel!.QueueDeclareAsync($"{queueName}.DLQ", durable, exclusive, autoDelete, arguments);
            await _channel.BasicPublishAsync(string.Empty, queueName, mandatory: false,  basicProperties: _basicProperties, EncodeMessage(message));
        }

        public async Task PublishTopicMessageAsync(string exchangeName, string routingKey, string message, bool durable = true)
        {
            await EnsureConnectionAsync();

            await _channel!.ExchangeDeclareAsync(exchangeName, ExchangeType.Topic, durable);
            await _channel!.BasicPublishAsync(exchangeName, routingKey, mandatory: false, basicProperties: _basicProperties, EncodeMessage(message));
        }

        public async Task PublishFanoutMessageAsync(string exchangeName, string message, bool durable = true)
        {
            await EnsureConnectionAsync();

            await _channel!.ExchangeDeclareAsync(exchangeName, ExchangeType.Fanout, durable);
            await _channel!.BasicPublishAsync(exchangeName, string.Empty, mandatory: false, basicProperties: _basicProperties, EncodeMessage(message));
        }

        public async Task PublishHeaderMessageAsync(string exchangeName, string routingKey, string message, IDictionary<string, object> headers, bool durable = true)
        {
            await EnsureConnectionAsync();

            await _channel!.ExchangeDeclareAsync(exchangeName, ExchangeType.Headers, durable);

            await _channel!.BasicPublishAsync(exchangeName, routingKey, mandatory: false, basicProperties: _basicProperties, EncodeMessage(message));
        }

        public async Task BindQueueToExchangeAsync(string queueName, string exchangeName, string routingKey)
        {
            await EnsureConnectionAsync();
            await _channel!.QueueBindAsync(queueName, exchangeName, routingKey);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _channel?.Dispose();
                _connection?.Dispose();
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}

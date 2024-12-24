using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMqHelper.Config;
using System.Text;

namespace RabbitMqHelper.Producer
{
    public class RabbitMqProducer : IDisposable
    {
        private readonly RabbitMqConfig _config;
        private readonly ConnectionFactory _factory;
        private IConnection _connection;
        private IChannel _channel;
        private bool _disposed;

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
            _connection = _factory.CreateConnectionAsync().Result;
            _channel = _connection.CreateChannelAsync().Result;
        }

        private async Task EnsureConnectionAsync()
        {
            if (_connection == null || !_connection.IsOpen)
            {
                _connection = await _factory.CreateConnectionAsync();
            }

            if (_channel == null || !_channel.IsOpen)
            {
                _channel = await _connection.CreateChannelAsync();
            }
        }


        /// <summary>
        /// Publish a message to a queue
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="message"></param>
        /// <param name="durable"></param>
        /// <param name="exclusive"></param>
        /// <param name="autoDelete"></param>
        /// <returns></returns>
        public async Task PublishMessageToQueueAsync(string queueName, string message, bool durable = true, bool exclusive = false, bool autoDelete = false)
        {
            await EnsureConnectionAsync();
            // Declare the queue
            await _channel.QueueDeclareAsync(
                queue: queueName,
                durable: durable,
                exclusive: exclusive,
                autoDelete: autoDelete,
                arguments: null);

            var body = Encoding.UTF8.GetBytes(message);

            // Publish the message
            await _channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queueName,
                body: body);

            Console.WriteLine($"[x] Sent: {message}");
        }

        /// <summary>
        /// Publish a message to an exchange
        /// </summary>
        /// <param name="exchangeName"></param>
        /// <param name="routingKey"></param>
        /// <param name="message"></param>
        /// <param name="durable"></param>
        /// <returns></returns>
        public async Task PublishMessageToExchangeAsync(string exchangeName, string routingKey, string message, bool durable = true)
        {
            await EnsureConnectionAsync();
            // Declare the exchange
            await _channel.ExchangeDeclareAsync(
                exchange: exchangeName,
                type: ExchangeType.Direct,
                durable: durable,
                autoDelete: false,
                arguments: null);

            var body = Encoding.UTF8.GetBytes(message);

            // Publish the message
            await _channel.BasicPublishAsync(
                exchange: exchangeName,
                routingKey: routingKey,
                body: body);

            Console.WriteLine($"[x] Sent: {message}");
        }
        /// <summary>
        /// Publish a message to a dead letter queue
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="message"></param>
        /// <param name="deadLetterExchange"></param>
        /// <param name="deadLetterRoutingKey"></param>
        /// <param name="durable"></param>
        /// <param name="exclusive"></param>
        /// <param name="autoDelete"></param>
        /// <returns></returns>
        public async Task PublishMessageToDeadLetterQueueAsync(string queueName, string message, string? deadLetterExchange = null, string? deadLetterRoutingKey = null, bool durable = true, bool exclusive = false, bool autoDelete = false)
        {
            await EnsureConnectionAsync();
            // Declare the queue
            await _channel.QueueDeclareAsync(
                queue: queueName + "-dead-letter",
                durable: durable,
                exclusive: exclusive,
                autoDelete: autoDelete,
                arguments: new Dictionary<string, object>
                {
                    { "x-dead-letter-exchange", deadLetterExchange },
                    { "x-dead-letter-routing-key", deadLetterRoutingKey }
                });

            var body = Encoding.UTF8.GetBytes(message);

            // Publish the message
            await _channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queueName,
                body: body);

            Console.WriteLine($"[x] Sent: {message}");
        }

        public async Task PublishTopicMessageAsync(string exchangeName, string routingKey, string message, bool durable = true)
        {
            await EnsureConnectionAsync();
            // Declare the exchange
            await _channel.ExchangeDeclareAsync(
                exchange: exchangeName,
                type: ExchangeType.Topic,
                durable: durable,
                autoDelete: false,
                arguments: null);

            var body = Encoding.UTF8.GetBytes(message);

            // Publish the message
            await _channel.BasicPublishAsync(
                exchange: exchangeName,
                routingKey: routingKey,
                body: body);

            Console.WriteLine($"[x] Sent: {message}");
        }

        /// <summary>
        /// Publish a message to a fanout exchange
        /// </summary>
        /// <param name="exchangeName"></param>
        /// <param name="message"></param>
        /// <param name="durable"></param>
        /// <returns></returns>
        public async Task PublishFanoutMessageAsync(string exchangeName, string message, bool durable = true)
        {
            await EnsureConnectionAsync();
            // Declare the exchange
            await _channel.ExchangeDeclareAsync(
                exchange: exchangeName,
                type: ExchangeType.Fanout,
                durable: durable,
                autoDelete: false,
                arguments: null);

            var body = Encoding.UTF8.GetBytes(message);

            // Publish the message
            await _channel.BasicPublishAsync(
                exchange: exchangeName,
                routingKey: string.Empty,
                body: body);

            Console.WriteLine($"[x] Sent: {message}");
        }

        /// <summary>
        /// Publish a message to a headers exchange
        /// </summary>
        /// <param name="exchangeName"></param>
        /// <param name="routingKey"></param>
        /// <param name="message"></param>
        /// <param name="headers"></param>
        /// <param name="durable"></param>
        /// <returns></returns>
        public async Task PublishHeaderMessageAsync(string exchangeName, string routingKey, string message, IDictionary<string, object> headers, bool durable = true)
        {
            await EnsureConnectionAsync();
            // Declare the exchange
            await _channel.ExchangeDeclareAsync(
                exchange: exchangeName,
                type: ExchangeType.Headers,
                durable: durable,
                autoDelete: false,
                arguments: null);

            var body = Encoding.UTF8.GetBytes(message);

            // Publish the message
            await _channel.BasicPublishAsync(
                exchange: exchangeName,
                routingKey: routingKey,
                body: body);

            Console.WriteLine($"[x] Sent: {message}");
        }

        /// <summary>
        /// Bind a queue to an exchange
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="exchangeName"></param>
        /// <param name="routingKey"></param>
        /// <returns></returns>
        public async Task BindQueueToExchangeAsync(string queueName, string exchangeName, string routingKey)
        {
            await EnsureConnectionAsync();
            await _channel.QueueBindAsync(queueName, exchangeName, routingKey);
        }





        public void Dispose()
        {
            if (_disposed) return;

            _channel?.Dispose();
            _connection?.Dispose();
            _disposed = true;
        }
    }
}

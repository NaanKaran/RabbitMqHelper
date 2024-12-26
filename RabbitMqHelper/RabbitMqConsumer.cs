using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using RabbitMqHelper.Config;
using System.Text;
using Microsoft.Extensions.Options;
using System.Drawing;
using RabbitMqHelper.Interface;

namespace RabbitMqHelper.Consumer
{
    public class RabbitMqConsumer : IRabbitMqConsumer, IDisposable
    {
        private readonly ConnectionFactory _factory;
        private IConnection _connection;
        private IChannel _channel;
        private bool _disposed;
        private readonly RabbitMqConfig _config;

        public RabbitMqConsumer(IOptions<RabbitMqConfig> config)
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

        public async Task<IChannel> GetChannelAsync(string queueName, bool durable = true, bool exclusive = false, bool autoDelete = false)
        {
            await EnsureConnectionAsync();

            // Declare the queue
            await _channel.QueueDeclareAsync(
                queue: queueName,
                durable: durable,
                exclusive: exclusive,
                autoDelete: autoDelete,
                arguments: null);

            return _channel;

        }


        public async Task<IChannel> GetChannelExchangeAsync(string exchangeName, string queueName, string routingKey, bool durable = true, bool autoDelete = false)
        {
            await EnsureConnectionAsync();
            // Declare the exchange
            await _channel.ExchangeDeclareAsync(exchange: exchangeName, type: ExchangeType.Direct, durable: durable, autoDelete: autoDelete, arguments: null);

            // Declare the queue
            await _channel.QueueDeclareAsync(
                queue: queueName,
                durable: durable,
                exclusive: false,
                autoDelete: autoDelete,
                arguments: null);

            // Bind the queue to the exchange
            await _channel.QueueBindAsync(queue: queueName, exchange: exchangeName, routingKey: routingKey);

            return _channel;
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

using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using RabbitMqHelper.Config;
using System.Text;
using Microsoft.Extensions.Options;
using System.Drawing;
using RabbitMqHelper.Interface;
using System.Net.Http.Headers;
using System.Text.Json;

namespace RabbitMqHelper.Consumer
{
    public class RabbitMqConsumer : IRabbitMqConsumer, IDisposable
    {
        private readonly ConnectionFactory _factory;
        private IConnection _connection;
        private IChannel _channel;
        private bool _disposed;
        private readonly RabbitMqConfig _config;
        private readonly IRabbitMqProducer _rabbitMqProducer;


        public RabbitMqConsumer(IOptions<RabbitMqConfig> config, IRabbitMqProducer rabbitMqProducer)
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
            _rabbitMqProducer = rabbitMqProducer;
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
        /// <summary>
        /// ConsumeAsync
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="messageHandler"></param>
        /// <returns></returns>
        public async Task ConsumeAsync(string queueName, Func<string, Task> messageHandler)
        {
            await EnsureConnectionAsync();
            var channel = await GetChannelAsync(queueName);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                string message = "";
                try
                {
                    var body = ea.Body.ToArray();
                    message = Encoding.UTF8.GetString(body);

                    // Invoke the provided message handler
                    await messageHandler(message);

                    // Acknowledge message after successful processing
                    await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($" [!] Error processing message: {ex.Message}");

                    // Requeue the message or handle it based on your requirements
                    await _rabbitMqProducer.PublishMessageToDeadLetterQueueAsync(queueName, message);
                    await channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                }
            };

            // Start consuming from the queue
            await channel.BasicConsumeAsync(queueName, autoAck: false, consumer: consumer);
        }


        /// <summary>
        /// GetChannelExchangeAsync
        /// </summary>
        /// <param name="exchangeName"></param>
        /// <param name="queueName"></param>
        /// <param name="routingKey"></param>
        /// <param name="durable"></param>
        /// <param name="autoDelete"></param>
        /// <returns></returns>
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
        /// <summary>
        /// GetAllQueuesAsync
        /// </summary>
        /// <returns></returns>
        public async Task<List<string>> GetAllQueuesAsync()
        {
            var queues = new List<string>();
            try
            {
                using var httpClient = new HttpClient();

                var uri = new Uri($"http://{_config.HostName}:15672/api/queues");
                var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_config.UserName}:{_config.Password}"));

                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);

                var response = await httpClient.GetAsync(uri);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var queueList = JsonSerializer.Deserialize<List<RabbitMqQueue>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                queues = queueList?.Select(q => q.Name).ToList() ?? new List<string>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching queues: {ex.Message}");
            }

            return queues;
        }
        /// <summary>
        /// GetMessageCountAsync
        /// </summary>
        /// <param name="queueName"></param>
        /// <returns></returns>
        public async Task<int> GetMessageCountAsync(string queueName)
        {
            try
            {
                using var httpClient = new HttpClient();

                var uri = new Uri($"http://{_config.HostName}:15672/api/queues/%2F/{queueName}");
                var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_config.UserName}:{_config.Password}"));

                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);

                var response = await httpClient.GetAsync(uri);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var queueInfo = JsonSerializer.Deserialize<RabbitMqQueue>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return queueInfo?.Messages ?? 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking messages: {ex.Message}");
                return -1;
            }
        }

        // Method to get the first message without acknowledging it

        /// <summary>
        /// GetFirstMessageWithoutAcknowledgingAsync
        /// </summary>
        /// <param name="queueName"></param>
        /// <returns></returns>
        public async Task<string> GetFirstMessageWithoutAcknowledgingAsync(string queueName)
        {
            try
            {
                await EnsureConnectionAsync();
                var channel = await GetChannelAsync(queueName);

                // Get the first message from the queue without acknowledging it
                var result = await channel.BasicGetAsync(queueName, autoAck: false);

                if (result != null)
                {
                    // Return the message body as string
                    return Encoding.UTF8.GetString(result.Body.ToArray());
                }
                else
                {
                    return "No message available in the queue.";
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
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

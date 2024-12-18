using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using RabbitMqHelper.Config;
using System.Text;
using Microsoft.Extensions.Options;

namespace RabbitMqHelper.Consumer
{
    public class RabbitMqConsumer
    {
        private readonly RabbitMqConfig _config;

        public RabbitMqConsumer(IOptions<RabbitMqConfig> config)
        {
            _config = config.Value;
        }

        public async Task ConsumeMessageAsync(string queueName, Func<string, Task> onMessageReceived)
        {
            var factory = new ConnectionFactory
            {
                HostName = _config.HostName,
                Port = _config.Port,
                UserName = _config.UserName,
                Password = _config.Password
            };

            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                await onMessageReceived(message);
            };

            await channel.BasicConsumeAsync(queue: queueName, autoAck: true, consumer: consumer);

            Console.WriteLine($"Consuming messages from queue '{queueName}'...");
            Console.WriteLine("Press [Enter] to stop.");
            Console.ReadLine();
        }
    }
}

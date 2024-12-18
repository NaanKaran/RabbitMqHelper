using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMqHelper.Config;
using System.Text;

namespace RabbitMqHelper.Producer
{
    public class RabbitMqProducer
    {
        private readonly RabbitMqConfig _config;

        public RabbitMqProducer(IOptions<RabbitMqConfig> config)
        {
            _config = config.Value;
        }

        public async Task PublishMessageAsync(string exchange, string routingKey, string message)
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
            var body = Encoding.UTF8.GetBytes(message);

            await channel.BasicPublishAsync(exchange: exchange, routingKey: routingKey, body: body);
        }
    }
}

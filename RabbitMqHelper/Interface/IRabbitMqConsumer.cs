using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMqHelper.Interface
{
    public interface IRabbitMqConsumer
    {
        Task<IChannel> GetChannelAsync(string queueName, bool durable = true, bool exclusive = false, bool autoDelete = false);
        Task<IChannel> GetChannelExchangeAsync(string exchangeName, string queueName, string routingKey, bool durable = true, bool autoDelete = false);

        Task ConsumeAsync(string queueName, Func<string, Task> messageHandler);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMqHelper.Interface
{
    public interface IRabbitMqProducer
    {
        Task BindQueueToExchangeAsync(string queueName, string exchangeName, string routingKey);
        Task PublishFanoutMessageAsync(string exchangeName, string message, bool durable = true);
        Task PublishHeaderMessageAsync(string exchangeName, string routingKey, string message, IDictionary<string, object> headers, bool durable = true);
        Task PublishMessageToDeadLetterQueueAsync(string queueName, string message, string? deadLetterExchange = null, string? deadLetterRoutingKey = null, bool durable = true, bool exclusive = false, bool autoDelete = false);
        Task PublishMessageToExchangeAsync(string exchangeName, string routingKey, string message, bool durable = true);
        Task PublishMessageToQueueAsync(string queueName, string message, bool durable = true, bool exclusive = false, bool autoDelete = false);
        Task PublishMessagesWithBatchAsync(
                            string queueName,
                            IEnumerable<string> messages,
                            int batchSize = 100,
                            bool durable = true,
                            bool exclusive = false,
                            bool autoDelete = false,
                            CancellationToken cancellationToken = default);
        Task PublishTopicMessageAsync(string exchangeName, string routingKey, string message, bool durable = true);
    }
}

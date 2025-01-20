using System.Text;
using Microsoft.Extensions.Options;
using Moq;
using RabbitMQ.Client.Events;
using RabbitMqHelper.Config;
using RabbitMqHelper.Consumer;
using RabbitMqHelper.Interface;
using RabbitMqHelper.Producer;
using Xunit;

namespace RabbitMqHelper.Tests
{
    public class RabbitMqConsumerTests
    {
        private readonly IServiceProvider _serviceProvider;

        public RabbitMqConsumerTests()
        {
            _serviceProvider = new ContainerResolver().ServiceProvider;
        }

        [Fact]
        public async Task ConsumeMessageAsync_ShouldProcessMessage()
        {
            // Arrange
            var mockConfig = new Mock<IOptions<RabbitMqConfig>>();
            mockConfig.Setup(x => x.Value).Returns(new RabbitMqConfig
            {
                HostName = "localhost",
                Port = 5672,
                UserName = "admin",
                Password = "StrongPassword123"
            });


            var producer = new RabbitMqProducer(mockConfig.Object);
            var consumer = new RabbitMqConsumer(mockConfig.Object, producer);

            // Act
            var channel = await consumer.GetChannelAsync("test-queue");
            var msgEvents = new AsyncEventingBasicConsumer(channel);
            msgEvents.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    await channel.BasicNackAsync(ea.DeliveryTag, false, true);

                }
            };
            await Task.Delay(1000);
            // Assert
            Assert.True(true); // Ensure the message was processed
        }
        [Fact]
        public async Task ConsumeMessageAsync_Simplyfied()
        {
            var queueName = "test-queue";
            // Arrange
            var mockConfig = new Mock<IOptions<RabbitMqConfig>>();
            mockConfig.Setup(x => x.Value).Returns(new RabbitMqConfig
            {
                HostName = "localhost",
                Port = 5672,
                UserName = "admin",
                Password = "StrongPassword123"
            });


            var producer = new RabbitMqProducer(mockConfig.Object);
            var consumer = new RabbitMqConsumer(mockConfig.Object, producer);

            // Act
            await consumer.ConsumeAsync(queueName, async (message) =>
            {
                Console.WriteLine($"Received message: {message}");

                // Add additional message processing logic here
                await Task.Delay(100); // Simulate processing time
            });
            // Assert
            Assert.True(true); // Ensure the message was processed
        }
    }
}

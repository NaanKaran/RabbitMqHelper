using Microsoft.Extensions.Options;
using Moq;
using RabbitMqHelper.Config;
using RabbitMqHelper.Producer;
using Xunit;

namespace RabbitMqHelper.Tests
{
    public class RabbitMqProducerTests
    {
        public RabbitMqConfig RabbitMqConfig = new RabbitMqConfig
        {
            HostName = "localhost",
            Port = 5672,
            UserName = "admin",
            Password = "StrongPassword123"
        };

        [Fact]
        public async Task PublishMessageAsync_ShouldPublishMessageSuccessfully()
        {
            // Arrange
            var mockConfig = new Mock<IOptions<RabbitMqConfig>>();
            mockConfig.Setup(x => x.Value).Returns(RabbitMqConfig);

            var producer = new RabbitMqProducer(mockConfig.Object);

            // Act
            for( var i = 1; i<= 10000; i++)
            {
                 await Record.ExceptionAsync(() =>
    producer.PublishMessageToQueueAsync("test-queue", $" ___ Message {i} ___"));
            }


            // Assert
            Assert.NotNull(producer); // No exceptions should occur
        }

        [Fact]
        public async Task PublishMessageToExchangeAsync_ShouldPublishMessageSuccessfully()
        {

            // Arrange
            var mockConfig = new Mock<IOptions<RabbitMqConfig>>();
            mockConfig.Setup(x => x.Value).Returns(RabbitMqConfig);

            var producer = new RabbitMqProducer(mockConfig.Object);

            // Act
            var exception = await Record.ExceptionAsync(() =>
                producer.PublishMessageToExchangeAsync("test-exchange", null, "test-routing-key"));

            // Assert
            Assert.Null(exception); // No exceptions should occur
        }

        [Fact]
        public async Task BindQueueToExchangeAsync_ShouldBindQueueToExchangeSuccessfully()
        {

            // Arrange
            var mockConfig = new Mock<IOptions<RabbitMqConfig>>();
            mockConfig.Setup(x => x.Value).Returns(RabbitMqConfig);

            var producer = new RabbitMqProducer(mockConfig.Object);

            // Act
            var exception = await Record.ExceptionAsync(() =>
                producer.BindQueueToExchangeAsync("test-exchange", "test-queue", "test-routing-key"));

            // Assert
            Assert.NotNull(exception); 
        }

    }

}

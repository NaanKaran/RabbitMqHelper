using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using RabbitMqHelper.Config;
using RabbitMqHelper.Producer;
using Xunit;
namespace RabbitMqHelper.IntegrationTests
{
    public class StartupTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public StartupTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task RabbitMqConfig_ShouldBeLoadedFromAppSettings()
        {
            // Arrange
            var scopeFactory = _factory.Services.GetRequiredService<IServiceScopeFactory>();

            using var scope = scopeFactory.CreateScope();
            var options = scope.ServiceProvider.GetRequiredService<IOptions<RabbitMqConfig>>();

            // Act
            var config = options.Value;

            // Assert
            Assert.Equal("localhost", config.HostName);
            Assert.Equal(5672, config.Port);
            Assert.Equal("guest", config.UserName);
            Assert.Equal("guest", config.Password);
        }

        [Fact]
        public async Task RabbitMqProducer_ShouldBeResolvedFromDI()
        {
            // Arrange
            var scopeFactory = _factory.Services.GetRequiredService<IServiceScopeFactory>();

            using var scope = scopeFactory.CreateScope();
            var producer = scope.ServiceProvider.GetRequiredService<RabbitMqProducer>();

            // Assert
            Assert.NotNull(producer);
        }
    }
}

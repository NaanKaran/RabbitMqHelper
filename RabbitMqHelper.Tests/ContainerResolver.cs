using Microsoft.Extensions.Hosting;
using RabbitMqHelper.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMqHelper.Consumer;
using RabbitMqHelper.Producer;


public class ContainerResolver  // Change this to public
{

    public IServiceProvider ServiceProvider
    {
        get; set;
    }

    public ContainerResolver()
    {

        var builder = Host.CreateDefaultBuilder();

        builder.ConfigureAppConfiguration((hostingContext, config) =>
        {
            config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();
        });
        builder.ConfigureServices((cxt, services) =>
        {
            // Add RabbitMQ configuration
            services.Configure<RabbitMqConfig>(cxt.Configuration.GetSection("RabbitMqConfig"));

            // Register RabbitMQ producer and consumer as singleton services
            services.AddSingleton<RabbitMqProducer>();
            services.AddSingleton<RabbitMqConsumer>();
        });

       
        ServiceProvider = builder.Build().Services;
    }

    
}
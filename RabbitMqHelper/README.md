# RabbitMQ Connection Helper

A .NET NuGet package to simplify working with RabbitMQ for producing and consuming messages. This package provides an easy-to-use interface for managing connections, exchanges, queues, and publishing/consuming messages in a RabbitMQ environment.

## Features

- Asynchronous support for RabbitMQ connections.
- Simplified message publishing and consumption.
- Built-in support for durable queues and exchanges.
- Helper methods for creating producers and consumers.

## Installation

Install the package via NuGet Package Manager:

```bash
Install-Package RabbitMQ.Connect.Helper
```

Or via the .NET CLI:

```bash
dotnet add package RabbitMQ.Connect.Helper
```

## Usage

### 1. Add Appsetings.json

```appsettings.json
    "RabbitMqConfig": {
      "HostName": "localhost",
      "Port": 5672,
      "UserName": "guest",
      "Password": "guest"
    }
```

### 2. Setup RabbitMQ Connection -- add this to your program/startup file


```csharp
        services.Configure<RabbitMqConfig>(cxt.Configuration.GetSection("RabbitMqConfig"));
        services.AddSingleton<IRabbitMqConsumer, RabbitMqConsumer>();
        services.AddSingleton<IRabbitMqProducer, RabbitMqProducer>();
```

### 3. Publish Messages

```csharp


    private readonly IRabbitMqConsumer _rabbitMqConsumer;
    private readonly IRabbitMqProducer _rabbitMqProducer;

    public YourClass(IRabbitMqConsumer rabbitMqConsumer, IRabbitMqProducer rabbitMqProducer)
    {
        _rabbitMqConsumer = rabbitMqConsumer;
        _rabbitMqProducer = rabbitMqProducer;
    }


    public ProducerExample(){
    var queueName = "example-queue";
    var message = "Hello, RabbitMQ!";
         await  _rabbitMqProducer.PublishMessageToQueueAsync(queueName, message); 
    }
```

### 4. Consume Messages

```csharp
          var queueName = "example-queue";

          var channel = await _rabbitMqConsumer.GetChannelAsync(queueName);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                string message = "";
                try
                {
                    var body = ea.Body.ToArray();
                    message = Encoding.UTF8.GetString(body);
                    Console.WriteLine($" [x] Received {message}");
                   // await _processFunction.QueueFunction(message);
                    await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($" [!] Error processing message: {ex.Message}");
                    await  _rabbitMqProducer.PublishMessageToQueueAsync(queueName, message);
                    // Optionally reject the message, don't requeue, since it's already in DLQ
                    await channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                }
            };

            // Start consuming from the queue
            await channel.BasicConsumeAsync(_queueName, autoAck: false, consumer: consumer);

            // Keep the service alive
            await Task.CompletedTask;
```



## Configuration

- **Exchange Type**: Supports `direct`, `topic`, `fanout`, and `headers`.
- **Durability**: Ensures exchanges and queues are durable for reliable messaging.
- **Auto Acknowledge**: Messages are acknowledged automatically or manually.

## Contributing

Contributions are welcome! If you find a bug or want to add a feature, feel free to open an issue or submit a pull request.

## License

This package is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Contact

For any issues or questions, feel free to reach out to the maintainer.


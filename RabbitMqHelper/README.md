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
Install-Package ConnectRabbitMq
```

Or via the .NET CLI:

```bash
dotnet add package ConnectRabbitMq
```

## Usage

### 1. Setup RabbitMQ Connection

```csharp
var factory = new ConnectionFactory
{
    HostName = "localhost",
    UserName = "guest",
    Password = "guest",
};

// Create a connection and a channel
using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();
```

### 2. Publish Messages

```csharp
var exchangeName = "example-exchange";
var routingKey = "example-key";
var message = "Hello, RabbitMQ!";

await RabbitMQHelper.PublishMessageAsync(channel, exchangeName, routingKey, message);
```

### 3. Consume Messages

```csharp
var queueName = "example-queue";

await RabbitMQHelper.ConsumeMessagesAsync(channel, queueName, async (message) =>
{
    Console.WriteLine($"Received: {message}");
    // Process the message
});
```

### 4. Helper Methods

#### PublishMessageAsync
Publishes a message to a specific exchange with a routing key.

```csharp
await RabbitMQHelper.PublishMessageAsync(channel, exchangeName, routingKey, message);
```

#### ConsumeMessagesAsync
Starts consuming messages from a queue.

```csharp
await RabbitMQHelper.ConsumeMessagesAsync(channel, queueName, messageHandler);
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


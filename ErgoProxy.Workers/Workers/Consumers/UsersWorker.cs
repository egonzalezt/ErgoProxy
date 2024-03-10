namespace ErgoProxy.Workers.Workers.Consumers;

using Domain.SharedKernel.Exceptions;
using Domain.User;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Text.Json;
using Extensions;
using Options;
using Microsoft.Extensions.Options;
using Frieren_Guard;

public class UsersWorker : BaseRabbitMQWorker
{
    private readonly ILogger<UsersWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    public UsersWorker(
        ILogger<UsersWorker> logger,
        ConnectionFactory rabbitConnection,
        IServiceProvider serviceProvider,
        IHealthCheckNotifier healthCheckNotifier,
        SystemStatusMonitor statusMonitor,
        IOptions<QueuesConfiguration> queues
    ) : base(logger, rabbitConnection.CreateConnection(), healthCheckNotifier, statusMonitor, queues.Value.RequestQueues.UserRequestQueue, queues.Value.ReplyQueues.UserReplyQueue)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ProcessMessageAsync(BasicDeliverEventArgs eventArgs, IModel channel)
    {
        var body = eventArgs.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        var headers = eventArgs.BasicProperties.Headers;
        var userId = headers.GetHeaderValue("UserId");
        var operation = headers.GetUserEventType();
        _logger.LogInformation("Processing request for user {userId}", userId);
        switch (operation)
        {
            case UserOperations.CreateUser:
                var createUserDto = JsonSerializer.Deserialize<CreateUserDto>(message) ?? throw new InvalidBodyException();
                await ExecuteUseCaseAsync(createUserDto, channel, userId, operation, eventArgs.DeliveryTag);
                break;
            case UserOperations.UnregisterUser:
                var unregisterUserDto = JsonSerializer.Deserialize<UnRegisterUserDto>(message) ?? throw new InvalidBodyException();
                await ExecuteUseCaseAsync(unregisterUserDto, channel, userId, operation, eventArgs.DeliveryTag);
                break;
            case UserOperations.VerifyUser:
                break;
            default:
                _logger.LogWarning("Not supported Operation: {0}", operation, operation);
                throw new InvalidEventTypeException();
        }
    }

    private async Task ExecuteUseCaseAsync<T>(T body, IModel channel, string userId, UserOperations eventType, ulong deliveryTag) where T : class
    {
        using var scope = _serviceProvider.CreateScope();
        var useCaseSelector = scope.ServiceProvider.GetRequiredService<IUserUseCaseSelector<T>>();
        var result = await useCaseSelector.ExecuteAsync(body);
        string jsonResult = JsonSerializer.Serialize(result);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonResult);
        var headers = new Dictionary<string, object>
                {
                    { "ProcessFailed", false },
                    { "UserId", userId },
                    { "EventType", eventType.ToString() }
                };
        var properties = channel.CreateBasicProperties();
        properties.Headers = headers;
        channel.BasicPublish("", _replyQueueName, properties, jsonBytes);
        channel.BasicAck(deliveryTag, false);
    }
}

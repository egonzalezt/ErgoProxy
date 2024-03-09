namespace ErgoProxy.Workers.Workers;

using HealthChecks;
using Domain.SharedKernel.Exceptions;
using Domain.User;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Text.Json;

public class UsersWorkerV2 : BaseRabbitMQWorker
{
    private readonly ILogger<UsersWorkerV2> _logger;
    private readonly IServiceProvider _serviceProvider;
    public UsersWorkerV2(
        ILogger<UsersWorkerV2> logger,
        IConnection rabbitConnection,
        IServiceProvider serviceProvider,
        IHealthCheckNotifier healthCheckNotifier,
        SystemStatusMonitor statusMonitor
    ) : base(logger, rabbitConnection, healthCheckNotifier, statusMonitor, "users_requests")
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ProcessMessageAsync(BasicDeliverEventArgs eventArgs, IModel channel)
    {
        var body = eventArgs.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        var userId = GetHeaderValue(eventArgs.BasicProperties.Headers, "UserId");
        var operation = GetEventType(eventArgs.BasicProperties.Headers);
        _logger.LogInformation("Processing request for user {userId}", userId);
        switch (operation)
        {
            case UserOperations.CreateUser:
                var createUserDto = JsonSerializer.Deserialize<CreateUserDto>(message) ?? throw new InvalidBodyException();
                await ExecuteUseCaseAsync(createUserDto, channel, userId);
                break;
            case UserOperations.UnregisterUser:
                var unregisterUserDto = JsonSerializer.Deserialize<UnRegisterUserDto>(message) ?? throw new InvalidBodyException();
                await ExecuteUseCaseAsync(unregisterUserDto, channel, userId);
                break;
            case UserOperations.VerifyUser:
                break;
            default:
                _logger.LogWarning("Not supported Operation: {0}", operation);
                throw new InvalidEventTypeException();
        }
    }

    private static UserOperations GetEventType(IDictionary<string, object> headers)
    {
        var key = headers.Keys.FirstOrDefault(k => string.Equals(k, "EventType", StringComparison.OrdinalIgnoreCase)) ?? throw new InvalidEventTypeException();
        if (headers != null &&
                    headers.TryGetValue(key, out object eventTypeHeader))
        {
            var eventType = Encoding.UTF8.GetString((byte[])eventTypeHeader);
            if (!Enum.TryParse(eventType, true, out UserOperations operation))
            {
                throw new InvalidEventTypeException();
            }
            return operation;
        }
        throw new InvalidEventTypeException();
    }

    private static string GetHeaderValue(IDictionary<string, object> headers, string headerKey)
    {
        if (headers != null)
        {
            var key = headers.Keys.FirstOrDefault(k => string.Equals(k, headerKey, StringComparison.OrdinalIgnoreCase)) ?? throw new HeaderNotFoundException();

            if (headers.TryGetValue(key, out object headerValue))
            {
                if (headerValue is byte[] byteArrayValue)
                {
                    return Encoding.UTF8.GetString(byteArrayValue);
                }
                else if (headerValue != null)
                {
                    return headerValue.ToString();
                }
            }
        }

        throw new HeaderNotFoundException();
    }

    private async Task ExecuteUseCaseAsync<T>(T body, IModel channel, string userId) where T : class
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
                };
        var properties = channel.CreateBasicProperties();
        properties.Headers = headers;
        channel.BasicPublish("", "users_responses", properties, jsonBytes);
    }
}

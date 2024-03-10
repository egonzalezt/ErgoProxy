namespace ErgoProxy.Workers.Workers.Consumers;

using HealthChecks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;
using System.Text;
using Domain.Operator;
using Domain.SharedKernel.Exceptions;
using Extensions;
using Microsoft.Extensions.Options;
using Options;

internal class OperatorsWorker : BaseRabbitMQWorker
{
    private readonly ILogger<OperatorsWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    public OperatorsWorker(
        ILogger<OperatorsWorker> logger,
        ConnectionFactory rabbitConnection,
        IServiceProvider serviceProvider,
        IHealthCheckNotifier healthCheckNotifier,
        SystemStatusMonitor statusMonitor,
        IOptions<QueuesConfiguration> queues 
    ) : base(logger, rabbitConnection.CreateConnection(), healthCheckNotifier, statusMonitor, queues.Value.RequestQueues.OperatorRequestQueue, queues.Value.ReplyQueues.OperatorReplyQueue)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ProcessMessageAsync(BasicDeliverEventArgs eventArgs, IModel channel)
    {
        var headers = eventArgs.BasicProperties.Headers;
        var userId = headers.GetHeaderValue("UserId");
        var operation = headers.GetOperatorEventType();
        _logger.LogInformation("Processing request for user {userId}", userId);
        switch (operation)
        {
            case OperatorOperations.GetOperators:
                await ExecuteUseCaseAsync(new GetOperatorsDto(), channel, userId);
                break;
            default:
                _logger.LogWarning("Not supported Operation: {0}", operation);
                throw new InvalidEventTypeException();
        }
    }

    private async Task ExecuteUseCaseAsync<T>(T body, IModel channel, string userId) where T : class
    {
        using var scope = _serviceProvider.CreateScope();
        var useCaseSelector = scope.ServiceProvider.GetRequiredService<IOperatorUseCaseSelector<T>>();
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
        channel.BasicPublish("", _replyQueueName, properties, jsonBytes);
    }
}
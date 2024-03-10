namespace ErgoProxy.Workers.Workers.Consumers;

using Options;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;
using System.Text;
using Domain.Document;
using Domain.SharedKernel.Exceptions;
using Extensions;
using Frieren_Guard;

public class DocumentsWorker : BaseRabbitMQWorker
{
    private readonly ILogger<DocumentsWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    public DocumentsWorker(
        ILogger<DocumentsWorker> logger,
        ConnectionFactory rabbitConnection,
        IServiceProvider serviceProvider,
        IHealthCheckNotifier healthCheckNotifier,
        SystemStatusMonitor statusMonitor,
        IOptions<QueuesConfiguration> queues
    ) : base(logger, rabbitConnection.CreateConnection(), healthCheckNotifier, statusMonitor, queues.Value.RequestQueues.DocumentRequestQueue, queues.Value.ReplyQueues.DocumentReplyQueue)
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
        var operation = headers.GetDocumentEventType();
        _logger.LogInformation("Processing request for user {userId}", userId);
        switch (operation)
        {
            case DocumentOperations.AuthenticateDocument:
                var authenticateDocument = JsonSerializer.Deserialize<AuthenticateDocumentDto>(message) ?? throw new InvalidBodyException();
                await ExecuteUseCaseAsync(authenticateDocument, channel, userId);
                break;
            default:
                _logger.LogWarning("Not supported Operation: {0}", operation);
                throw new InvalidEventTypeException();
        }
    }

    private async Task ExecuteUseCaseAsync<T>(T body, IModel channel, string userId) where T : class
    {
        using var scope = _serviceProvider.CreateScope();
        var useCaseSelector = scope.ServiceProvider.GetRequiredService<IDocumentUseCaseSelector<T>>();
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

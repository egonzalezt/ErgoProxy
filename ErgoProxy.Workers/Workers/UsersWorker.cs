namespace ErgoProxy.Workers.Workers;

using Domain.SharedKernel.Exceptions;
using Domain.User;
using ErgoProxy.HealthChecks;
using ErgoProxy.HealthChecks.Events;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Net;
using System.Text;
using System.Text.Json;

public class UsersWorker : BackgroundService
{
    private readonly ILogger<UsersWorker> _logger;
    private readonly IConnection _rabbitConnection;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHealthCheckNotifier _healthCheckNotifier;
    private readonly SystemStatusMonitor _statusMonitor;
    private readonly IModel _channel;
    private readonly EventingBasicConsumer _consumer;
    private bool _isSystemHealthy = true;
    private bool _subscriptionCancelled = false;
    private string _consumerTag;

    public UsersWorker(
        ILogger<UsersWorker> logger,
        IConnection rabbitConnection,
        IServiceProvider serviceProvider,
        IHealthCheckNotifier healthCheckNotifier,
        SystemStatusMonitor statusMonitor
    )
    {
        _logger = logger;
        _rabbitConnection = rabbitConnection;
        _serviceProvider = serviceProvider;
        _healthCheckNotifier = healthCheckNotifier;
        _statusMonitor = statusMonitor;
        _statusMonitor.SystemStatusChanged += OnSystemStatusChanged;

        _channel = _rabbitConnection.CreateModel();
        _channel.QueueDeclare("users_requests", durable: true, exclusive: false, autoDelete: false, arguments: new Dictionary<string, object>
            {
                { "x-message-ttl", 604800000 }
            });
        _consumer = new EventingBasicConsumer(_channel);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Received += async (sender, eventArgs) =>
        {
            var body = eventArgs.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            try
            {
                var userId = GetHeaderValue(eventArgs.BasicProperties.Headers, "UserId");
                var operation = GetEventType(eventArgs.BasicProperties.Headers);
                _logger.LogInformation("Processing request for user {userId}", userId);
                switch (operation)
                {
                    case UserOperations.CreateUser:
                        var createUserDto = JsonSerializer.Deserialize<CreateUserDto>(message) ?? throw new InvalidBodyException();
                        await ExecuteUseCaseAsync(createUserDto, _channel, userId);
                        break;
                    case UserOperations.UnregisterUser:
                        var unregisterUserDto = JsonSerializer.Deserialize<UnRegisterUserDto>(message) ?? throw new InvalidBodyException();
                        await ExecuteUseCaseAsync(unregisterUserDto, _channel, userId);
                        break;
                    case UserOperations.VerifyUser:
                        break;
                    default:
                        _logger.LogWarning("Not supported Operation: {0}", operation);
                        throw new InvalidEventTypeException();
                }
                _channel.BasicAck(eventArgs.DeliveryTag, false);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error al deserializar JSON: {0}", message);
            }
            catch (InvalidBodyException ex)
            {
                var headers = new Dictionary<string, object>
                {
                    { "ProcessFailed", true }
                };
                var properties = _channel.CreateBasicProperties();
                properties.Headers = headers;

                _channel.BasicPublish("", "users_responses", properties, Encoding.UTF8.GetBytes("Not Valid Body"));
                _logger.LogError(ex, "Invalid Body");
                _channel.BasicAck(eventArgs.DeliveryTag, false);
            }
            catch (InvalidEventTypeException ex)
            {
                var headers = new Dictionary<string, object>
                {
                    { "ProcessFailed", true }
                };
                var properties = _channel.CreateBasicProperties();
                properties.Headers = headers;
                _channel.BasicPublish("", "users_responses", properties, Encoding.UTF8.GetBytes("Not Valid Operation"));
                _logger.LogError(ex, "Invalid EventType");
                _channel.BasicAck(eventArgs.DeliveryTag, false);
            }
            catch (HeaderNotFoundException ex)
            {
                var headers = new Dictionary<string, object>
                {
                    { "ProcessFailed", true }
                };
                var properties = _channel.CreateBasicProperties();
                properties.Headers = headers;
                _channel.BasicPublish("", "users_responses", properties, Encoding.UTF8.GetBytes("Not Valid Operation"));
                _logger.LogError(ex, "Invalid EventType");
                _channel.BasicAck(eventArgs.DeliveryTag, false);
            }
            catch (GovCarpetaApplicationErrorException ex)
            {
                _logger.LogError(ex, "GovCarpeta is not working");
            }
            catch (HttpRequestException ex)
            {
                if (ex.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    _logger.LogError(ex, "Requeue the message because the external service is not available");
                    await _healthCheckNotifier.ReportUnhealthyServiceAsync("GovCarpeta", "The external service is not available", stoppingToken);
                    _channel.BasicNack(eventArgs.DeliveryTag, false, true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing user");
            }
        };

        _consumerTag = _channel.BasicConsume("users_requests", false, _consumer);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
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

    private void OnSystemStatusChanged(object sender, SystemStatusChangedEvent e)
    {
        HealthReport newHealthReport = e.HealthReport;
        _logger.LogInformation("New state of the system: {status}", newHealthReport.Status);

        if (newHealthReport.Status == HealthStatus.Unhealthy && _isSystemHealthy)
        {
            UpdateSubscription(false);
            _isSystemHealthy = false;
        }
        else if (newHealthReport.Status == HealthStatus.Healthy && !_isSystemHealthy)
        {
            UpdateSubscription(true);
            _isSystemHealthy = true;
        }
    }

    private void UpdateSubscription(bool subscribe)
    {
        if (subscribe)
        {
            _logger.LogInformation("Subcribing to the channel users_requests");
            _consumerTag = _channel.BasicConsume("users_requests", false, _consumer);
            _subscriptionCancelled = false;
        }
        else if (!_subscriptionCancelled)
        {
            _logger.LogWarning("Subscription on the channel users_requests cancelled with consumer tag {tag}", _consumerTag);
            _channel.BasicCancel(_consumerTag);
            _consumerTag = string.Empty;
            _subscriptionCancelled = true;
        }

    }

    public override void Dispose()
    {
        GC.SuppressFinalize(this);
        _channel.Close();
        _channel.Dispose();
        base.Dispose();
    }

}

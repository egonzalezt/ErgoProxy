namespace ErgoProxy.Workers.Workers.Consumers;

using Domain.SharedKernel.Exceptions;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net;
using System.Text.Json;
using System.Text;
using Frieren_Guard;
using Frieren_Guard.Events;

public abstract class BaseRabbitMQWorker : BackgroundService
{
    private readonly ILogger _logger;
    private readonly IConnection _rabbitConnection;
    private readonly IHealthCheckNotifier _healthCheckNotifier;
    private readonly SystemStatusMonitor _statusMonitor;
    private readonly IModel _channel;
    private readonly EventingBasicConsumer _consumer;
    private bool _isSystemHealthy = true;
    private bool _subscriptionCancelled = false;
    private string _consumerTag;
    internal readonly string _queueName;
    internal readonly string _replyQueueName;

    public BaseRabbitMQWorker(
        ILogger logger,
        IConnection rabbitConnection,
        IHealthCheckNotifier healthCheckNotifier,
        SystemStatusMonitor statusMonitor,
        string queueName,
        string replyQueue
    )
    {
        _logger = logger;
        _rabbitConnection = rabbitConnection;
        _healthCheckNotifier = healthCheckNotifier;
        _statusMonitor = statusMonitor;
        _statusMonitor.SystemStatusChanged += OnSystemStatusChanged;
        _queueName = queueName;
        _replyQueueName = replyQueue;
        _channel = _rabbitConnection.CreateModel();
        _channel = _rabbitConnection.CreateModel();
        _consumer = new EventingBasicConsumer(_channel);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        await SubscribeAsync(stoppingToken);
    }

    internal async Task SubscribeAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Subscribed to the queue {queue}", _queueName);
        _logger.LogInformation("Reply queue {queue}", _replyQueueName);
        _consumer.Received += async (sender, eventArgs) =>
        {

            try
            {
                _logger.LogInformation("New message received");
                await ProcessMessageAsync(eventArgs, _channel);
            }
            catch (JsonException ex)
            {
                var body = eventArgs.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
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

                _channel.BasicPublish("", _replyQueueName, properties, Encoding.UTF8.GetBytes("Not Valid Body"));
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
                _channel.BasicPublish("", _replyQueueName, properties, Encoding.UTF8.GetBytes("Not Valid Operation"));
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
                _channel.BasicPublish("", _replyQueueName, properties, Encoding.UTF8.GetBytes("Not Valid Operation"));
                _logger.LogError(ex, "Invalid EventType");
                _channel.BasicAck(eventArgs.DeliveryTag, false);
            }
            catch (HttpRequestException ex)
            {
                if (ex.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    _logger.LogError(ex, "Requeue the message because the external service is not available");
                    await _healthCheckNotifier.ReportUnhealthyServiceAsync("GovCarpeta", "The external service is not available", stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing user");
            }
        };

        _consumerTag = _channel.BasicConsume(_queueName, false, _consumer);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    protected abstract Task ProcessMessageAsync(BasicDeliverEventArgs eventArgs, IModel channel);

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
            _logger.LogInformation("Subscribing to the channel {channel}", _queueName);
            _consumerTag = _channel.BasicConsume(_queueName, false, _consumer);
            _subscriptionCancelled = false;
        }
        else if (!_subscriptionCancelled)
        {
            _logger.LogWarning("Subscription on the channel {channel} cancelled with consumer tag {tag}", _queueName, _consumerTag);
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

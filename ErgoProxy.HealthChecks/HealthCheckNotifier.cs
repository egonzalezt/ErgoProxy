namespace ErgoProxy.HealthChecks;

using Microsoft.Extensions.Diagnostics.HealthChecks;

public class HealthCheckNotifier : IHealthCheckNotifier
{
    private readonly IHealthCheckPublisher _publisher;

    public HealthCheckNotifier(IHealthCheckPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task ReportUnhealthyServiceAsync(string serviceName, string message, CancellationToken cancellationToken, Exception? exception = null, IReadOnlyDictionary<string, object>? data = null)
    {
        var entry = new HealthReportEntry(HealthStatus.Unhealthy, message, TimeSpan.Zero, exception, data);
        var unhealthyService = new Dictionary<string, HealthReportEntry> { { serviceName, entry } };
        var report = new HealthReport(unhealthyService, TimeSpan.Zero);
        await _publisher.PublishAsync(report, cancellationToken);
    }
}
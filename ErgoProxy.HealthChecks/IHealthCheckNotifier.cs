namespace ErgoProxy.HealthChecks;

public interface IHealthCheckNotifier
{
    Task ReportUnhealthyServiceAsync(string serviceName, string message, CancellationToken cancellationToken, Exception? exception = null, IReadOnlyDictionary<string, object>? data = null);
}

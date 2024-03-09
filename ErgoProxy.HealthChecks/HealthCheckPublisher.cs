namespace ErgoProxy.HealthChecks;

using Microsoft.Extensions.Diagnostics.HealthChecks;

public class HealthCheckPublisher : IHealthCheckPublisher
{
    private readonly SystemStatusMonitor _statusMonitor;

    public HealthCheckPublisher(SystemStatusMonitor statusMonitor)
    {
        _statusMonitor = statusMonitor;
    }

    public async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        _statusMonitor.UpdateSystemStatus(report);
        await Task.CompletedTask;
    }
}

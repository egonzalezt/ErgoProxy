namespace ErgoProxy.HealthChecks.Events;

using Microsoft.Extensions.Diagnostics.HealthChecks;

public class SystemStatusChangedEvent : EventArgs
{
    public HealthReport HealthReport { get; }

    public SystemStatusChangedEvent(HealthReport healthReport)
    {
        HealthReport = healthReport ?? throw new ArgumentNullException(nameof(healthReport));
    }
}
namespace ErgoProxy.HealthChecks;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using ErgoProxy.HealthChecks.Events;

public class SystemStatusMonitor
{
    public event EventHandler<SystemStatusChangedEvent>? SystemStatusChanged;

    public void UpdateSystemStatus(HealthReport newHealthReport)
    {
        OnSystemStatusChanged(new SystemStatusChangedEvent(newHealthReport));
    }

    protected virtual void OnSystemStatusChanged(SystemStatusChangedEvent e)
    {
        SystemStatusChanged?.Invoke(this, e);
    }
}

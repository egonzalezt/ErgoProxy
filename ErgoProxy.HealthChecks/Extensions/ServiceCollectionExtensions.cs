namespace ErgoProxy.HealthChecks.Extensions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

public static class ServiceCollectionExtensions
{
    public static void AddHealthChecksServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<SystemStatusMonitor>();
        services.AddSingleton<IHealthCheckNotifier, HealthCheckNotifier>();
        services.AddSingleton<IHealthCheckPublisher, HealthCheckPublisher>();
        services.AddSingleton((serviceProvider) =>
        {
            var apiUri = new Uri(configuration["GovCarpeta:HealthChecks"]);
            return new ApiHealthCheck(serviceProvider.GetRequiredService<IHttpClientFactory>(), apiUri);
        });
        var intervalMinutes = configuration.GetValue<int>("HealthChecks:IntervalMinutes");
        services.Configure<HealthCheckPublisherOptions>(options =>
        {
            options.Period = TimeSpan.FromMinutes(intervalMinutes);
        });
        services.AddHealthChecks()
            .AddCheck<ApiHealthCheck>("api_check");
    }
}

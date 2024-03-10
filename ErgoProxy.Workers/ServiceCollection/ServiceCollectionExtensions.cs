namespace ErgoProxy.Workers.ServiceCollection;

using HealthChecks;
using Workers.Consumers.Options;
using RabbitMQ.Client;

public static class ServiceCollectionExtensions
{
    public static void AddWorkerInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ConnectionFactory>(sp =>
        {
            var factory = new ConnectionFactory();
            configuration.GetSection("RabbitMQ:Connection").Bind(factory);
            return factory;
        });
        services.Configure<QueuesConfiguration>(configuration.GetSection("RabbitMQ:Queues"));

        services.AddSingleton((serviceProvider) =>
        {
            var apiUri = new Uri(configuration["GovCarpeta:HealthChecks"]);
            return new ApiHealthCheck(serviceProvider.GetRequiredService<IHttpClientFactory>(), apiUri);
        });

        services.AddHealthChecks()
            .AddCheck<ApiHealthCheck>("api_check");
    }
}

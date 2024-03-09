using ErgoProxy.Workers.Workers.Consumers.Options;
using RabbitMQ.Client;

namespace ErgoProxy.Workers.ServiceCollection;

public static class ServiceCollectionExtensions
{
    public static void AddWorkerInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IConnection>(sp =>
        {
            var factory = new ConnectionFactory();
            configuration.GetSection("RabbitMQ:Connection").Bind(factory);
            return factory.CreateConnection();
        });
        services.AddSingleton<ConnectionFactory>(sp =>
        {
            var factory = new ConnectionFactory();
            configuration.GetSection("RabbitMQ:Connection").Bind(factory);
            return factory;
        });
        services.Configure<QueuesConfiguration>(configuration.GetSection("RabbitMQ:Queues"));

    }
}

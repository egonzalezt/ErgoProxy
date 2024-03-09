namespace ErgoProxy.Infrastructure.ServiceCollection;

using Domain.User;
using UserProcessors;
using Domain.Operator;
using Domain.Document;
using DocumentProcessors;
using OperatorProcessors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient("GovCarpeta", client =>
        {
            client.BaseAddress = new Uri(configuration["GovCarpeta:BaseUrl"]);
        });
        services.AddScoped<IUserUseCaseSelector<CreateUserDto>, CreateUser>();
        services.AddScoped<IUserUseCaseSelector<UnRegisterUserDto>, UnRegisterUser>();
        services.AddScoped<IOperatorUseCaseSelector<GetOperatorsDto>, GetOperators>();
        services.AddScoped<IDocumentUseCaseSelector<AuthenticateDocumentDto>, AuthenticateDocument>();
    }
}
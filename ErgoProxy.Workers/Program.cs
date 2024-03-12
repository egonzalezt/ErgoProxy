using ErgoProxy.Workers.ServiceCollection;
using ErgoProxy.Infrastructure.ServiceCollection;
using ErgoProxy.Workers.Workers.Consumers;
using Frieren_Guard.Extensions;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddFrierenGuardServices(builder.Configuration);
builder.Services.AddWorkerInfrastructure(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<UsersWorker>();
builder.Services.AddHostedService<OperatorsWorker>();
builder.Services.AddHostedService<DocumentsWorker>();
builder.Configuration.AddEnvironmentVariables();

var host = builder.Build();
host.Run();

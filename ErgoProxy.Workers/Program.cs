using ErgoProxy.Workers.ServiceCollection;
using ErgoProxy.Infrastructure.ServiceCollection;
using ErgoProxy.HealthChecks.Extensions;
using ErgoProxy.Workers.Workers.Consumers;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHealthChecksServices(builder.Configuration);
builder.Services.AddWorkerInfrastructure(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<UsersWorker>();
builder.Services.AddHostedService<OperatorsWorker>();
builder.Services.AddHostedService<DocumentsWorker>();
var host = builder.Build();
host.Run();

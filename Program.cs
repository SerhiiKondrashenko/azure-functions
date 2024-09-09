using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Azure.Storage.Blobs;
using Microsoft.Azure.Cosmos;
using Azure.Messaging.ServiceBus;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services => {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddLogging();

        var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
        var blobServiceClient = new BlobServiceClient(configuration.GetConnectionString("AzureWebJobsStorage"));
        services.AddSingleton(blobServiceClient);
        
        var cosmosServiceClient = new CosmosClient(configuration.GetConnectionString("CosmosDBConnection"));
        services.AddSingleton(cosmosServiceClient);
    })
    .Build();

host.Run();

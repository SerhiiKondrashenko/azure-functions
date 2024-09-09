using System;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs;
using Azure.Storage.Blobs;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;

namespace AzureWebshopFunctions
{
    public class CosmoOrdersApp
    {
        private readonly ILogger<CosmoOrdersApp> _logger;
        private readonly CosmosClient _cosmosClient;

        public CosmoOrdersApp(ILogger<CosmoOrdersApp> logger, CosmosClient cosmosClient)
        {
            _logger = logger;
            _cosmosClient = cosmosClient;
        }

        [Function("CosmoOrdersApp")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            _logger.LogInformation("Processing request");

            string order = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic orderData = JObject.Parse(order);

            var container = _cosmosClient.GetContainer("CosmoLab", "Orders");
            orderData.Id = orderData.Id.ToString();
            orderData.id = orderData.Id;
            var partitionKey = $"{orderData.Id}";

            _logger.LogInformation($"{orderData.Id.ToString()}");

            var response = await container.CreateItemAsync(orderData, new PartitionKey(partitionKey));

            _logger.LogInformation($"Item created with id {response.Resource.id}");

            return new OkResult();
        }
    }
}

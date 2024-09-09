using System;
using System.Text;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Azure.Storage.Blobs;

namespace AzureWebshopFunctions
{
    public class ServiceBusConsumer(ILogger<ServiceBusConsumer> logger, BlobServiceClient blobServiceClient)
    {
        private readonly ILogger<ServiceBusConsumer> _logger = logger;
        private readonly BlobServiceClient _blobServiceClient = blobServiceClient;

        [Function("ServiceBusConsumer")]
        public async Task Run(
            [ServiceBusTrigger("warehouse_reserver", Connection = "ServiceBusConnectionString")] ServiceBusReceivedMessage message
        )
        {
            string messageBody = message.Body.ToString();
            _logger.LogInformation($"Received message: {messageBody}");

            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient("warehouse-storage");
            
            Guid guid = Guid.NewGuid();
            string fileTemplate = "warehouse-store-{id}.json";
            string file = fileTemplate.Replace("{id}", guid.ToString("N"));
            BlobClient blobClient = containerClient.GetBlobClient(file);

            using Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(messageBody));
            await blobClient.UploadAsync(stream, true);

            _logger.LogInformation($"Blob was created at {blobClient.Uri}");
        }
    }
}

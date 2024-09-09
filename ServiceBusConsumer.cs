using System;
using System.Text;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Azure.Storage.Blobs;
using System.Net.Http;

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

            await UploadBlobStorage(messageBody);
        }

        private async Task UploadBlobStorage(string content, int attempt = 1)
        {
            try {
                BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient("warehouse-storage");
                
                Guid guid = Guid.NewGuid();
                string fileTemplate = "warehouse-store-{id}.json";
                string file = fileTemplate.Replace("{id}", guid.ToString("N"));
                BlobClient blobClient = containerClient.GetBlobClient(file);

                using Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        
                await blobClient.UploadAsync(stream, true);

                _logger.LogInformation($"Blob was created at {blobClient.Uri}");

            } catch (Exception ex) {
                _logger.LogError($"Failed to upload blob: {ex.Message}");
                if (attempt < 3) {
                    _logger.LogInformation("Retrying upload...");
                    await UploadBlobStorage(content, attempt + 1);
                } else {
                    _logger.LogError("Max upload attempts reached, sending failure notification...");
                    await SendFailureEmail(content);
                }
            } 
        }

        private async Task SendFailureEmail(string content)
        {
            var httpClient = new HttpClient();
            var data = new StringContent(content, Encoding.UTF8, "application/json");
            string sendEmailUrl = Environment.GetEnvironmentVariable("SendEmailUrl") ?? "";
            await httpClient.PostAsync(sendEmailUrl, data);
        }
    }
}

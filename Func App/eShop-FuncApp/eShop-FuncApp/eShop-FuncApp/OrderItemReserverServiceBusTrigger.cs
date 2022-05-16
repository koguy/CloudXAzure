using System;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace eShop_FuncApp
{
    public class OrderItemReserverServiceBusTrigger
    {
        [FunctionName("OrderItemReserverServiceBusTrigger")]
        public async Task Run([ServiceBusTrigger("eshoporderqueue", Connection = "eshopServiceBus")]string myQueueItem, ILogger log)
        {
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");

            string connectionString = Environment.GetEnvironmentVariable("blobConnectionString", EnvironmentVariableTarget.Process);
            string containerName = Environment.GetEnvironmentVariable("containerName", EnvironmentVariableTarget.Process);
            string logicAppUrl = Environment.GetEnvironmentVariable("logicAppUrl", EnvironmentVariableTarget.Process);

            int attempt = 1;
            bool uploadedFailed = false;
            do
            {
                try
                {
                    BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
                    BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                    await containerClient.CreateIfNotExistsAsync();
                    log.LogInformation($"Container {containerName} has been created.");

                    BlobClient blobClient = containerClient.GetBlobClient($"{new Random().Next()}.json");
                    log.LogInformation($"Blob has been created.");

                    await blobClient.UploadAsync(new BinaryData(myQueueItem));
                    log.LogInformation("uploaded to the blob");
                }
                catch (Exception ex)
                {
                    uploadedFailed = true;
                    attempt++;
                }
            }
            while (uploadedFailed && attempt != 3);

            if (uploadedFailed)
            {
                var client = new HttpClient();
                HttpResponseMessage result = await client.PostAsync(logicAppUrl, new StringContent(myQueueItem, Encoding.UTF8, "application/json"));
            }
        }
    }
}

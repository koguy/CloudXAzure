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

            string connectionString = "DefaultEndpointsProtocol=https;AccountName=eshopstorageac;AccountKey=Z3jJ0P9U8vpZMQAB3r7eMDS+4S05M7luPLNhbLADpwjAPMkuvpzS8IBc3hp5kzXiWSiwy+NumbJE/0F2UrJuOw==;EndpointSuffix=core.windows.net";
            string containerName = "eshopcontainer";

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
                HttpResponseMessage result = await client.PostAsync("https://prod-03.centralus.logic.azure.com:443/workflows/07c05f1f2354408bab385e6653210fad/triggers/manual/paths/invoke?api-version=2016-10-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=djaDWdR35D4I22b1m_5T8MZ3Exa_SMbltEkAw-P8MPw",
                    new StringContent(myQueueItem, Encoding.UTF8, "application/json"));
            }
        }
    }
}

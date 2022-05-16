using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace eShop_FuncApp
{
    public static class OrderItemsReserver
    {
        [FunctionName("OrderItemsReserver")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string connectionString = "DefaultEndpointsProtocol=https;AccountName=eshoprg8813;AccountKey=m8rn5wLrhUtPAURsVz1SA3ZggoMt0Y8HLn7DPVb0Kq4oe6kpSPbYsBZvs0lgWCAEqA/LSns0PMIeuEV8krtETg==;EndpointSuffix=core.windows.net";
            string containerName = "eshop-container";

            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();

            BlobClient blobClient = containerClient.GetBlobClient($"{new Random().Next()}.txt");
            await blobClient.UploadAsync(req.Body);

            return new OkObjectResult($"The order was reserved.");
        }
    }
}

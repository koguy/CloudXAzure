using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;

namespace eShop_FuncApp
{
    public static class OrderItemsForDelivery
    {
        [FunctionName("OrderItemsForDelivery")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string endpointUri = "https://koguy.documents.azure.com:443/";
            string key = "vYJBz6jE1rC18ZgwM20xdyX8hXNnNIKtPEnshe7zIGXz0ynM2sLc8CxiYsazkPWb8qCktms8UL7IAyrn47PntA==";
            string databaseId = "eshopDelivery";
            string containerId = "order";

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Order order = JsonConvert.DeserializeObject<Order>(requestBody);


            using (CosmosClient client = new CosmosClient(endpointUri, key))
            {
                Database database = await client.CreateDatabaseIfNotExistsAsync(databaseId);
                Container container = await database.CreateContainerIfNotExistsAsync(containerId, "/ShipAddress");
                try
                {
                    ItemResponse<Order> response = await container.CreateItemAsync<Order>(order, new PartitionKey(order.ShipAddress));
                }
                catch (CosmosException e)
                {
                    log.LogError(e.Message);
                }

            }
            log.LogInformation("C# HTTP trigger function processed a request.");

            return new OkObjectResult("");
        }
    }

    public class Order
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        public string ShipAddress { get; set; }
        public List<string> Items { get; set; }
        public int FinalPrice { get; set; }
    }
}

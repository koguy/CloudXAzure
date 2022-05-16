using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Microsoft.eShopWeb.ApplicationCore.Services;
public class ServiceBusService : IServiceBusService
{
    private readonly IConfiguration _configuration;
    private readonly IAppLogger<ServiceBusService> _logger;
    private readonly ServiceBusClient _client;
    public ServiceBusService(IConfiguration configuration, IAppLogger<ServiceBusService> logger)
    {
        _configuration = configuration;
        _client = new ServiceBusClient(_configuration.GetSection("ServiceBusConnectionString").Value);
        _logger = logger;
    }
    public async Task SendMessageAsync(string queueName, string body)
    {
        try
        {
            await using ServiceBusSender sender = _client.CreateSender(queueName);
            var message = new ServiceBusMessage(body);
            await sender.SendMessageAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error with sending message to ServiceBus with queue {queueName}", ex);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace Microsoft.eShopWeb.ApplicationCore.Interfaces;
public interface IServiceBusService
{
    Task SendMessageAsync(string queueName, string body);
}

// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Azure.ServiceBus.Core.Tests
{
    using System;


    public static class AzureServiceBusEndpointUriCreator
    {
        public static Uri Create(string serviceBusNamespace, string entityPath = null, string azureEndPoint = "servicebus.windows.net")
        {
            var endpoint = $"sb://{serviceBusNamespace}.{azureEndPoint}/{entityPath}";

            return new Uri(endpoint);
        }
    }
}

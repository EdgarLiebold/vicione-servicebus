using System;
using System.Linq;
using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

internal static class AuthorizationRuleSnapshot
{
    public static void CopyTo(AuthorizationRules source, AuthorizationRules destination)
    {
        foreach (AuthorizationRule rule in source)
        {
            if (rule is not SharedAccessAuthorizationRule shared)
                throw new NotSupportedException($"Unsupported Azure Service Bus authorization rule: {rule.GetType().FullName}.");

            destination.Add(new SharedAccessAuthorizationRule(
                shared.KeyName, shared.PrimaryKey, shared.SecondaryKey, shared.Rights.ToArray()));
        }
    }
}

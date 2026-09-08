using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ViciOne.ServiceBus.Quartz.Runtime;

internal static class QuartzSchedulerNamespace
{
    public static string ForBus(Type busType)
    {
        ArgumentNullException.ThrowIfNull(busType);
        string identity = GetStableBusIdentity(busType);
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(identity));
        return $"ViciOne.ServiceBus.Quartz.{Convert.ToHexStringLower(digest.AsSpan(0, 12))}";
    }

    public static string ForEndpoint(string queueName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(queueName));
        return $"ViciOne.ServiceBus.Quartz.{Convert.ToHexStringLower(digest.AsSpan(0, 12))}";
    }

    internal static string GetStableBusIdentity(Type busType)
    {
        ArgumentNullException.ThrowIfNull(busType);
        string assemblyName = busType.Assembly.GetName().Name
            ?? throw new ArgumentException("The bus type assembly must have a simple name.", nameof(busType));
        if (!busType.IsGenericType)
            return $"{assemblyName}:{busType.FullName ?? busType.Name}";

        Type definition = busType.GetGenericTypeDefinition();
        string definitionName = definition.FullName ?? definition.Name;
        int aritySeparator = definitionName.IndexOf('`');
        if (aritySeparator >= 0)
            definitionName = definitionName[..aritySeparator];
        string arguments = string.Join(",", busType.GetGenericArguments().Select(GetStableBusIdentity));

        return $"{assemblyName}:{definitionName}[{arguments}]";
    }
}

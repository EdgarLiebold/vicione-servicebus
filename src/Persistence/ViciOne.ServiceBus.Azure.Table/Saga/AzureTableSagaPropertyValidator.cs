using System;
using System.Text;
using ViciOne.ServiceBus.Azure.Table.Infrastructure;

namespace ViciOne.ServiceBus.Azure.Table.Saga;

internal static class AzureTableSagaPropertyValidator
{
    internal static string ValidateStorageName(string storageName, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageName, parameterName);
        if (storageName.Length > AzureTableStorageLimits.MaximumPropertyNameCharacters)
        {
            throw new ArgumentException(
                $"An Azure Table property name cannot exceed {AzureTableStorageLimits.MaximumPropertyNameCharacters} characters.",
                parameterName);
        }

        return storageName;
    }

    internal static object ValidateValue(object value, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        return value switch
        {
            string text when Encoding.Unicode.GetByteCount(text) > AzureTableStorageLimits.MaximumPropertyBytes =>
                throw UnsupportedValue(
                    propertyName,
                    $"its UTF-16 representation exceeds {AzureTableStorageLimits.MaximumPropertyBytes} bytes"),
            byte[] bytes when bytes.Length > AzureTableStorageLimits.MaximumPropertyBytes =>
                throw UnsupportedValue(
                    propertyName,
                    $"its binary representation exceeds {AzureTableStorageLimits.MaximumPropertyBytes} bytes"),
            DateTime dateTime when dateTime.ToUniversalTime() < AzureTableStorageLimits.MinimumDateTimeUtc =>
                throw UnsupportedValue(propertyName, $"its UTC value is earlier than {AzureTableStorageLimits.MinimumDateTimeUtc:O}"),
            DateTimeOffset dateTimeOffset when dateTimeOffset < AzureTableStorageLimits.MinimumDateTimeOffset =>
                throw UnsupportedValue(propertyName, $"its UTC value is earlier than {AzureTableStorageLimits.MinimumDateTimeOffset:O}"),
            _ => value,
        };
    }

    private static InvalidOperationException UnsupportedValue(string propertyName, string reason) =>
        new($"Saga property '{propertyName}' cannot be stored in Azure Table because {reason}.");
}

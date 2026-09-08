using System;

namespace ViciOne.ServiceBus.Azure.Table.Saga;

/// <summary>Represents the Azure Table ETag associated with a persisted saga.</summary>
internal sealed class AzureTableSagaETag
{
    /// <summary>Captures the Azure Table entity tag loaded with a saga instance.</summary>
    /// <param name="eTag">The entity tag used for optimistic update and delete operations.</param>
    public AzureTableSagaETag(string eTag)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eTag);
        ETag = eTag;
    }

    /// <summary>Gets the Azure Table entity tag.</summary>
    public string ETag { get; }
}

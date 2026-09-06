namespace ViciOne.ServiceBus.AzureTable.Saga;

/// <summary>Represents the Azure Table ETag associated with a persisted saga.</summary>
public class SagaETag
{
    /// <summary>Captures the Azure Table entity tag loaded with a saga instance.</summary>
    /// <param name="eTag">The entity tag used for optimistic update and delete operations.</param>
    public SagaETag(string eTag)
    {
        ETag = eTag;
    }

    /// <summary>Gets the Azure Table entity tag.</summary>
    public string ETag { get; }
}

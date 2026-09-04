namespace ViciOne.ServiceBus.AzureTable.Saga;

/// <summary>
/// Provides a saga e tag implementation.
/// </summary>
public class SagaETag
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="eTag">The e tag value.</param>
    public SagaETag(string eTag)
    {
        ETag = eTag;
    }

    /// <summary>
    /// Gets the e tag value.
    /// </summary>
    public string ETag { get; }
}

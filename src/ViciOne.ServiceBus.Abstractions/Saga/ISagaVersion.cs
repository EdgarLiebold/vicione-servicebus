namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// For saga repositories that use an incrementing version
/// </summary>
public interface ISagaVersion :
    ISaga
{
    /// <summary>
    /// Gets or sets the version value.
    /// </summary>
    int Version { get; set; }
}

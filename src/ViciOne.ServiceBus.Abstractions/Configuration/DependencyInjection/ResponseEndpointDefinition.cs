namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Specifies a temporary endpoint, with the prefix "response"
/// </summary>
public class ResponseEndpointDefinition :
    TemporaryEndpointDefinition
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public ResponseEndpointDefinition()
        : base("response")
    {
    }
}

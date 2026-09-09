namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines an auto-deleting receive endpoint tagged for request responses.</summary>
public sealed class ResponseEndpointDefinition :
    TemporaryEndpointDefinition
{
    /// <summary>Creates a temporary endpoint definition whose generated name uses the <c>response</c> tag.</summary>
    public ResponseEndpointDefinition()
        : base("response")
    {
    }
}

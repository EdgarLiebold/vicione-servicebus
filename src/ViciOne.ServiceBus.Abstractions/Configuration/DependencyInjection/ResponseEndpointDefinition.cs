// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    /// <summary>
    /// Specifies a temporary endpoint, with the prefix "response"
    /// </summary>
    public class ResponseEndpointDefinition :
        TemporaryEndpointDefinition
    {
        public ResponseEndpointDefinition()
            : base("response")
        {
        }
    }
}

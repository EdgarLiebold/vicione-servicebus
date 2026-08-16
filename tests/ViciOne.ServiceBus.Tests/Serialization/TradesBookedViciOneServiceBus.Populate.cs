// ViciOne modification: WP-F2-SERVICEBUS-A-PLUS-RECOVERY-03, 2026-08-16.
namespace ViciOne.ServiceBus.Tests.Serialization
{
    using System.Text.Json.Serialization;


    /// <summary>
    /// A protobuf generated message exposes its repeated fields through a getter only property over a readonly
    /// backing field. System.Text.Json replaces a property by default and therefore skips one it cannot assign,
    /// which leaves the collection empty. Populate tells it to fill the instance the getter already returns.
    ///
    /// The attribute sits on this test type alone. The product serialization stays unchanged: this is a statement
    /// about one message shape used as a compatibility contract, not a global change of how the bus serializes.
    /// </summary>
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public sealed partial class TradesBookedViciOneServiceBus
    {
    }
}

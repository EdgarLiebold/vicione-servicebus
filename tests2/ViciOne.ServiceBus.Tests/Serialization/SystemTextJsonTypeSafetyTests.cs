using System.Text.Json;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class SystemTextJsonTypeSafetyTests
{
    private const string HostileBody =
        "{\"$type\":\"Foreign.Commands.DangerousCommand, Foreign.Commands\",\"id\":1,\"name\":\"bob\"}";

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-TYPE-SAFETY", "payload-type-marker-is-data")]
    public void PayloadTypeMarker_CannotChooseTheInstantiatedType()
    {
        var serializer = new SystemTextJsonMessageSerializer();
        using JsonDocument document = JsonDocument.Parse(HostileBody);

        Assert.True(
            document.RootElement.TryGetProperty("$type", out JsonElement marker),
            "The security fixture must contain the hostile type marker it claims to exercise.");
        Assert.Equal(
            "Foreign.Commands.DangerousCommand, Foreign.Commands",
            marker.GetString());

        TypeSafeCommand? result = serializer.DeserializeObject<TypeSafeCommand>(HostileBody);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("bob", result.Name);
        Assert.DoesNotContain("Foreign.Commands", result.GetType().AssemblyQualifiedName, StringComparison.Ordinal);
    }
}

public interface TypeSafeCommand
{
    int Id { get; }

    string Name { get; }
}

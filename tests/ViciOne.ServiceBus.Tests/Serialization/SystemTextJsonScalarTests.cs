using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class SystemTextJsonScalarTests
{
    [Theory]
    [InlineData('\0', null)]
    [InlineData('A', null)]
    [InlineData('\0', 'A')]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-SCALARS", "char-and-nullable-char")]
    public void CharacterValues_RoundTripExactly(char character, char? optionalCharacter)
    {
        var source = new CharacterMessage
        {
            Character = character,
            OptionalCharacter = optionalCharacter,
        };

        CharacterMessage result = SystemTextJsonRoundTrip.Execute(source);

        Assert.Equal(character, result.Character);
        Assert.Equal(optionalCharacter, result.OptionalCharacter);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-SCALARS", "control-character-private-setter")]
    public void ControlCharacterAndPrivateSetter_RoundTripExactly()
    {
        var source = new PrivateSetterStringMessage("\u0001");

        PrivateSetterStringMessage result = SystemTextJsonRoundTrip.Execute(source);

        Assert.Equal("\u0001", result.Body);
    }
}

public sealed class CharacterMessage
{
    public char Character { get; init; }

    public char? OptionalCharacter { get; init; }
}

public sealed class PrivateSetterStringMessage
{
    public PrivateSetterStringMessage(string body)
    {
        Body = body;
    }

    public string Body { get; private set; }
}

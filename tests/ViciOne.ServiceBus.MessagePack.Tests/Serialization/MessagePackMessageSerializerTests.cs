using System.Text;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.MessageData;
using ViciOne.ServiceBus.MessageData.Values;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.MessagePack.Tests.Serialization;

public sealed class MessagePackMessageSerializerTests
{
    private const string XmlDocument =
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
        "<order id=\"4711\"><customer name=\"Grüße &amp; Co\" />" +
        "<note><![CDATA[keep <this> verbatim]]></note></order>";

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-ENVELOPE", "metadata-and-body-roundtrip")]
    public void EnvelopeRoundTrip_PreservesMessageAndTransportMetadata()
    {
        var source = new ScalarMessage
        {
            DecimalValue = 123.45m,
            LongValue = 98_123_213,
            BoolValue = true,
            ByteValue = 127,
            IntValue = 123,
            DateTimeValue = new DateTime(2008, 9, 8, 7, 6, 5, 4, DateTimeKind.Utc),
            TimeSpanValue = TimeSpan.FromSeconds(30),
            GuidValue = Guid.NewGuid(),
            StringValue = "Chris's sample",
            DoubleValue = 1823.172,
            OptionalDecimal = 567.89m,
        };

        var result = MessagePackRoundTrip.ExecuteWithContext(source);

        AssertScalarMessage(source, result.Message);
        Assert.Equal(result.RequestId, result.Context.RequestId);
        Assert.Equal(new Uri("loopback://localhost/source"), result.Context.SourceAddress);
        Assert.Equal(new Uri("loopback://localhost/destination"), result.Context.DestinationAddress);
        Assert.Equal(new Uri("loopback://localhost/response"), result.Context.ResponseAddress);
        Assert.Equal(new Uri("loopback://localhost/fault"), result.Context.FaultAddress);
        Assert.Equal("preserved", result.Context.Headers.Get<string>("test-header"));
        Assert.NotEmpty(result.Bytes);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-SCALARS", "binary-concrete-and-interface")]
    public void BinaryPayloads_RoundTripForConcreteAndInterfaceContracts()
    {
        byte[] expected = [0x56, 0x34, 0xF3];

        var concrete = MessagePackRoundTrip.Execute(new BinaryMessage { Contents = expected });
        BinaryContract contract = new BinaryContractImplementation { Contents = expected };
        var interfaceResult = MessagePackRoundTrip.Execute(contract);

        Assert.Equal(expected, concrete.Contents);
        Assert.Equal(expected, interfaceResult.Contents);
        Assert.NotSame(expected, concrete.Contents);
        Assert.NotSame(expected, interfaceResult.Contents);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-SCALARS", "date-time-kind")]
    public void DateTimeValues_RoundTripWithTheirKinds()
    {
        var source = new TemporalMessage
        {
            Local = new DateTime(2001, 9, 11, 8, 46, 30, DateTimeKind.Local),
            Universal = new DateTime(2001, 9, 11, 13, 3, 2, DateTimeKind.Utc),
        };

        var result = MessagePackRoundTrip.Execute(source);

        Assert.Equal(source.Local, result.Local);
        Assert.Equal(DateTimeKind.Local, result.Local.Kind);
        Assert.Equal(source.Universal, result.Universal);
        Assert.Equal(DateTimeKind.Utc, result.Universal.Kind);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-CONTRACTLESS", "private-setters-no-default-constructor")]
    public void ContractlessObjects_RoundTripWithoutPublicDefaultConstructorOrSetters()
    {
        var result = MessagePackRoundTrip.Execute(new ConstructorBoundMessage("Dru", "Sellers"));

        Assert.Equal("Dru", result.Name);
        Assert.Equal("Sellers", result.Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-SCALARS", "precision-floor")]
    public void DecimalNearPrecisionFloor_RoundTripsExactly()
    {
        var source = new ScalarMessage { DecimalValue = 0.000001m };

        var result = MessagePackRoundTrip.Execute(source);

        Assert.Equal(0.000001m, result.DecimalValue);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-COLLECTIONS", "arrays-sets-enumerables-and-duplicate-keys")]
    public void CollectionShapes_RoundTripWithoutChangingTheirSemantics()
    {
        var source = new CollectionMessage
        {
            Array = [1, 2, 3],
            ConcreteSet = [1, 2, 3],
            InterfaceSet = new HashSet<int> { 4, 5, 6 },
            Enumerable = [new NestedMessage { Name = "Frank" }, new NestedMessage { Name = "Mary" }],
            DuplicateKeys =
            [
                new("Frank", "Mary"),
                new("Peter", "Mary"),
                new("Frank", "Peter"),
            ],
            Dictionary = new Dictionary<string, NestedMessage>
            {
                ["Chris"] = new() { Name = "Chris" },
                ["David"] = new() { Name = "David" },
            },
            Matrix = new[,] { { 1, 2 }, { 3, 4 }, { 5, 6 } },
        };

        var result = MessagePackRoundTrip.Execute(source);
        var emptyDictionary = MessagePackRoundTrip.Execute(new CollectionMessage());
        var singleDictionary = MessagePackRoundTrip.Execute(new CollectionMessage
        {
            Dictionary = new Dictionary<string, NestedMessage>
            {
                ["Only"] = new() { Name = "Only" },
            },
        });

        Assert.Equal(source.Array, result.Array);
        Assert.True(result.ConcreteSet.SetEquals(source.ConcreteSet));
        Assert.True(result.InterfaceSet.SetEquals(source.InterfaceSet));
        Assert.Equal(["Frank", "Mary"], result.Enumerable.Select(item => item.Name));
        Assert.Equal(3, result.DuplicateKeys.Count);
        Assert.Equal(2, result.DuplicateKeys.Count(pair => pair.Key == "Frank"));
        Assert.Equal("Chris", result.Dictionary["Chris"].Name);
        Assert.Equal("David", result.Dictionary["David"].Name);
        Assert.Empty(emptyDictionary.Dictionary);
        Assert.Equal("Only", Assert.Single(singleDictionary.Dictionary).Value.Name);
        Assert.Equal(3, result.Matrix.GetLength(0));
        Assert.Equal(2, result.Matrix.GetLength(1));
        Assert.Equal(4, result.Matrix[1, 1]);
    }

    [Theory]
    [InlineData('\0', null)]
    [InlineData('A', null)]
    [InlineData('\0', 'A')]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-SCALARS", "char-and-nullable-char")]
    public void CharacterValues_RoundTrip(char character, char? optionalCharacter)
    {
        var source = new EdgeShapeMessage("private")
        {
            Character = character,
            OptionalCharacter = optionalCharacter,
            State = ExampleState.Ready,
        };

        var result = MessagePackRoundTrip.Execute(source);

        Assert.Equal(character, result.Character);
        Assert.Equal(optionalCharacter, result.OptionalCharacter);
        Assert.Equal(ExampleState.Ready, result.State);
        Assert.Equal("private", result.PrivateValue);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-EDGE-SHAPES", "empty-object-and-empty-collections")]
    public void EmptyShapes_RoundTripWithoutInventingContent()
    {
        var emptyObject = MessagePackRoundTrip.Execute(new EmptyMessage());
        var emptyCollections = MessagePackRoundTrip.Execute(new CollectionMessage());

        Assert.NotNull(emptyObject);
        Assert.Empty(emptyCollections.Array);
        Assert.Empty(emptyCollections.ConcreteSet);
        Assert.Empty(emptyCollections.InterfaceSet);
        Assert.Empty(emptyCollections.Enumerable);
        Assert.Empty(emptyCollections.DuplicateKeys);
        Assert.Empty(emptyCollections.Dictionary);
        Assert.Empty(emptyCollections.Matrix.Cast<int>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-OBJECT-GRAPHS", "nested-list-array-and-pairs")]
    public void ObjectGraphShapes_RoundTripWithZeroOneAndManyMembers()
    {
        var source = new ObjectGraphMessage
        {
            Nested = new NestedMessage { Name = "root" },
            List = [new() { Name = "first" }, new() { Name = "second" }],
            Array = [new() { Name = "only" }],
            Pairs = [new("Key1", "Value1"), new("Key2", "Value2")],
        };

        var result = MessagePackRoundTrip.Execute(source);

        Assert.Equal("root", result.Nested.Name);
        Assert.Equal(["first", "second"], result.List.Select(item => item.Name));
        Assert.Equal(["only"], result.Array.Select(item => item.Name));
        Assert.Equal(source.Pairs, result.Pairs);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-OBJECT-GRAPHS", "read-only-interface-collections")]
    public void ReadOnlyInterfaceCollections_RoundTripThroughTheirDeclaredContract()
    {
        ValidationContract source = new ValidationMessage
        {
            IsValid = false,
            Errors = new Dictionary<string, IReadOnlyList<string>>
            {
                ["Name"] = new List<string> { "required", "too-long" },
            },
        };

        var result = MessagePackRoundTrip.Execute(source);

        Assert.False(result.IsValid);
        Assert.Equal(["required", "too-long"], result.Errors["Name"]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-XML-PAYLOAD", "opaque-text-and-bytes")]
    public void XmlPayload_RemainsOpaqueTextAndBytes()
    {
        var source = new XmlPayloadMessage
        {
            Text = XmlDocument,
            Bytes = Encoding.UTF8.GetBytes(XmlDocument),
        };

        var result = MessagePackRoundTrip.Execute(source);

        Assert.Equal(XmlDocument, result.Text);
        Assert.Equal(source.Bytes, result.Bytes);
        Assert.Equal(XmlDocument, Encoding.UTF8.GetString(result.Bytes));
        Assert.DoesNotContain(
            "xml",
            new ViciOne.ServiceBus.Serialization.MessagePackMessageSerializer().ContentType.MediaType,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-MESSAGE-DATA", "external-reference")]
    public async Task MessageData_RoundTripsItsExternalReferenceAsync()
    {
        var repository = new InMemoryMessageDataRepository();
        var source = new MessageDataContainer
        {
            Value = await repository.PutStringAsync(
                new string('*', MessageDataPolicy.Default.Threshold + 100),
                TestContext.Current.CancellationToken),
        };

        var result = MessagePackRoundTrip.Execute(source);

        Assert.NotNull(result.Value);
        Assert.Equal(source.Value.Address, result.Value.Address);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-MESSAGE-DATA", "inline-text-value")]
    public async Task MessageData_RoundTripsItsInlineTextAsync()
    {
        const string expected = "Inline payload preserved by MessagePack";
        var source = new MessageDataContainer
        {
            Value = new StringInlineMessageData(expected),
        };

        var result = MessagePackRoundTrip.Execute(source);

        Assert.True(result.Value.HasValue);
        Assert.Null(result.Value.Address);
        Assert.Equal(expected, await result.Value.Value);
    }

    private static void AssertScalarMessage(ScalarMessage expected, ScalarMessage actual)
    {
        Assert.Equal(expected.DecimalValue, actual.DecimalValue);
        Assert.Equal(expected.LongValue, actual.LongValue);
        Assert.Equal(expected.BoolValue, actual.BoolValue);
        Assert.Equal(expected.ByteValue, actual.ByteValue);
        Assert.Equal(expected.IntValue, actual.IntValue);
        Assert.Equal(expected.DateTimeValue, actual.DateTimeValue);
        Assert.Equal(expected.TimeSpanValue, actual.TimeSpanValue);
        Assert.Equal(expected.GuidValue, actual.GuidValue);
        Assert.Equal(expected.StringValue, actual.StringValue);
        Assert.Equal(expected.DoubleValue, actual.DoubleValue);
        Assert.Equal(expected.OptionalDecimal, actual.OptionalDecimal);
    }

    private sealed class BinaryContractImplementation : BinaryContract
    {
        public byte[] Contents { get; set; } = [];
    }

    private sealed class MessageDataContainer
    {
        public ViciOne.ServiceBus.Advanced.Serialization.MessageData<string> Value { get; set; } = null!;
    }
}

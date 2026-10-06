using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports;

public sealed class JsonTransportHeaderConversionTests
{
    private static readonly AsyncLocal<ReadFailureState?> ReadFailure = new();

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    [RequirementCoverage("REQ-VSB-JSON-TRANSPORT-HEADERS", "compatible-representations-and-primary-failure-preservation")]
    public void Get_CompatibleValuesAndFailureBoundaries_PreservePublicContract(bool referenceType, int representation)
    {
        AssertCompatibleAndFailureBoundaries(referenceType, representation);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    [RequirementCoverage("REQ-VSB-JSON-TRANSPORT-HEADERS", "valid-incompatible-representations-preserve-caller-fallback")]
    public void Get_ValidIncompatibleGuid_ReturnsExactCallerFallback(bool referenceType, int representation)
    {
        AssertCompatibleAndFailureBoundaries(referenceType, representation);
        var input = Guid.Parse("d5e4c956-d694-4f0e-bfea-46c553d7b32a");
        object represented = Represent(input, representation);
        var headers = CreateHeaders(represented);
        Assert.True(headers.TryGetHeader("value", out object? raw));
        Assert.Same(represented, raw);

        if (referenceType)
        {
            List<int> fallback = [7919, 7927];
            List<int>? converted = null;
            Exception? conversionFailure = Record.Exception(() => { converted = headers.Get("value", fallback); });
            Assert.Null(conversionFailure);
            Assert.Same(fallback, converted);
            Assert.Equal(new[] { 7919, 7927 }, converted);
        }
        else
        {
            int? fallback = 7919;
            int? converted = null;
            Exception? conversionFailure = Record.Exception(() => { converted = headers.Get<int>("value", fallback); });
            Assert.Null(conversionFailure);
            Assert.Equal(fallback, converted);
        }

        Assert.True(headers.TryGetHeader("value", out object? after));
        Assert.Same(represented, after);
    }

    private static void AssertCompatibleAndFailureBoundaries(bool referenceType, int representation)
    {
        if (referenceType)
        {
            List<int> compatible = [37, 41];
            List<int> fallback = [7919, 7927];
            var headers = CreateHeaders(Represent(compatible, representation));
            List<int>? result = headers.Get("value", fallback);
            Assert.Equal(compatible, result);
            Assert.NotSame(fallback, result);
            if (representation == 0)
                Assert.Same(compatible, result);
            Assert.Same(fallback, headers.Get("missing", fallback));
            Assert.Same(fallback, CreateHeaders(null).Get("value", fallback));
        }
        else
        {
            const int compatible = 37;
            int? fallback = 7919;
            var headers = CreateHeaders(Represent(compatible, representation));
            Assert.Equal(compatible, headers.Get<int>("value", fallback));
            Assert.Equal(fallback, headers.Get<int>("missing", fallback));
            Assert.Equal(fallback, CreateHeaders(null).Get<int>("value", fallback));
        }

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Exception[] failures =
        [
            new IOException("Unique header boundary IO failure"),
            new JsonException("Unique header boundary JSON failure"),
            new OperationCanceledException("Unique header boundary cancellation", cancellation.Token)
        ];

        foreach (Exception primary in failures)
        {
            var provider = new FaultingHeaderProvider(primary);
            Exception? providerFailure = Record.Exception(() => { GetSelected(new JsonTransportHeaders(provider), referenceType); });
            Assert.Same(primary, providerFailure);
            Assert.Equal(1, provider.Calls);

            var carrier = new WriteCarrier(primary);
            Exception? writeFailure = Record.Exception(() => { GetSelected(CreateHeaders(carrier), referenceType); });
            Assert.Same(primary, writeFailure);
            Assert.Equal(1, carrier.WriteCalls);

            using var readScope = new ReadFailureScope(primary);
            var readHeaders = CreateHeaders(Represent(new ReadInput { Value = 37 }, representation));
            Exception? readFailure = Record.Exception(() =>
            {
                if (referenceType)
                    readHeaders.Get("value", new ReadHeader());
                else
                    readHeaders.Get<ReadNumber>("value", default(ReadNumber));
            });
            Assert.Same(primary, readFailure);
            Assert.Equal(1, readScope.State.Calls);
            if (primary is OperationCanceledException canceled)
            {
                Assert.Equal(cancellation.Token, canceled.CancellationToken);
                Assert.True(canceled.CancellationToken.IsCancellationRequested);
            }
        }

        Assert.Throws<JsonException>(() => { GetSelected(CreateHeaders("{"), referenceType); });
        Assert.Throws<InvalidOperationException>(() => { GetSelected(CreateHeaders(default(JsonElement)), referenceType); });
    }

    private static object Represent(object value, int representation) => representation switch
    {
        0 => value,
        1 => JsonSerializer.Serialize(value),
        2 => JsonSerializer.SerializeToElement(value),
        _ => throw new ArgumentOutOfRangeException(nameof(representation))
    };

    private static JsonTransportHeaders CreateHeaders(object? value)
        => new(new DictionaryHeaderProvider(new Dictionary<string, object> { ["value"] = value! }));

    private static void GetSelected(JsonTransportHeaders headers, bool referenceType)
    {
        if (referenceType)
            headers.Get("value", new List<int> { 7919, 7927 });
        else
            headers.Get<int>("value", (int?)7919);
    }

    private sealed class FaultingHeaderProvider(Exception primary) : IHeaderProvider
    {
        public int Calls { get; private set; }
        public IEnumerable<KeyValuePair<string, object>> GetAll() => throw new InvalidOperationException("Unexpected enumeration");

        public bool TryGetHeader(string key, [NotNullWhen(true)] out object? value)
        {
            Calls++;
            throw primary;
        }
    }

    [JsonConverter(typeof(WriteCarrierConverter))]
    public sealed class WriteCarrier(Exception failure)
    {
        public Exception Failure { get; } = failure;
        public int WriteCalls { get; set; }
    }

    public sealed class WriteCarrierConverter : JsonConverter<WriteCarrier>
    {
        public override WriteCarrier? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => throw new InvalidOperationException("Unexpected carrier read");

        public override void Write(Utf8JsonWriter writer, WriteCarrier value, JsonSerializerOptions options)
        {
            value.WriteCalls++;
            throw value.Failure;
        }
    }

    public sealed class ReadInput
    {
        public int Value { get; init; }
    }

    [JsonConverter(typeof(ReadHeaderConverter))]
    public sealed class ReadHeader
    {
    }

    [JsonConverter(typeof(ReadNumberConverter))]
    public readonly struct ReadNumber
    {
    }

    public sealed class ReadHeaderConverter : JsonConverter<ReadHeader>
    {
        public override ReadHeader? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => throw CaptureReadFailure();

        public override void Write(Utf8JsonWriter writer, ReadHeader value, JsonSerializerOptions options)
            => throw new InvalidOperationException("Unexpected target write");
    }

    public sealed class ReadNumberConverter : JsonConverter<ReadNumber>
    {
        public override ReadNumber Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => throw CaptureReadFailure();

        public override void Write(Utf8JsonWriter writer, ReadNumber value, JsonSerializerOptions options)
            => throw new InvalidOperationException("Unexpected target write");
    }

    private static Exception CaptureReadFailure()
    {
        ReadFailureState state = ReadFailure.Value ?? throw new InvalidOperationException("No owned read-failure scope");
        state.Calls++;
        return state.Primary;
    }

    private sealed class ReadFailureState(Exception primary)
    {
        public Exception Primary { get; } = primary;
        public int Calls { get; set; }
    }

    private sealed class ReadFailureScope : IDisposable
    {
        private readonly ReadFailureState? _previous = ReadFailure.Value;
        public ReadFailureState State { get; }

        public ReadFailureScope(Exception primary)
        {
            State = new ReadFailureState(primary);
            ReadFailure.Value = State;
        }

        public void Dispose() => ReadFailure.Value = _previous;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [RequirementCoverage("REQ-VSB-JSON-TRANSPORT-HEADERS", "encoded-null-and-native-null-conversion-semantics")]
    public void Get_EncodedNullAndNativeNull_PreserveExistingConversionResults(int representation)
    {
        object encodedNull = representation switch
        {
            0 => "null",
            1 => JsonSerializer.SerializeToElement<object?>(null),
            _ => throw new ArgumentOutOfRangeException(nameof(representation))
        };
        var headers = CreateHeaders(encodedNull);
        List<int> fallback = [7919, 7927];
        Assert.Null(headers.Get("value", fallback));
        Assert.Equal(0m, headers.Get<decimal>("value", 7919m));
        Assert.True(headers.TryGetHeader("value", out object? unchangedEncoded));
        Assert.Same(encodedNull, unchangedEncoded);

        var carrier = new NullWriteCarrier();
        var nativeHeaders = CreateHeaders(carrier);
        Assert.Same(fallback, nativeHeaders.Get("value", fallback));
        Assert.Equal(7919m, nativeHeaders.Get<decimal>("value", 7919m));
        Assert.Equal(2, carrier.WriteCalls);
        Assert.True(nativeHeaders.TryGetHeader("value", out object? unchangedNative));
        Assert.Same(carrier, unchangedNative);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 1)]
    [InlineData(0, 2)]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 0)]
    [InlineData(2, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 0)]
    [InlineData(3, 1)]
    [InlineData(3, 2)]
    [RequirementCoverage("REQ-VSB-JSON-TRANSPORT-HEADERS", "nested-attributed-reader-causes-and-raw-values-preserved")]
    public void Get_NestedAttributedReader_PreservesExactFailureAndRawValue(int targetShape, int failureKind)
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Exception primary = failureKind switch
        {
            0 => new IOException("Unique nested transport-header reader IO failure"),
            1 => new JsonException("Unique nested transport-header reader JSON failure"),
            2 => new OperationCanceledException("Unique nested transport-header reader cancellation", null, cancellation.Token),
            _ => throw new ArgumentOutOfRangeException(nameof(failureKind))
        };
        object native = targetShape == 3
            ? new Dictionary<string, NestedInput> { ["a"] = new() { Value = 37 }, ["b"] = new() { Value = 41 } }
            : new[] { new NestedInput { Value = 37 }, new NestedInput { Value = 41 } };
        using var ambient = new NestedReadScope(null);
        for (int representation = 0; representation < 3; representation++)
        {
            object represented = Represent(native, representation);
            var headers = CreateHeaders(represented);
            NestedReadState? previous = NestedRead.Value;
            using (var healthy = new NestedReadScope(null))
            {
                object? result = GetNestedTarget(headers, targetShape);
                AssertNestedValues(result, targetShape);
                Assert.Equal(2, healthy.State.Calls);
            }
            Assert.Same(previous, NestedRead.Value);

            using (var hostile = new NestedReadScope(primary))
            {
                Exception? observed = Record.Exception(() => { GetNestedTarget(headers, targetShape); });
                Assert.Same(primary, observed);
                Assert.Equal(1, hostile.State.Calls);
                if (failureKind == 2)
                {
                    var canceled = Assert.IsType<OperationCanceledException>(observed);
                    Assert.Equal(cancellation.Token, canceled.CancellationToken);
                    Assert.True(canceled.CancellationToken.IsCancellationRequested);
                }
                Assert.True(headers.TryGetHeader("value", out object? unchanged));
                Assert.Same(represented, unchanged);
            }
            Assert.Same(previous, NestedRead.Value);
        }
        Assert.Equal(0, ambient.State.Calls);
    }

    private static object? GetNestedTarget(JsonTransportHeaders headers, int targetShape) => targetShape switch
    {
        0 => headers.Get("value", new List<NestedReadValue> { new(7919) }),
        1 => headers.Get("value", new NestedReadValue[] { new(7919) }),
        2 => headers.Get<IReadOnlyList<NestedReadValue>>("value", new NestedReadValue[] { new(7919) }),
        3 => headers.Get("value", new Dictionary<string, NestedReadValue> { ["fallback"] = new(7919) }),
        _ => throw new ArgumentOutOfRangeException(nameof(targetShape))
    };

    private static void AssertNestedValues(object? result, int targetShape)
    {
        int[] values = targetShape switch
        {
            0 => Assert.IsType<List<NestedReadValue>>(result).Select(x => x.Value).ToArray(),
            1 => Assert.IsType<NestedReadValue[]>(result).Select(x => x.Value).ToArray(),
            2 => Assert.IsAssignableFrom<IReadOnlyList<NestedReadValue>>(result).Select(x => x.Value).ToArray(),
            3 => Assert.IsType<Dictionary<string, NestedReadValue>>(result).OrderBy(x => x.Key, StringComparer.Ordinal)
                .Select(x => x.Value.Value).ToArray(),
            _ => throw new ArgumentOutOfRangeException(nameof(targetShape))
        };
        Assert.Equal(new[] { 37, 41 }, values);
        if (targetShape == 3)
            Assert.Equal(new[] { "a", "b" }, Assert.IsType<Dictionary<string, NestedReadValue>>(result).Keys.OrderBy(x => x, StringComparer.Ordinal));
    }

    [JsonConverter(typeof(NullWriteCarrierConverter))]
    public sealed class NullWriteCarrier
    {
        public int WriteCalls { get; set; }
    }

    public sealed class NullWriteCarrierConverter : JsonConverter<NullWriteCarrier>
    {
        public override NullWriteCarrier? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => throw new InvalidOperationException("Unexpected null carrier read");

        public override void Write(Utf8JsonWriter writer, NullWriteCarrier value, JsonSerializerOptions options)
        {
            value.WriteCalls++;
            writer.WriteNullValue();
        }
    }

    public sealed class NestedInput
    {
        public int Value { get; init; }
    }

    [JsonConverter(typeof(NestedReadValueConverter))]
    public sealed class NestedReadValue(int value)
    {
        public int Value { get; } = value;
    }

    public sealed class NestedReadValueConverter : JsonConverter<NestedReadValue>
    {
        public override NestedReadValue? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            NestedReadState state = NestedRead.Value ?? throw new InvalidOperationException("No owned nested read scope");
            state.Calls++;
            if (state.Primary is { } failure)
                throw failure;

            NestedInput? input = JsonSerializer.Deserialize<NestedInput>(ref reader, options);
            return input == null ? null : new NestedReadValue(input.Value);
        }

        public override void Write(Utf8JsonWriter writer, NestedReadValue value, JsonSerializerOptions options)
            => throw new InvalidOperationException("Unexpected nested target write");
    }

    private static readonly AsyncLocal<NestedReadState?> NestedRead = new();

    private sealed class NestedReadState(Exception? primary)
    {
        public Exception? Primary { get; } = primary;
        public int Calls { get; set; }
    }

    private sealed class NestedReadScope : IDisposable
    {
        private readonly NestedReadState? _previous = NestedRead.Value;
        public NestedReadState State { get; }

        public NestedReadScope(Exception? primary)
        {
            State = new NestedReadState(primary);
            NestedRead.Value = State;
        }

        public void Dispose() => NestedRead.Value = _previous;
    }
}

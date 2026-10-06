using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class DictionarySendHeadersTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-HEADERS", "independent-case-insensitive-copy")]
    public void CopyingConstructor_CreatesAnIndependentCaseInsensitiveSnapshot()
    {
        var source = new Dictionary<string, object>
        {
            ["Trace-Id"] = "original",
        };

        var headers = new DictionarySendHeaders(source);
        source["Trace-Id"] = "source-changed";
        headers.Set("TRACE-ID", "copy-changed");

        Assert.Equal("copy-changed", headers.Get<string>("trace-id", null));
        Assert.Equal("source-changed", source["Trace-Id"]);
        Assert.Single(headers.GetAll());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-HEADERS", "explicit-shared-dictionary")]
    public void ExistingDictionaryConstructor_PreservesTheExplicitSharedStorageContract()
    {
        var source = new Dictionary<string, object>
        {
            ["Trace-Id"] = "original",
        };
        var headers = DictionarySendHeaders.Wrap(source);

        headers.Set("Trace-Id", "through-headers");
        source["Extra"] = 27;

        Assert.Equal("through-headers", source["Trace-Id"]);
        Assert.Equal(27, headers.Get<int>("Extra", null));
        Assert.Equal(2, headers.GetAll().Count());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-HEADERS", "overwrite-preservation-and-removal")]
    public void Set_HonorsOverwritePreservationAndNullRemoval()
    {
        var headers = new DictionarySendHeaders();

        headers.Set("Name", "first");
        headers.Set("NAME", "ignored", overwrite: false);

        Assert.Equal("first", headers.Get<string>("name", null));

        headers.Set("name", "replacement", overwrite: true);
        Assert.Equal("replacement", headers.Get<string>("NAME", null));

        headers.Set("NAME", null);
        Assert.False(headers.TryGetHeader("name", out _));
        Assert.Empty(headers.GetAll());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-HEADERS", "required-inputs")]
    public void ConstructorsAndSet_RejectEveryMissingRequiredInput()
    {
        ArgumentNullException constructor = Assert.Throws<ArgumentNullException>(() =>
            new DictionarySendHeaders(null!));
        var headers = new DictionarySendHeaders();
        ArgumentNullException stringKey = Assert.Throws<ArgumentNullException>(() =>
            headers.Set(null!, "value"));
        ArgumentNullException objectKey = Assert.Throws<ArgumentNullException>(() =>
            headers.Set(null!, new object()));

        Assert.Equal("headers", constructor.ParamName);
        Assert.Equal("key", stringKey.ParamName);
        Assert.Equal("key", objectKey.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-HEADERS", "arbitrary-dictionary-implementation")]
    public void WrappedDictionary_DoesNotRequireIReadOnlyDictionaryForTypedReads()
    {
        IDictionary<string, object> source = new DictionaryOnly<string, object>
        {
            ["attempt"] = "27",
        };
        DictionarySendHeaders headers = DictionarySendHeaders.Wrap(source);

        Assert.Equal(27, headers.Get<int>("attempt", null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [RequirementCoverage("REQ-VSB-SEND-HEADERS", "non-empty-header-name")]
    public void Set_RejectsEveryEmptyHeaderName(string key)
    {
        var headers = new DictionarySendHeaders();

        ArgumentException stringValue = Assert.Throws<ArgumentException>(() => headers.Set(key, "value"));
        ArgumentException objectValue = Assert.Throws<ArgumentException>(() => headers.Set(key, new object()));

        Assert.Equal("key", stringValue.ParamName);
        Assert.Equal("key", objectValue.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-HEADERS", "wrapped-invalid-entry-containment")]
    public void WrappedDictionary_NeverExposesExternallyInjectedInvalidEntries()
    {
        var source = new Dictionary<string, object>
        {
            ["valid"] = 27,
        };
        DictionarySendHeaders headers = DictionarySendHeaders.Wrap(source);
        source[""] = "invalid-name";
        source["null-value"] = null!;

        Assert.False(headers.TryGetHeader("null-value", out _));
        Assert.Equal([new KeyValuePair<string, object>("valid", 27)], headers.GetAll());
        Assert.Equal([new HeaderValue("valid", 27)], headers.ToArray());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-HEADERS", "incompatible-value-fallback-codec-failure-distinction")]
    public void IncompatibleValueType_ReturnsTheFallbackWhileCodecFailuresRemainVisible()
    {
        var id = Guid.Parse("015dbb12-c268-4ad2-9275-b1fa1ea15c90");
        var source = new Dictionary<string, object>
        {
            ["id"] = id,
            ["number"] = 27,
            ["number-text"] = "27",
            ["null"] = null!,
        };
        DictionarySendHeaders headers = DictionarySendHeaders.Wrap(source);
        var codecFailure = new IOException("unique-value-header-codec-failure");
        var codecValue = new CodecFailureHeaderValue(codecFailure);
        headers.Set("codec-failure", codecValue);

        Exception? observedCodecFailure = Record.Exception(() =>
        {
            _ = headers.Get<int>("codec-failure", 42);
        });
        Assert.Same(codecFailure, observedCodecFailure);
        Assert.Equal(1, codecValue.WriteCalls);
        var jsonCodecFailure = new System.Text.Json.JsonException("unique-value-header-json-codec-failure");
        var jsonCodecValue = new CodecFailureHeaderValue(jsonCodecFailure);
        headers.Set("json-codec-failure", jsonCodecValue);
        Exception? observedJsonCodecFailure = Record.Exception(() =>
        {
            _ = headers.Get<int>("json-codec-failure", 42);
        });
        Assert.Same(jsonCodecFailure, observedJsonCodecFailure);
        Assert.Equal(1, jsonCodecValue.WriteCalls);

        Assert.Equal(27, headers.Get<int>("number", 42));
        Assert.Equal(27, headers.Get<int>("number-text", 42));
        Assert.Equal(42, headers.Get<int>("missing", 42));
        Assert.Null(headers.Get<int>("missing", null));
        Assert.Equal(42, headers.Get<int>("null", 42));
        Assert.Equal(id, headers.Get<Guid>("id", null));
        Assert.True(headers.TryGetHeader("id", out object? raw));
        Assert.Equal(id, Assert.IsType<Guid>(raw));

        int? result = null;
        Exception? conversionFailure = Record.Exception(() =>
        {
            result = headers.Get<int>("id", 42);
        });

        Assert.Null(conversionFailure);
        Assert.Equal(42, result);
        Assert.Null(headers.Get<int>("id", null));
        Assert.Equal(id, Assert.IsType<Guid>(source["id"]));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-HEADERS", "incompatible-reference-fallback-codec-cancellation-distinction")]
    public void IncompatibleReferenceType_ReturnsTheExactFallbackWhileCodecCancellationRemainsVisible()
    {
        var id = Guid.Parse("9017b4ee-33e2-4e8d-945c-b43dbbfda7d1");
        var compatible = new List<int> { 27 };
        var fallback = new List<int> { 42 };
        var source = new Dictionary<string, object>
        {
            ["id"] = id,
            ["list"] = compatible,
            ["null"] = null!,
        };
        DictionarySendHeaders headers = DictionarySendHeaders.Wrap(source);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var codecCancellation = new OperationCanceledException("unique-reference-header-codec-cancellation", null, cancellation.Token);
        var codecValue = new CodecFailureHeaderValue(codecCancellation);
        headers.Set("codec-cancellation", codecValue);

        Exception? observedCodecCancellation = Record.Exception(() =>
        {
            _ = headers.Get<List<int>>("codec-cancellation", fallback);
        });
        Assert.Same(codecCancellation, observedCodecCancellation);
        Assert.Equal(cancellation.Token, Assert.IsType<OperationCanceledException>(observedCodecCancellation).CancellationToken);
        Assert.Equal(1, codecValue.WriteCalls);
        Assert.Same(compatible, headers.Get<List<int>>("list", fallback));
        Assert.Same(fallback, headers.Get<List<int>>("missing", fallback));
        Assert.Null(headers.Get<List<int>>("missing", null));
        Assert.Same(fallback, headers.Get<List<int>>("null", fallback));
        Assert.Equal(id, headers.Get<Guid>("id", null));
        Assert.True(headers.TryGetHeader("id", out object? raw));
        Assert.Equal(id, Assert.IsType<Guid>(raw));

        List<int>? result = null;
        Exception? conversionFailure = Record.Exception(() =>
        {
            result = headers.Get<List<int>>("id", fallback);
        });

        Assert.Null(conversionFailure);
        Assert.Same(fallback, result);
        Assert.Null(headers.Get<List<int>>("id", null));
        Assert.Equal(id, Assert.IsType<Guid>(source["id"]));
    }

    [System.Text.Json.Serialization.JsonConverter(typeof(CodecFailureHeaderValueConverter))]
    public sealed class CodecFailureHeaderValue
    {
        public CodecFailureHeaderValue(Exception failure) => Failure = failure;
        public Exception Failure { get; }
        public int WriteCalls { get; set; }
    }

    public sealed class CodecFailureHeaderValueConverter : System.Text.Json.Serialization.JsonConverter<CodecFailureHeaderValue>
    {
        public override CodecFailureHeaderValue Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert,
            System.Text.Json.JsonSerializerOptions options) => throw new NotSupportedException("This fixture only records actual codec writes.");

        public override void Write(System.Text.Json.Utf8JsonWriter writer, CodecFailureHeaderValue value,
            System.Text.Json.JsonSerializerOptions options)
        {
            value.WriteCalls++;
            throw value.Failure;
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-SEND-HEADERS", "valid-incompatible-json-representations-preserve-fallback")]
    public void IncompatibleJsonRepresentations_ReturnTheFallbackAndPreserveCompatibleReads(int sourceForm)
    {
        var id = Guid.Parse("8a7d2c05-c953-4e38-a4a8-afc477bf5d08");
        string encoded = System.Text.Json.JsonSerializer.Serialize(id, ServiceBusMetadataJson.Options);
        using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(encoded);
        object represented = sourceForm switch
        {
            0 => id,
            1 => encoded,
            2 => document.RootElement.Clone(),
            _ => throw new ArgumentOutOfRangeException(nameof(sourceForm)),
        };
        var headers = new DictionarySendHeaders();
        headers.Set("id", represented);
        var fallback = new List<int> { 42 };
        Assert.Equal(id, headers.Get<Guid>("id", null));
        Assert.Equal(sourceForm == 1 ? encoded : id.ToString(), headers.Get<string>("id", null));
        headers.Set("integer-text", "27");
        Assert.Equal(27, headers.Get<int>("integer-text", 42));
        Assert.Null(headers.Get<int>("absent", null));
        Assert.Same(fallback, headers.Get<List<int>>("absent", fallback));

        int? number = null;
        Exception? valueFailure = Record.Exception(() => { number = headers.Get<int>("id", 42); });
        Assert.Null(valueFailure);
        Assert.Equal(42, number);
        Assert.Null(headers.Get<int>("id", null));
        List<int>? list = null;
        Exception? referenceFailure = Record.Exception(() => { list = headers.Get<List<int>>("id", fallback); });
        Assert.Null(referenceFailure);
        Assert.Same(fallback, list);
        Assert.Null(headers.Get<List<int>>("id", null));
        Assert.True(headers.TryGetHeader("id", out object? unchanged));
        Assert.Same(represented, unchanged);

        headers.Set("malformed", "{not-json");
        Assert.ThrowsAny<System.Text.Json.JsonException>(() => headers.Get<int>("malformed", 42));
        Assert.ThrowsAny<System.Text.Json.JsonException>(() => headers.Get<List<int>>("malformed", fallback));
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
    [RequirementCoverage("REQ-VSB-SEND-HEADERS", "custom-root-and-nested-read-failures-remain-visible")]
    public void CustomReadFailures_PreserveTheExactCauseAcrossNativeTextAndJsonHeaders(int targetShape, int failureKind)
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Exception expected = failureKind switch
        {
            0 => new IOException("unique-header-custom-read-io"),
            1 => new System.Text.Json.JsonException("unique-header-custom-read-json"),
            2 => new OperationCanceledException("unique-header-custom-read-cancellation", null, cancellation.Token),
            _ => throw new ArgumentOutOfRangeException(nameof(failureKind)),
        };
        var probe = new HeaderReadProbe();
        HeaderReadProbe? previous = HeaderReadProbe.Current.Value;
        try
        {
            HeaderReadProbe.Current.Value = probe;
            object native = targetShape switch
            {
                0 => new { Value = "actual" },
                1 => new[] { new { Value = "actual" } },
                2 => new Dictionary<string, object> { ["item"] = new { Value = "actual" } },
                _ => throw new ArgumentOutOfRangeException(nameof(targetShape)),
            };
            string encoded = System.Text.Json.JsonSerializer.Serialize(native, ServiceBusMetadataJson.Options);
            using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(encoded);
            object[] inputs = [native, encoded, document.RootElement.Clone()];
            foreach (object input in inputs)
            {
                var headers = new DictionarySendHeaders();
                headers.Set("probe", input);
                probe.Failure = null;
                probe.ReadCalls = 0;
                HeaderReadTarget healthy = targetShape switch
                {
                    0 => Assert.IsType<HeaderReadTarget>(headers.Get<HeaderReadTarget>("probe", null)),
                    1 => Assert.Single(Assert.IsType<List<HeaderReadTarget>>(headers.Get<List<HeaderReadTarget>>("probe", null))),
                    2 => Assert.Single(Assert.IsType<Dictionary<string, HeaderReadTarget>>(
                        headers.Get<Dictionary<string, HeaderReadTarget>>("probe", null))).Value,
                    _ => throw new ArgumentOutOfRangeException(nameof(targetShape)),
                };
                Assert.Equal("actual", healthy.Value);
                Assert.Equal(1, probe.ReadCalls);
                probe.Failure = expected;
                probe.ReadCalls = 0;
                Exception? observed = Record.Exception(() =>
                {
                    if (targetShape == 1)
                        _ = headers.Get<List<HeaderReadTarget>>("probe", [new HeaderReadTarget("fallback")]);
                    else if (targetShape == 2)
                        _ = headers.Get<Dictionary<string, HeaderReadTarget>>("probe",
                            new Dictionary<string, HeaderReadTarget> { ["fallback"] = new HeaderReadTarget("fallback") });
                    else
                        _ = headers.Get<HeaderReadTarget>("probe", new HeaderReadTarget("fallback"));
                });
                Assert.Same(expected, observed);
                Assert.Equal(1, probe.ReadCalls);
                if (failureKind == 2)
                    Assert.Equal(cancellation.Token, Assert.IsType<OperationCanceledException>(observed).CancellationToken);
            }
        }
        finally
        {
            HeaderReadProbe.Current.Value = previous;
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-SEND-HEADERS", "owned-dictionary-representation-fallback-preserves-valid-shapes")]
    public void OwnedDictionaryRepresentations_ReturnExactFallbackAndPreserveTheirValidShapes(int sourceForm)
    {
        var id = Guid.Parse("d2779e8e-12b6-4ef3-82ed-c7f24ae2aa8f");
        string encoded = System.Text.Json.JsonSerializer.Serialize(id, ServiceBusMetadataJson.Options);
        using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(encoded);
        object represented = sourceForm switch
        {
            0 => id,
            1 => encoded,
            2 => document.RootElement.Clone(),
            _ => throw new ArgumentOutOfRangeException(nameof(sourceForm)),
        };
        var headers = new DictionarySendHeaders();
        headers.Set("id", represented);
        var typedFallback = new Dictionary<string, int> { ["fallback"] = 42 };
        var uri = new Uri("urn:header:owned");
        var uriFallback = new Dictionary<Uri, int> { [uri] = 42 };
        var objectFallback = new Dictionary<string, object> { ["fallback"] = 42 };
        headers.Set("valid", "{\"Value\":27}");
        Dictionary<string, int> valid = Assert.IsType<Dictionary<string, int>>(headers.Get<Dictionary<string, int>>("valid", null));
        Assert.Equal(27, valid["value"]);
        Assert.Single(valid);
        headers.Set("valid-uri", "{\"urn:header:owned\":27}");
        Assert.Equal(27, Assert.Single(Assert.IsType<Dictionary<Uri, int>>(
            headers.Get<Dictionary<Uri, int>>("valid-uri", null))).Value);
        headers.Set("valid-kv", "[{\"Key\":\"item\",\"Value\":27}]");
        KeyValuePair<string, object> keyValue = Assert.Single(Assert.IsType<Dictionary<string, object>>(
            headers.Get<Dictionary<string, object>>("valid-kv", null)));
        Assert.Equal("item", keyValue.Key);
        Assert.Equal(27L, Assert.IsType<long>(keyValue.Value));

        Dictionary<string, int>? result = null;
        Exception? conversion = Record.Exception(() => { result = headers.Get<Dictionary<string, int>>("id", typedFallback); });
        Assert.Null(conversion);
        Assert.Same(typedFallback, result);
        Assert.Null(headers.Get<Dictionary<string, int>>("id", null));
        Assert.Same(uriFallback, headers.Get<Dictionary<Uri, int>>("id", uriFallback));
        Assert.Null(headers.Get<Dictionary<Uri, int>>("id", null));
        Assert.Same(objectFallback, headers.Get<Dictionary<string, object>>("id", objectFallback));
        Assert.Null(headers.Get<Dictionary<string, object>>("id", null));
        Assert.True(headers.TryGetHeader("id", out object? raw));
        Assert.Same(represented, raw);

        headers.Set("undefined", default(System.Text.Json.JsonElement));
        Assert.Throws<InvalidOperationException>(() => headers.Get<int>("undefined", 42));
        Assert.Throws<InvalidOperationException>(() => headers.Get<List<int>>("undefined", [42]));
        Assert.Throws<InvalidOperationException>(() => headers.Get<Dictionary<string, int>>("undefined", typedFallback));
    }

    private sealed class HeaderReadProbe
    {
        public static readonly AsyncLocal<HeaderReadProbe?> Current = new();
        public Exception? Failure { get; set; }
        public int ReadCalls { get; set; }
    }

    [System.Text.Json.Serialization.JsonConverter(typeof(HeaderReadTargetConverter))]
    public sealed class HeaderReadTarget(string value)
    {
        public string Value { get; } = value;
    }

    public sealed class HeaderReadTargetConverter : System.Text.Json.Serialization.JsonConverter<HeaderReadTarget>
    {
        public override HeaderReadTarget Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert,
            System.Text.Json.JsonSerializerOptions options)
        {
            HeaderReadProbe probe = HeaderReadProbe.Current.Value
                ?? throw new InvalidOperationException("The custom header read probe was not installed.");
            probe.ReadCalls++;
            if (probe.Failure is { } failure)
                throw failure;
            using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.ParseValue(ref reader);
            return new HeaderReadTarget(document.RootElement.GetProperty("value").GetString()!);
        }

        public override void Write(System.Text.Json.Utf8JsonWriter writer, HeaderReadTarget value,
            System.Text.Json.JsonSerializerOptions options) => throw new NotSupportedException("This fixture records target reads only.");
    }

    private sealed class DictionaryOnly<TKey, TValue> : IDictionary<TKey, TValue>
        where TKey : notnull
    {
        private readonly Dictionary<TKey, TValue> _items = [];

        public TValue this[TKey key] { get => _items[key]; set => _items[key] = value; }
        public ICollection<TKey> Keys => _items.Keys;
        public ICollection<TValue> Values => _items.Values;
        public int Count => _items.Count;
        public bool IsReadOnly => false;
        public void Add(TKey key, TValue value) => _items.Add(key, value);
        public void Add(KeyValuePair<TKey, TValue> item) => ((IDictionary<TKey, TValue>)_items).Add(item);
        public void Clear() => _items.Clear();
        public bool Contains(KeyValuePair<TKey, TValue> item) => ((IDictionary<TKey, TValue>)_items).Contains(item);
        public bool ContainsKey(TKey key) => _items.ContainsKey(key);
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) =>
            ((IDictionary<TKey, TValue>)_items).CopyTo(array, arrayIndex);
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _items.GetEnumerator();
        public bool Remove(TKey key) => _items.Remove(key);
        public bool Remove(KeyValuePair<TKey, TValue> item) => ((IDictionary<TKey, TValue>)_items).Remove(item);
        public bool TryGetValue(TKey key, out TValue value) => _items.TryGetValue(key, out value!);
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

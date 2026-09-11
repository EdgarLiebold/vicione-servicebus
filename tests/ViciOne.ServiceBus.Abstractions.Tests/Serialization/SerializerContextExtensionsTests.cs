using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Serialization;

public sealed class SerializerContextExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SERIALIZER-CONTEXT-EXTENSIONS", "dictionary-and-header-provider-conversion")]
    public void GetValueOverloads_ResolveExactAndCamelCaseKeysAndPreserveDefaults()
    {
        var recorder = new DeserializerRecorder
        {
            Deserialize = static (source, targetType, fallback) => targetType == typeof(string)
                ? $"converted:{source}"
                : int.TryParse(source?.ToString(), out int value) ? value : fallback,
        };
        IObjectDeserializer deserializer = recorder.CreateObjectDeserializer();
        IDictionary<string, object> mutable = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["referenceValue"] = "mutable",
            ["Count"] = "17",
        };
        IReadOnlyDictionary<string, object> readOnly = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["ReferenceValue"] = "read-only",
            ["count"] = "29",
        };
        IHeaderProvider headers = new HeaderProvider(("ReferenceValue", "header"), ("Count", "41"));

        Assert.Equal("converted:mutable", deserializer.GetValue<string>(mutable, "ReferenceValue"));
        Assert.Equal(17, deserializer.GetValue<int>(mutable, "Count"));
        Assert.Equal("converted:read-only", deserializer.GetValue<string>(readOnly, "ReferenceValue"));
        Assert.Equal(29, deserializer.GetValue<int>(readOnly, "Count"));
        Assert.Equal("converted:header", deserializer.GetValue<string>(headers, "ReferenceValue"));
        Assert.Equal(41, deserializer.GetValue<int>(headers, "Count"));

        int callsBeforeMissingValues = recorder.DeserializeCalls;
        Assert.Equal("reference-default", deserializer.GetValue(mutable, "Missing", "reference-default"));
        Assert.Equal(73, deserializer.GetValue(readOnly, "Missing", (int?)73));
        Assert.Equal("header-default", deserializer.GetValue(headers, "Missing", "header-default"));
        Assert.Equal(91, deserializer.GetValue(headers, "Missing", (int?)91));
        Assert.Equal(callsBeforeMissingValues, recorder.DeserializeCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SERIALIZER-CONTEXT-EXTENSIONS", "try-get-dictionary-conversion")]
    public void TryGetValueOverloads_ReportKeyAndConversionOutcomesExactly()
    {
        var recorder = new DeserializerRecorder
        {
            Deserialize = static (source, targetType, _) => source?.ToString() == "unconvertible"
                ? null
                : targetType == typeof(string) ? source?.ToString() : int.Parse(source!.ToString()!),
        };
        IObjectDeserializer deserializer = recorder.CreateObjectDeserializer();
        IDictionary<string, object> values = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["referenceValue"] = "text",
            ["count"] = "37",
            ["RejectedReference"] = "unconvertible",
            ["RejectedCount"] = "unconvertible",
        };

        Assert.True(deserializer.TryGetValue(values, "ReferenceValue", out string? reference));
        Assert.Equal("text", reference);
        Assert.True(deserializer.TryGetValue(values, "Count", out int? count));
        Assert.Equal(37, count);
        Assert.False(deserializer.TryGetValue(values, "Missing", out string? missingReference));
        Assert.Null(missingReference);
        Assert.False(deserializer.TryGetValue(values, "Missing", out int? missingCount));
        Assert.Null(missingCount);
        Assert.False(deserializer.TryGetValue(values, "RejectedReference", out string? rejectedReference));
        Assert.Null(rejectedReference);
        Assert.False(deserializer.TryGetValue(values, "RejectedCount", out int? rejectedCount));
        Assert.Null(rejectedCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SERIALIZER-CONTEXT-EXTENSIONS", "dictionary-serialization")]
    public void SerializeDictionary_FiltersNullValuesUsesCaseInsensitiveLastValueAndRejectsMissingKeys()
    {
        var recorder = new DeserializerRecorder();
        IObjectDeserializer deserializer = recorder.CreateObjectDeserializer();
        KeyValuePair<string, object>[] values =
        [
            new("Name", "first"),
            new("name", "second"),
            new(" ", null!),
        ];

        Assert.Equal("serialized", deserializer.SerializeDictionary(values));
        var serialized = Assert.IsType<Dictionary<string, object>>(recorder.SerializedValue);
        Assert.Single(serialized);
        Assert.Equal("second", serialized["NAME"]);

        int callsBeforeEmptyDictionary = recorder.SerializeCalls;
        Assert.Null(deserializer.SerializeDictionary([new KeyValuePair<string, object>("Ignored", null!)]));
        Assert.Equal(callsBeforeEmptyDictionary, recorder.SerializeCalls);

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            deserializer.SerializeDictionary([new KeyValuePair<string, object>(" ", 1)]));
        Assert.Equal("values", exception.ParamName);
        Assert.Equal(callsBeforeEmptyDictionary, recorder.SerializeCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SERIALIZER-CONTEXT-EXTENSIONS", "dictionary-deserialization")]
    public void DeserializeDictionary_NormalizesEmptyInputAndUsesCaseInsensitiveLastValue()
    {
        var recorder = new DeserializerRecorder
        {
            Deserialize = static (source, _, _) => source?.ToString() switch
            {
                "empty" => Array.Empty<KeyValuePair<string, int>>(),
                "values" => new KeyValuePair<string, int>[]
                {
                    new("Count", 17),
                    new("count", 29),
                },
                "invalid" => new KeyValuePair<string, int>[] { new(" ", 1) },
                _ => null,
            },
        };
        IObjectDeserializer deserializer = recorder.CreateObjectDeserializer();

        Assert.Null(deserializer.DeserializeDictionary<int>(null));
        Assert.Null(deserializer.DeserializeDictionary<int>(""));
        Assert.Null(deserializer.DeserializeDictionary<int>("   "));
        Assert.Equal(0, recorder.DeserializeCalls);
        Assert.Null(deserializer.DeserializeDictionary<int>("missing"));
        Assert.Null(deserializer.DeserializeDictionary<int>("empty"));

        Dictionary<string, int> values = Assert.IsType<Dictionary<string, int>>(
            deserializer.DeserializeDictionary<int>("values"));
        Assert.Single(values);
        Assert.Equal(29, values["COUNT"]);

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            deserializer.DeserializeDictionary<int>("invalid"));
        Assert.Equal("text", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SERIALIZER-CONTEXT-EXTENSIONS", "consume-header-conversion")]
    public void ConsumeHeaderExtensions_ConvertPresentValuesAndPreserveMissingOrRejectedDefaults()
    {
        var recorder = new DeserializerRecorder
        {
            Deserialize = static (source, targetType, fallback) => source?.ToString() == "rejected"
                ? fallback
                : targetType == typeof(string) ? $"converted:{source}" : int.Parse(source!.ToString()!),
        };
        SerializerContext serializerContext = recorder.CreateSerializerContext();
        Headers headers = CreateHeaders<Headers>(
            ("DirectText", "direct"),
            ("Reference", 27),
            ("Count", "31"),
            ("Rejected", new RejectedValue()));
        ConsumeContext context = CreateConsumeContext(headers, serializerContext);

        Assert.Equal("direct", context.GetHeader("DirectText"));
        int callsAfterDirectText = recorder.DeserializeCalls;
        Assert.Equal("converted:27", context.GetHeader("Reference"));
        Assert.Equal("converted:27", context.GetHeader<string>("Reference"));
        Assert.Equal(31, context.GetHeader<int>("Count"));
        Assert.True(context.TryGetHeader("Reference", out string? reference));
        Assert.Equal("converted:27", reference);
        Assert.True(context.TryGetHeader("Count", out int? count));
        Assert.Equal(31, count);
        Assert.Equal(callsAfterDirectText + 5, recorder.DeserializeCalls);

        Assert.Equal("missing", context.GetHeader("Missing", "missing"));
        Assert.Equal("missing", context.GetHeader("Rejected", "missing"));
        Assert.Equal(47, context.GetHeader("Missing", (int?)47));
        Assert.Equal(47, context.GetHeader("Rejected", (int?)47));
        Assert.False(context.TryGetHeader("Missing", out string? missing));
        Assert.Null(missing);
        Assert.False(context.TryGetHeader("Rejected", out string? rejected));
        Assert.Null(rejected);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SERIALIZER-CONTEXT-EXTENSIONS", "send-header-typed-read")]
    public void SendHeaderExtensions_RequireTheStoredRuntimeTypeWithoutDeserialization()
    {
        SendHeaders headers = CreateHeaders<SendHeaders>(
            ("Reference", "text"),
            ("Count", 43),
            ("Convertible", "47"));
        SendContext context = CreateSendContext(headers);

        Assert.True(context.TryGetHeader("Reference", out string? reference));
        Assert.Equal("text", reference);
        Assert.True(context.TryGetHeader("Count", out int? count));
        Assert.Equal(43, count);
        Assert.False(context.TryGetHeader("Convertible", out int? convertible));
        Assert.Null(convertible);
        Assert.False(context.TryGetHeader("Missing", out string? missing));
        Assert.Null(missing);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SERIALIZER-CONTEXT-EXTENSIONS", "consume-object-projection")]
    public void ToDictionary_DelegatesTheExactValueToTheConsumeSerializerContext()
    {
        var expected = new Dictionary<string, object> { ["value"] = 59 };
        object? projected = null;
        var recorder = new DeserializerRecorder
        {
            Project = value =>
            {
                projected = value;
                return expected;
            },
        };
        var source = new SourceValue(59);
        ConsumeContext context = CreateConsumeContext(CreateHeaders<Headers>(), recorder.CreateSerializerContext());

        Dictionary<string, object> actual = context.ToDictionary(source);

        Assert.Same(source, projected);
        Assert.Same(expected, actual);
        Assert.Equal(1, recorder.ProjectCalls);
    }

    private static ConsumeContext CreateConsumeContext(Headers headers, SerializerContext serializerContext) =>
        CreateProxy<ConsumeContext>((method, _) => method.Name switch
        {
            "get_Headers" => headers,
            "get_SerializerContext" => serializerContext,
            _ => throw Unexpected(method),
        });

    private static SendContext CreateSendContext(SendHeaders headers) =>
        CreateProxy<SendContext>((method, _) => method.Name == "get_Headers"
            ? headers
            : throw Unexpected(method));

    private static THeaders CreateHeaders<THeaders>(params (string Key, object Value)[] values)
        where THeaders : class
    {
        var dictionary = values.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        return CreateProxy<THeaders>((method, arguments) =>
        {
            if (method.Name != nameof(Headers.TryGetHeader))
                throw Unexpected(method);

            bool found = dictionary.TryGetValue((string)arguments[0]!, out object? value);
            arguments[1] = value;
            return found;
        });
    }

    private static TContract CreateProxy<TContract>(Func<MethodInfo, object?[], object?> invoke)
        where TContract : class
    {
        TContract contract = DispatchProxy.Create<TContract, InvocationProxy>();
        ((InvocationProxy)(object)contract).InvokeMember = invoke;
        return contract;
    }

    private static InvalidOperationException Unexpected(MethodInfo method) =>
        new($"Unexpected invocation of '{method.DeclaringType?.Name}.{method.Name}'.");

    private sealed record SourceValue(int Value);

    private sealed record RejectedValue
    {
        public override string ToString() => "rejected";
    }

    private sealed class HeaderProvider(params (string Key, object Value)[] values) : IHeaderProvider
    {
        private readonly Dictionary<string, object> _values =
            values.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

        public IEnumerable<KeyValuePair<string, object>> GetAll() => _values;

        public bool TryGetHeader(string key, [NotNullWhen(true)] out object? value) => _values.TryGetValue(key, out value);
    }

    private sealed class DeserializerRecorder
    {
        public Func<object?, Type, object?, object?> Deserialize { get; init; } = static (_, _, fallback) => fallback;
        public Func<object, Dictionary<string, object>> Project { get; init; } =
            static _ => throw new InvalidOperationException("No object projection was expected.");
        public int DeserializeCalls { get; private set; }
        public int ProjectCalls { get; private set; }
        public int SerializeCalls { get; private set; }
        public object? SerializedValue { get; private set; }

        public IObjectDeserializer CreateObjectDeserializer() => CreateProxy<IObjectDeserializer>(Invoke);

        public SerializerContext CreateSerializerContext() => CreateProxy<SerializerContext>(Invoke);

        private object? Invoke(MethodInfo method, object?[] arguments)
        {
            if (method.Name == nameof(IObjectDeserializer.DeserializeObject))
            {
                DeserializeCalls++;
                return Deserialize(arguments[0], method.GetGenericArguments()[0], arguments[1]);
            }

            if (method.Name == nameof(IObjectDeserializer.SerializeObject))
            {
                SerializeCalls++;
                SerializedValue = arguments[0];
                return new StringMessageBody("serialized");
            }

            if (method.Name == nameof(SerializerContext.ToDictionary))
            {
                ProjectCalls++;
                return Project(arguments[0]!);
            }

            throw Unexpected(method);
        }
    }

    private class InvocationProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[], object?> InvokeMember { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? arguments) =>
            InvokeMember(
                targetMethod ?? throw new InvalidOperationException("The proxy invocation did not provide a method."),
                arguments ?? []);
    }
}

using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Serialization;

public sealed class SerializerContextExtensionsBoundaryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SERIALIZER-CONTEXT-EXTENSIONS", "public-extensions-reject-invalid-required-inputs")]
    public void PublicExtensions_RejectNullCollaboratorsAndMissingKeys()
    {
        IObjectDeserializer deserializer = DispatchProxy.Create<IObjectDeserializer, UnexpectedInvocationProxy>();
        IHeaderProvider headers = DispatchProxy.Create<IHeaderProvider, UnexpectedInvocationProxy>();
        ConsumeContext consumeContext = DispatchProxy.Create<ConsumeContext, UnexpectedInvocationProxy>();
        SendContext sendContext = DispatchProxy.Create<SendContext, UnexpectedInvocationProxy>();
        var dictionary = new Dictionary<string, object>();
        IReadOnlyDictionary<string, object> readOnlyDictionary = dictionary;

        AssertParameter("context", () => SerializerContextExtensions.GetValue<string>(null!, readOnlyDictionary, "key"));
        AssertParameter("context", () => SerializerContextExtensions.GetValue<int>(null!, readOnlyDictionary, "key", default(int?)));
        AssertParameter("dictionary", () => deserializer.GetValue<string>((IReadOnlyDictionary<string, object>)null!, "key"));
        AssertParameter("dictionary", () => deserializer.GetValue<int>((IReadOnlyDictionary<string, object>)null!, "key", default(int?)));
        AssertMissingKey(() => deserializer.GetValue<string>(readOnlyDictionary, " "));
        AssertMissingKey(() => deserializer.GetValue<int>(readOnlyDictionary, " ", default(int?)));

        AssertParameter("context", () => SerializerContextExtensions.GetValue<string>(null!, (IDictionary<string, object>)dictionary, "key"));
        AssertParameter("context", () => SerializerContextExtensions.GetValue<int>(null!, (IDictionary<string, object>)dictionary, "key", default(int?)));
        AssertParameter("dictionary", () => deserializer.GetValue<string>((IDictionary<string, object>)null!, "key"));
        AssertParameter("dictionary", () => deserializer.GetValue<int>((IDictionary<string, object>)null!, "key", default(int?)));
        AssertMissingKey(() => deserializer.GetValue<string>((IDictionary<string, object>)dictionary, " "));
        AssertMissingKey(() => deserializer.GetValue<int>((IDictionary<string, object>)dictionary, " ", default(int?)));

        AssertParameter("context", () => SerializerContextExtensions.GetValue<string>(null!, headers, "key"));
        AssertParameter("context", () => SerializerContextExtensions.GetValue<int>(null!, headers, "key", default(int?)));
        AssertParameter("headers", () => deserializer.GetValue<string>((IHeaderProvider)null!, "key"));
        AssertParameter("headers", () => deserializer.GetValue<int>((IHeaderProvider)null!, "key", default(int?)));
        AssertMissingKey(() => deserializer.GetValue<string>(headers, " "));
        AssertMissingKey(() => deserializer.GetValue<int>(headers, " ", default(int?)));

        AssertParameter("context", () => SerializerContextExtensions.TryGetValue<string>(null!, dictionary, "key", out _));
        AssertParameter("context", () => SerializerContextExtensions.TryGetValue<int>(null!, dictionary, "key", out _));
        AssertParameter("dictionary", () => deserializer.TryGetValue<string>(null!, "key", out _));
        AssertParameter("dictionary", () => deserializer.TryGetValue<int>(null!, "key", out _));
        AssertMissingKey(() => deserializer.TryGetValue<string>(dictionary, " ", out _));
        AssertMissingKey(() => deserializer.TryGetValue<int>(dictionary, " ", out _));

        AssertParameter("deserializer", () => SerializerContextExtensions.SerializeDictionary(null!, dictionary));
        AssertParameter("values", () => deserializer.SerializeDictionary(null!));
        AssertParameter("deserializer", () => SerializerContextExtensions.DeserializeDictionary<string>(null!, null));

        AssertParameter("context", () => SerializerContextExtensions.TryGetHeader<string>((ConsumeContext)null!, "key", out _));
        AssertParameter("context", () => SerializerContextExtensions.TryGetHeader<int>((ConsumeContext)null!, "key", out _));
        AssertMissingKey(() => consumeContext.TryGetHeader<string>(" ", out _));
        AssertMissingKey(() => consumeContext.TryGetHeader<int>(" ", out _));
        AssertParameter("context", () => SerializerContextExtensions.TryGetHeader<string>((SendContext)null!, "key", out _));
        AssertParameter("context", () => SerializerContextExtensions.TryGetHeader<int>((SendContext)null!, "key", out _));
        AssertMissingKey(() => sendContext.TryGetHeader<string>(" ", out _));
        AssertMissingKey(() => sendContext.TryGetHeader<int>(" ", out _));

        AssertParameter("context", () => SerializerContextExtensions.GetHeader(null!, "key"));
        AssertParameter("context", () => SerializerContextExtensions.GetHeader<string>(null!, "key"));
        AssertParameter("context", () => SerializerContextExtensions.GetHeader<int>(null!, "key", default(int?)));
        AssertMissingKey(() => consumeContext.GetHeader(" "));
        AssertMissingKey(() => consumeContext.GetHeader<string>(" "));
        AssertMissingKey(() => consumeContext.GetHeader<int>(" ", default(int?)));

        AssertParameter("context", () => SerializerContextExtensions.ToDictionary(null!, new SourceValue()));
        AssertParameter("value", () => consumeContext.ToDictionary<SourceValue>(null!));
    }

    private static void AssertParameter(string expectedParameterName, Action operation)
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(operation);

        Assert.Equal(expectedParameterName, exception.ParamName);
    }

    private static void AssertMissingKey(Action operation)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(operation);

        Assert.Equal("key", exception.ParamName);
    }

    private sealed record SourceValue;

    private class UnexpectedInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"The invalid boundary invoked {targetMethod?.Name}.");
    }
}

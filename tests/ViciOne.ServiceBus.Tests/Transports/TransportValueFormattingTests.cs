using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports;

public sealed class TransportValueFormattingTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-ADDRESS-IDENTITY", "component-equality-and-hash-contract")]
    public void EndpointAddressComparer_UsesTransportIdentityComponentsConsistently()
    {
        var first = new Uri("rabbitmq://Broker.example:5672/Orders?temporary=true#first");
        var equivalent = new Uri("RABBITMQ://broker.EXAMPLE:5672/Orders?durable=true#second");
        var differentPathCase = new Uri("rabbitmq://broker.example:5672/orders");

        Assert.True(EndpointAddressComparer.Instance.Equals(first, equivalent));
        Assert.Equal(
            EndpointAddressComparer.Instance.GetHashCode(first),
            EndpointAddressComparer.Instance.GetHashCode(equivalent));
        Assert.False(EndpointAddressComparer.Instance.Equals(first, differentPathCase));
        Assert.False(EndpointAddressComparer.Instance.Equals(first, null));
        Assert.True(EndpointAddressComparer.Instance.Equals(null, null));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-ADDRESS-IDENTITY", "relative-address-and-null-boundaries")]
    public void EndpointAddressComparer_HandlesRelativeAddressesAndRejectsANullHashOperand()
    {
        var first = new Uri("Orders/priority", UriKind.Relative);
        var equivalent = new Uri("Orders/priority", UriKind.Relative);
        var differentCase = new Uri("orders/priority", UriKind.Relative);

        Assert.True(EndpointAddressComparer.Instance.Equals(first, equivalent));
        Assert.Equal(
            EndpointAddressComparer.Instance.GetHashCode(first),
            EndpointAddressComparer.Instance.GetHashCode(equivalent));
        Assert.False(EndpointAddressComparer.Instance.Equals(first, differentCase));
        Assert.Throws<ArgumentNullException>(() => EndpointAddressComparer.Instance.GetHashCode(null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-NAME-FORMATTING", "separator-and-type-validation")]
    public void DefaultMessageNameFormatter_RejectsEveryMissingSeparatorAndType()
    {
        Assert.Equal(
            "genericArgumentSeparator",
            Assert.Throws<ArgumentNullException>(() => new DefaultMessageNameFormatter(null!, "--", ":", "-")).ParamName);
        Assert.Equal(
            "genericTypeSeparator",
            Assert.Throws<ArgumentNullException>(() => new DefaultMessageNameFormatter("::", null!, ":", "-")).ParamName);
        Assert.Equal(
            "namespaceSeparator",
            Assert.Throws<ArgumentNullException>(() => new DefaultMessageNameFormatter("::", "--", null!, "-")).ParamName);
        Assert.Equal(
            "nestedTypeSeparator",
            Assert.Throws<ArgumentNullException>(() => new DefaultMessageNameFormatter("::", "--", ":", null!)).ParamName);

        var formatter = new DefaultMessageNameFormatter("::", "--", ":", "-");
        Assert.Equal("type", Assert.Throws<ArgumentNullException>(() => formatter.GetMessageName(null!)).ParamName);
        Assert.Equal("type", Assert.Throws<ArgumentException>(() => formatter.GetMessageName(typeof(GenericMessage<>))).ParamName);
        Type genericParameter = typeof(GenericMessage<>).GetGenericArguments()[0];
        Type partiallyOpenType = typeof(GenericPair<,>).MakeGenericType(typeof(SampleMessage), genericParameter);
        Assert.Equal("type", Assert.Throws<ArgumentException>(() => formatter.GetMessageName(partiallyOpenType)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-NAME-FORMATTING", "namespace-nesting-and-cache-identity")]
    public void DefaultMessageNameFormatter_FormatsNestedTypesAndCachesTheResult()
    {
        var formatter = new DefaultMessageNameFormatter("::", "--", ":", "-");
        string expected = $"{typeof(TransportValueFormattingTests).Namespace}:TransportValueFormattingTests-SampleMessage";

        string first = formatter.GetMessageName(typeof(SampleMessage));
        string second = formatter.GetMessageName(typeof(SampleMessage));

        Assert.Equal(expected, first);
        Assert.Same(first, second);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-NAME-FORMATTING", "closed-generics-and-namespace-policy")]
    public void DefaultMessageNameFormatter_FormatsOnlyOwnedGenericArgumentsAndHonorsNamespacePolicy()
    {
        var qualified = new DefaultMessageNameFormatter("::", "--", ":", "-");
        var unqualified = new DefaultMessageNameFormatter("::", "--", ":", "-", includeNamespace: false);
        Type messageType = typeof(GenericContainer<SampleMessage>.Nested<string>);

        Assert.Equal(
            $"{typeof(TransportValueFormattingTests).Namespace}:TransportValueFormattingTests-GenericContainer--TransportValueFormattingTests-SampleMessage---Nested--System:String--",
            qualified.GetMessageName(messageType));
        Assert.Equal(
            "TransportValueFormattingTests-GenericContainer--TransportValueFormattingTests-SampleMessage---Nested--String--",
            unqualified.GetMessageName(messageType));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TRANSPORT-KEY-FORMATTING", "delegate-contract-boundaries")]
    public void DelegateKeyFormatters_ValidateDependenciesContextsAndResults()
    {
        Assert.Equal(
            "formatter",
            Assert.Throws<ArgumentNullException>(() => new DelegatePartitionKeyFormatter<SampleMessage>(null!)).ParamName);
        Assert.Equal(
            "formatter",
            Assert.Throws<ArgumentNullException>(() => new DelegateRoutingKeyFormatter<SampleMessage>(null!)).ParamName);

        var partition = new DelegatePartitionKeyFormatter<SampleMessage>(_ => "partition-a");
        var routing = new DelegateRoutingKeyFormatter<SampleMessage>(_ => "route-a");
        var context = new MessageSendContext<SampleMessage>(new SampleMessage());

        Assert.Equal("partition-a", partition.FormatPartitionKey(context));
        Assert.Equal("route-a", routing.FormatRoutingKey(context));
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => partition.FormatPartitionKey(null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => routing.FormatRoutingKey(null!)).ParamName);
        Assert.Throws<InvalidOperationException>(() =>
            new DelegatePartitionKeyFormatter<SampleMessage>(_ => null!).FormatPartitionKey(context));
        Assert.Throws<InvalidOperationException>(() =>
            new DelegateRoutingKeyFormatter<SampleMessage>(_ => null!).FormatRoutingKey(context));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TRANSPORT-KEY-FORMATTING", "generic-adapter-contract-boundaries")]
    public void MessageKeyFormatterAdapters_ValidateDependenciesContextsAndResults()
    {
        Assert.Equal(
            "formatter",
            Assert.Throws<ArgumentNullException>(() => new MessagePartitionKeyFormatter<SampleMessage>(null!)).ParamName);
        Assert.Equal(
            "formatter",
            Assert.Throws<ArgumentNullException>(() => new MessageRoutingKeyFormatter<SampleMessage>(null!)).ParamName);

        var context = new MessageSendContext<SampleMessage>(new SampleMessage());
        var partition = new MessagePartitionKeyFormatter<SampleMessage>(new PartitionKeyFormatter("partition-b"));
        var routing = new MessageRoutingKeyFormatter<SampleMessage>(new RoutingKeyFormatter("route-b"));

        Assert.Equal("partition-b", partition.FormatPartitionKey(context));
        Assert.Equal("route-b", routing.FormatRoutingKey(context));
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => partition.FormatPartitionKey(null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => routing.FormatRoutingKey(null!)).ParamName);
        Assert.Throws<InvalidOperationException>(() =>
            new MessagePartitionKeyFormatter<SampleMessage>(new PartitionKeyFormatter(null!)).FormatPartitionKey(context));
        Assert.Throws<InvalidOperationException>(() =>
            new MessageRoutingKeyFormatter<SampleMessage>(new RoutingKeyFormatter(null!)).FormatRoutingKey(context));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JSON-TRANSPORT-HEADERS", "provider-and-key-boundaries")]
    public void JsonTransportHeaders_ExposeProviderValuesAndValidateRequiredInputs()
    {
        Assert.Equal(
            "provider",
            Assert.Throws<ArgumentNullException>(() => new JsonTransportHeaders(null!)).ParamName);

        var values = new Dictionary<string, object>
        {
            ["attempt"] = 3,
            ["name"] = "priority",
        };
        var headers = new JsonTransportHeaders(new DictionaryHeaderProvider(values));

        Assert.Equal(values, headers.GetAll());
        Assert.True(headers.TryGetHeader("attempt", out object? attempt));
        Assert.Equal(3, attempt);
        Assert.Equal("priority", headers.Get("name", "fallback"));
        Assert.Equal(3, headers.Get<int>("attempt", (int?)0));
        Assert.Equal(2, headers.Count());
        Assert.Equal("key", Assert.Throws<ArgumentNullException>(() => headers.TryGetHeader(null!, out _)).ParamName);
        Assert.Equal("key", Assert.Throws<ArgumentException>(() => headers.TryGetHeader(" ", out _)).ParamName);
        Assert.Equal("key", Assert.Throws<ArgumentException>(() => headers.Get(" ", "fallback")).ParamName);
        Assert.Equal("key", Assert.Throws<ArgumentException>(() => headers.Get<int>(" ", 0)).ParamName);

        var nullValueHeaders = new JsonTransportHeaders(new NullValueHeaderProvider());
        Assert.False(nullValueHeaders.TryGetHeader("invalid", out object? missing));
        Assert.Null(missing);

        var nullSequenceHeaders = new JsonTransportHeaders(new NullSequenceHeaderProvider());
        Assert.Throws<InvalidOperationException>(() => nullSequenceHeaders.GetAll());
        Assert.Throws<InvalidOperationException>(() => nullSequenceHeaders.Count());
    }

    private sealed class SampleMessage;
    private sealed class GenericMessage<T>;
    private sealed class GenericPair<TFirst, TSecond>;

    private sealed class GenericContainer<TOuter>
    {
        public sealed class Nested<TInner>;
    }

    private sealed class PartitionKeyFormatter(string value) : IPartitionKeyFormatter
    {
        public string FormatPartitionKey<T>(SendContext<T> context)
            where T : class => value;
    }

    private sealed class RoutingKeyFormatter(string value) : IRoutingKeyFormatter
    {
        public string FormatRoutingKey<T>(SendContext<T> context)
            where T : class => value;
    }

    private sealed class NullValueHeaderProvider : IHeaderProvider
    {
        public IEnumerable<KeyValuePair<string, object>> GetAll() =>
            [new KeyValuePair<string, object>("invalid", null!)];

        public bool TryGetHeader(string key, out object value)
        {
            value = null!;
            return true;
        }
    }

    private sealed class NullSequenceHeaderProvider : IHeaderProvider
    {
        public IEnumerable<KeyValuePair<string, object>> GetAll() => null!;

        public bool TryGetHeader(string key, out object value)
        {
            value = null!;
            return false;
        }
    }
}

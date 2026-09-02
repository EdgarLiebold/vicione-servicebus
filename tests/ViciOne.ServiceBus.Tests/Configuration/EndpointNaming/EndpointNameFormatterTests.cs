using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration.EndpointNaming;

public sealed class EndpointNameFormatterTests
{
    [Theory]
    [InlineData(ConsumerNameShape.Words, "some_really_cool")]
    [InlineData(ConsumerNameShape.Digit, "one_or2_message")]
    [InlineData(ConsumerNameShape.Acronym, "some_super_idformat")]
    [RequirementCoverage("REQ-VSB-ENDPOINT-NAME-SNAKE-CASE", "word-digit-acronym-boundaries")]
    public void SnakeCaseConsumerName_PreservesWordDigitAndAcronymBoundaries(
        ConsumerNameShape shape,
        string expected)
    {
        var formatter = SnakeCaseEndpointNameFormatter.Instance;

        var actual = shape switch
        {
            ConsumerNameShape.Words => formatter.Consumer<SomeReallyCoolConsumer>(),
            ConsumerNameShape.Digit => formatter.Consumer<OneOr2MessageConsumer>(),
            ConsumerNameShape.Acronym => formatter.Consumer<SomeSuperIDFormatConsumer>(),
            _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, null),
        };

        Assert.Equal(expected, actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-NAME-NAMESPACE", "nested-consumer")]
    public void KebabCaseConsumerName_IncludesNestedNamespace()
    {
        var formatter = new KebabCaseEndpointNameFormatter(includeNamespace: true);

        var actual = formatter.Consumer<SomeReallyCoolConsumer>();

        Assert.Equal(
            "vici-one-service-bus-tests-configuration-endpoint-naming-endpoint-name-formatter-tests-some-really-cool",
            actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-NAME-PREFIX", "namespace-plus-prefix")]
    public void KebabCaseConsumerName_PrependsPrefixToNestedNamespace()
    {
        var formatter = new KebabCaseEndpointNameFormatter("Dev", includeNamespace: true);

        var actual = formatter.Consumer<SomeReallyCoolConsumer>();

        Assert.Equal(
            "dev-vici-one-service-bus-tests-configuration-endpoint-naming-endpoint-name-formatter-tests-some-really-cool",
            actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-NAME-GENERIC-CONSUMER", "message-type-owner")]
    public void GenericConsumerName_UsesItsMessageType()
    {
        var formatter = new KebabCaseEndpointNameFormatter("Dev", includeNamespace: true);

        string first = formatter.Consumer<GenericConsumer<MarkerType, EndpointMessage>>();
        string second = formatter.Consumer<GenericConsumer<MarkerType, AlternateEndpointMessage>>();

        Assert.Equal(
            "dev-vici-one-service-bus-tests-configuration-endpoint-naming-endpoint-name-formatter-tests-endpoint-message",
            first);
        Assert.Equal(
            "dev-vici-one-service-bus-tests-configuration-endpoint-naming-endpoint-name-formatter-tests-alternate-endpoint-message",
            second);
        Assert.NotEqual(first, second);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-NAME-MESSAGE", "namespace-plus-prefix")]
    public void MessageName_IncludesNestedNamespaceAndPrefix()
    {
        var formatter = new KebabCaseEndpointNameFormatter("Dev", includeNamespace: true);

        var actual = formatter.Message<EndpointMessage>();

        Assert.Equal(
            "dev-vici-one-service-bus-tests-configuration-endpoint-naming-endpoint-name-formatter-tests-endpoint-message",
            actual);
    }

    [Theory]
    [InlineData(PrefixShape.Kebab, "dev-some-really-cool")]
    [InlineData(PrefixShape.Default, "DevSomeReallyCool")]
    [InlineData(PrefixShape.DefaultWithSeparator, "Dev-SomeReallyCool")]
    [InlineData(PrefixShape.SnakeWithSeparator, "dev-some_really_cool")]
    [RequirementCoverage("REQ-VSB-ENDPOINT-NAME-PREFIX", "formatter-separator-rules")]
    public void ConsumerName_PreservesEachFormatterPrefixRule(PrefixShape shape, string expected)
    {
        var actual = shape switch
        {
            PrefixShape.Kebab => new KebabCaseEndpointNameFormatter("Dev", includeNamespace: false)
                .Consumer<SomeReallyCoolConsumer>(),
            PrefixShape.Default => new DefaultEndpointNameFormatter("Dev", includeNamespace: false)
                .Consumer<SomeReallyCoolConsumer>(),
            PrefixShape.DefaultWithSeparator => new DefaultEndpointNameFormatter("Dev-", includeNamespace: false)
                .Consumer<SomeReallyCoolConsumer>(),
            PrefixShape.SnakeWithSeparator => new SnakeCaseEndpointNameFormatter("Dev-", includeNamespace: false)
                .Consumer<SomeReallyCoolConsumer>(),
            _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, null),
        };

        Assert.Equal(expected, actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-NAME-MESSAGE", "kebab-without-prefix")]
    public void DefaultKebabMessageName_ContainsOnlyTheMessageType()
    {
        var actual = KebabCaseEndpointNameFormatter.Instance.Message<EndpointMessage>();

        Assert.Equal("endpoint-message", actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-NAME-INSTANCE", "kebab-instance-id")]
    public void ConsumerEndpointDefinition_AppendsSanitizedInstanceId()
    {
        var settings = new EndpointSettings<IEndpointDefinition<SomeReallyCoolConsumer>>
        {
            InstanceId = "TopShelf",
        };
        var definition = new ConsumerEndpointDefinition<SomeReallyCoolConsumer>(settings);

        var actual = definition.GetEndpointName(KebabCaseEndpointNameFormatter.Instance);

        Assert.Equal("some-really-cool-top-shelf", actual);
    }

    [Theory]
    [InlineData(ReservedTypeShape.Consumer)]
    [InlineData(ReservedTypeShape.Saga)]
    [InlineData(ReservedTypeShape.ExecuteActivity)]
    [InlineData(ReservedTypeShape.CompensateActivity)]
    [RequirementCoverage("REQ-VSB-ENDPOINT-NAME-RESERVED-TYPE", "suffix-only-type")]
    public void SuffixOnlyEndpointType_IsRejected(ReservedTypeShape shape)
    {
        var formatter = DefaultEndpointNameFormatter.Instance;

        Action format = shape switch
        {
            ReservedTypeShape.Consumer => () => formatter.Consumer<Consumer>(),
            ReservedTypeShape.Saga => () => formatter.Saga<Saga>(),
            ReservedTypeShape.ExecuteActivity => () => formatter.ExecuteActivity<Activity, EndpointMessage>(),
            ReservedTypeShape.CompensateActivity => () => formatter.CompensateActivity<Activity, EndpointMessage>(),
            _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, null),
        };

        var exception = Assert.Throws<ConfigurationException>(format);

        Assert.Contains("may not be named", exception.Message, StringComparison.Ordinal);
        Assert.Contains("meaningful prefix", exception.Message, StringComparison.Ordinal);
    }

    public enum ConsumerNameShape
    {
        Words,
        Digit,
        Acronym,
    }

    public enum PrefixShape
    {
        Kebab,
        Default,
        DefaultWithSeparator,
        SnakeWithSeparator,
    }

    public enum ReservedTypeShape
    {
        Consumer,
        Saga,
        ExecuteActivity,
        CompensateActivity,
    }

    private sealed record EndpointMessage;

    private sealed record AlternateEndpointMessage;

    private sealed class MarkerType
    {
    }

    private sealed class SomeReallyCoolConsumer : IConsumer<EndpointMessage>
    {
        public Task Consume(ConsumeContext<EndpointMessage> context) => Task.CompletedTask;
    }

    private sealed class SomeSuperIDFormatConsumer : IConsumer<EndpointMessage>
    {
        public Task Consume(ConsumeContext<EndpointMessage> context) => Task.CompletedTask;
    }

    private sealed class OneOr2MessageConsumer : IConsumer<EndpointMessage>
    {
        public Task Consume(ConsumeContext<EndpointMessage> context) => Task.CompletedTask;
    }

    private sealed class GenericConsumer<TMetadata, TMessage> : IConsumer<TMessage>
        where TMetadata : class
        where TMessage : class
    {
        public Task Consume(ConsumeContext<TMessage> context) => Task.CompletedTask;
    }

    private sealed class Consumer : IConsumer<EndpointMessage>
    {
        public Task Consume(ConsumeContext<EndpointMessage> context) => Task.CompletedTask;
    }

    private sealed class Saga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    private sealed class Activity :
        IExecuteActivity<EndpointMessage>,
        ICompensateActivity<EndpointMessage>
    {
        public Task<ExecutionResult> Execute(ExecuteContext<EndpointMessage> context) =>
            Task.FromResult(context.Completed());

        public Task<CompensationResult> Compensate(CompensateContext<EndpointMessage> context) =>
            Task.FromResult(context.Compensated());
    }
}

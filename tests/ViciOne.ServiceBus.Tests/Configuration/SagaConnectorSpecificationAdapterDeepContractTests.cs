using System.Reflection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class SagaConnectorSpecificationAdapterDeepContractTests
{
    private const BindingFlags DeclaredPublicMembers =
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    private static readonly NullabilityInfoContext Nullability = new();

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-CONFIGURATION", "specification-adapter-required-inputs-and-optional-callback-noops")]
    public void RequiredInputs_RejectNullWhileOptionalCallbacksRemainNoOps()
    {
        var specification = new SagaConnector<AdapterSaga, AdapterMessage>.SagaMessageSpecification();
        var sagaSource = new RecordingSpecification<SagaConsumeContext<AdapterSaga>>();
        var messageSource = new RecordingSpecification<ConsumeContext<AdapterMessage>>();
        var sagaSplit = new SagaConnector<AdapterSaga, AdapterMessage>.SagaSplitFilterSpecification(sagaSource);
        var messageSplit = new SagaConnector<AdapterSaga, AdapterMessage>.SagaMessageSplitFilterSpecification(messageSource);
        var sagaProxy = new SagaConnector<AdapterSaga, AdapterMessage>.SagaPipeSpecificationProxy(sagaSource);

        Assert.Equal("specification", Assert.Throws<ArgumentNullException>(() =>
            new SagaConnector<AdapterSaga, AdapterMessage>.SagaSplitFilterSpecification(null!)).ParamName);
        Assert.Equal("specification", Assert.Throws<ArgumentNullException>(() =>
            new SagaConnector<AdapterSaga, AdapterMessage>.SagaMessageSplitFilterSpecification(null!)).ParamName);
        Assert.Equal("specification", Assert.Throws<ArgumentNullException>(() =>
            new SagaConnector<AdapterSaga, AdapterMessage>.SagaPipeSpecificationProxy(
                (IPipeSpecification<SagaConsumeContext<AdapterSaga>>)null!)).ParamName);
        Assert.Equal("specification", Assert.Throws<ArgumentNullException>(() =>
            new SagaConnector<AdapterSaga, AdapterMessage>.SagaPipeSpecificationProxy(
                (IPipeSpecification<ConsumeContext<AdapterMessage>>)null!)).ParamName);

        Assert.Equal("specification", Assert.Throws<ArgumentNullException>(() => specification.AddPipeSpecification(
            (IPipeSpecification<SagaConsumeContext<AdapterSaga, AdapterMessage>>)null!)).ParamName);
        Assert.Equal("specification", Assert.Throws<ArgumentNullException>(() => specification.AddPipeSpecification(
            (IPipeSpecification<SagaConsumeContext<AdapterSaga>>)null!)).ParamName);
        Assert.Equal("specification", Assert.Throws<ArgumentNullException>(() => specification.AddPipeSpecification(
            (IPipeSpecification<ConsumeContext<AdapterMessage>>)null!)).ParamName);
        Assert.Equal("consumeFilter", Assert.Throws<ArgumentNullException>(() =>
            specification.BuildConsumerPipe(null!)).ParamName);
        IPipe<ConsumeContext<AdapterMessage>> nullCallbackPipe = specification.BuildMessagePipe(null!);
        Assert.Equal("observer", Assert.Throws<ArgumentNullException>(() =>
            specification.ConnectSagaConfigurationObserver(null!)).ParamName);
        specification.Message(null!);

        Assert.Empty(ReadPipeFilters(nullCallbackPipe));
        Assert.Empty(ReadSpecifications(ReadField(specification, "_configurator")));
        Assert.Empty(ReadSpecifications(ReadField(specification, "_messagePipeConfigurator")));

        ISagaMessageConfigurator<AdapterMessage>? messageConfigurator = null;
        specification.Message(configurator => messageConfigurator = configurator);
        Assert.NotNull(messageConfigurator);
        Assert.Equal("specification", Assert.Throws<ArgumentNullException>(() =>
            messageConfigurator.AddPipeSpecification(null!)).ParamName);

        Assert.Equal("builder", Assert.Throws<ArgumentNullException>(() => sagaSplit.Apply(null!)).ParamName);
        Assert.Equal("builder", Assert.Throws<ArgumentNullException>(() => messageSplit.Apply(null!)).ParamName);
        Assert.Equal("builder", Assert.Throws<ArgumentNullException>(() => sagaProxy.Apply(null!)).ParamName);
        AssertPrivateConstructorNullGuard(typeof(SagaConnector<AdapterSaga, AdapterMessage>.SagaMessageSpecification),
            "SagaMessageConfigurator", "configurator");
        AssertPrivateConstructorNullGuard(typeof(SagaConnector<AdapterSaga, AdapterMessage>.SagaSplitFilterSpecification),
            "BuilderProxy", "builder");
        AssertPrivateConstructorNullGuard(typeof(SagaConnector<AdapterSaga, AdapterMessage>.SagaMessageSplitFilterSpecification),
            "BuilderProxy", "builder");

        var sagaNullFilterSource = new RecordingSpecification<SagaConsumeContext<AdapterSaga>> { AddNullFilter = true };
        var messageNullFilterSource = new RecordingSpecification<ConsumeContext<AdapterMessage>> { AddNullFilter = true };
        var sagaBuilder = new RecordingBuilder<SagaConsumeContext<AdapterSaga, AdapterMessage>>();
        var messageBuilder = new RecordingBuilder<SagaConsumeContext<AdapterSaga, AdapterMessage>>();

        Assert.Equal("filter", Assert.Throws<ArgumentNullException>(() =>
            new SagaConnector<AdapterSaga, AdapterMessage>.SagaSplitFilterSpecification(sagaNullFilterSource)
                .Apply(sagaBuilder)).ParamName);
        Assert.Equal("filter", Assert.Throws<ArgumentNullException>(() =>
            new SagaConnector<AdapterSaga, AdapterMessage>.SagaMessageSplitFilterSpecification(messageNullFilterSource)
                .Apply(messageBuilder)).ParamName);
        Assert.Equal(0, sagaBuilder.AddCalls);
        Assert.Equal(0, messageBuilder.AddCalls);
        Assert.Equal(0, sagaSource.ApplyCalls);
        Assert.Equal(0, messageSource.ApplyCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-CONFIGURATION", "specification-adapter-exact-public-surface")]
    public void PublicSurface_PreservesNestedTypeConstructorsAndSynchronousMethodNames()
    {
        Type messageSpecification = typeof(SagaConnector<AdapterSaga, AdapterMessage>.SagaMessageSpecification);
        Type sagaSplit = typeof(SagaConnector<AdapterSaga, AdapterMessage>.SagaSplitFilterSpecification);
        Type messageSplit = typeof(SagaConnector<AdapterSaga, AdapterMessage>.SagaMessageSplitFilterSpecification);
        Type proxy = typeof(SagaConnector<AdapterSaga, AdapterMessage>.SagaPipeSpecificationProxy);

        Assert.All(new[] { messageSpecification, sagaSplit, messageSplit, proxy }, type =>
        {
            Assert.True(type.IsNestedPublic);
            Assert.False(type.IsSealed);
        });
        Assert.Single(messageSpecification.GetConstructors());
        Assert.Single(sagaSplit.GetConstructors());
        Assert.Single(messageSplit.GetConstructors());
        Assert.Equal(2, proxy.GetConstructors().Length);
        Assert.Equal(
            ["AddPipeSpecification", "AddPipeSpecification", "AddPipeSpecification", "BuildConsumerPipe", "BuildMessagePipe",
                "ConnectSagaConfigurationObserver", "Message", "Validate", "get_MessageType"],
            PublicDeclaredMethodNames(messageSpecification));
        Assert.Equal(["Apply", "Validate"], PublicDeclaredMethodNames(sagaSplit));
        Assert.Equal(["Apply", "Validate"], PublicDeclaredMethodNames(messageSplit));
        Assert.Equal(["Apply", "Validate"], PublicDeclaredMethodNames(proxy));
        Assert.DoesNotContain(
            new[] { messageSpecification, sagaSplit, messageSplit, proxy }
                .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)),
            method => typeof(Task).IsAssignableFrom(method.ReturnType) &&
                !method.Name.EndsWith("Async", StringComparison.Ordinal));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-CONFIGURATION", "specification-adapter-exact-signatures-interfaces-nullability")]
    public void PublicSurface_HasExactConstructorMethodInterfaceAndNullabilityContracts()
    {
        Type messageSpecification = typeof(SagaConnector<AdapterSaga, AdapterMessage>.SagaMessageSpecification);
        Type sagaSplit = typeof(SagaConnector<AdapterSaga, AdapterMessage>.SagaSplitFilterSpecification);
        Type messageSplit = typeof(SagaConnector<AdapterSaga, AdapterMessage>.SagaMessageSplitFilterSpecification);
        Type proxy = typeof(SagaConnector<AdapterSaga, AdapterMessage>.SagaPipeSpecificationProxy);
        Type sagaContext = typeof(SagaConsumeContext<AdapterSaga, AdapterMessage>);
        Type messageContext = typeof(ConsumeContext<AdapterMessage>);
        Type sagaOnlyContext = typeof(SagaConsumeContext<AdapterSaga>);

        AssertDirectInterfaces(messageSpecification, typeof(ISagaMessageSpecification<AdapterSaga, AdapterMessage>));
        AssertDirectInterfaces(sagaSplit, typeof(IPipeSpecification<>).MakeGenericType(sagaContext));
        AssertDirectInterfaces(messageSplit, typeof(IPipeSpecification<>).MakeGenericType(sagaContext));
        AssertDirectInterfaces(proxy, typeof(IPipeSpecification<>).MakeGenericType(sagaContext));

        AssertConstructor(messageSpecification);
        AssertConstructor(sagaSplit, typeof(IPipeSpecification<>).MakeGenericType(sagaOnlyContext));
        AssertConstructor(messageSplit, typeof(IPipeSpecification<>).MakeGenericType(messageContext));
        Assert.Equal(
            new[]
            {
                typeof(IPipeSpecification<>).MakeGenericType(messageContext),
                typeof(IPipeSpecification<>).MakeGenericType(sagaOnlyContext),
            }.OrderBy(TypeIdentity, StringComparer.Ordinal),
            proxy.GetConstructors(DeclaredPublicMembers)
                .Select(constructor => Assert.Single(constructor.GetParameters()).ParameterType)
                .OrderBy(TypeIdentity, StringComparer.Ordinal));
        Assert.All(proxy.GetConstructors(DeclaredPublicMembers), AssertNonNullableParameters);

        AssertPublicProperty(messageSpecification, nameof(ISagaMessageSpecification<AdapterSaga>.MessageType), typeof(Type));
        AssertPublicMethod(messageSpecification, nameof(IPipeConfigurator<PipeContext>.AddPipeSpecification), typeof(void),
            typeof(IPipeSpecification<>).MakeGenericType(sagaContext));
        AssertPublicMethod(messageSpecification, nameof(IPipeConfigurator<PipeContext>.AddPipeSpecification), typeof(void),
            typeof(IPipeSpecification<>).MakeGenericType(messageContext));
        AssertPublicMethod(messageSpecification, nameof(IPipeConfigurator<PipeContext>.AddPipeSpecification), typeof(void),
            typeof(IPipeSpecification<>).MakeGenericType(sagaOnlyContext));
        AssertPublicMethod(messageSpecification, nameof(ISagaMessageSpecification<AdapterSaga, AdapterMessage>.BuildConsumerPipe),
            typeof(IPipe<>).MakeGenericType(sagaContext), typeof(IFilter<>).MakeGenericType(sagaContext));
        AssertPublicMethod(messageSpecification, nameof(ISagaMessageSpecification<AdapterSaga, AdapterMessage>.BuildMessagePipe),
            typeof(IPipe<>).MakeGenericType(messageContext),
            typeof(Action<>).MakeGenericType(typeof(IPipeConfigurator<>).MakeGenericType(messageContext)));
        AssertPublicMethod(messageSpecification, nameof(ISagaConfigurationObserverConnector.ConnectSagaConfigurationObserver),
            typeof(ConnectHandle), typeof(ISagaConfigurationObserver));
        AssertPublicMethod(messageSpecification, nameof(ISagaMessageConfigurator<AdapterSaga, AdapterMessage>.Message), typeof(void),
            typeof(Action<>).MakeGenericType(typeof(ISagaMessageConfigurator<>).MakeGenericType(typeof(AdapterMessage))));
        AssertPublicMethod(messageSpecification, nameof(ISpecification.Validate), typeof(IEnumerable<ValidationResult>));

        foreach (Type adapter in new[] { sagaSplit, messageSplit, proxy })
        {
            AssertPublicMethod(adapter, nameof(IPipeSpecification<PipeContext>.Apply), typeof(void),
                typeof(IPipeBuilder<>).MakeGenericType(sagaContext));
            AssertPublicMethod(adapter, nameof(ISpecification.Validate), typeof(IEnumerable<ValidationResult>));
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-CONFIGURATION", "typed-selection-callback-and-wrapper-identity")]
    public void ConfigurationSurfaces_PreserveTypedSelectionCallbackAndWrappedSpecificationIdentity()
    {
        var specification = new SagaConnector<AdapterSaga, AdapterMessage>.SagaMessageSpecification();
        ISagaMessageSpecification<AdapterSaga> untyped = specification;

        Assert.Same(specification, untyped.GetMessageSpecification<AdapterMessage>());
        ArgumentException mismatch = Assert.Throws<ArgumentException>(() =>
            untyped.GetMessageSpecification<OtherMessage>());
        Assert.Contains(nameof(OtherMessage), mismatch.Message, StringComparison.Ordinal);

        var source = new RecordingSpecification<ConsumeContext<AdapterMessage>>();
        var sagaSource = new RecordingSpecification<SagaConsumeContext<AdapterSaga>>();
        ISagaMessageConfigurator<AdapterMessage>? callbackArgument = null;
        specification.Message(configurator =>
        {
            callbackArgument = configurator;
            configurator.AddPipeSpecification(source);
        });
        specification.AddPipeSpecification(sagaSource);

        Assert.NotNull(callbackArgument);
        object configurator = ReadField(callbackArgument!, "_configurator");
        Assert.Collection(
            ReadSpecifications(configurator),
            registered =>
            {
                var proxy = Assert.IsType<SagaConnector<AdapterSaga, AdapterMessage>.SagaPipeSpecificationProxy>(registered);
                object split = ReadField(proxy, "_specification");
                Assert.IsType<SagaConnector<AdapterSaga, AdapterMessage>.SagaMessageSplitFilterSpecification>(split);
                Assert.Same(source, ReadField(split, "_specification"));
            },
            registered =>
            {
                var proxy = Assert.IsType<SagaConnector<AdapterSaga, AdapterMessage>.SagaPipeSpecificationProxy>(registered);
                object split = ReadField(proxy, "_specification");
                Assert.IsType<SagaConnector<AdapterSaga, AdapterMessage>.SagaSplitFilterSpecification>(split);
                Assert.Same(sagaSource, ReadField(split, "_specification"));
            });

        var callbackFailure = new InvalidOperationException("callback failed");
        Assert.Same(callbackFailure, Assert.Throws<InvalidOperationException>(() =>
            specification.BuildMessagePipe(_ => throw callbackFailure)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONFIGURATION-OBSERVER-LIFECYCLE", "saga-message-adapter-once-order-and-result-identity")]
    public void Validate_NotifiesOnceBeforeEachOrderedSnapshotAndPreservesFailureIdentity()
    {
        var specification = new SagaConnector<AdapterSaga, AdapterMessage>.SagaMessageSpecification();
        var sagaResult = specification.Failure("saga", "failure");
        var injectedResult = specification.Failure("observer", "failure");
        var messageResult = specification.Failure("message", "failure");
        var sagaValidation = new RecordingSpecification<SagaConsumeContext<AdapterSaga, AdapterMessage>>();
        sagaValidation.Results.Add(sagaResult);
        var injectedValidation = new RecordingSpecification<SagaConsumeContext<AdapterSaga, AdapterMessage>>();
        injectedValidation.Results.Add(injectedResult);
        var messageValidation = new RecordingSpecification<ConsumeContext<AdapterMessage>>();
        messageValidation.Results.Add(messageResult);
        var observer = new RecordingSagaObserver(injectedValidation);
        specification.AddPipeSpecification(sagaValidation);
        specification.AddPipeSpecification(messageValidation);
        specification.ConnectSagaConfigurationObserver(observer);

        ValidationResult[] first = specification.Validate().ToArray();
        ValidationResult[] second = specification.Validate().ToArray();

        Assert.Equal(1, observer.MessageCalls);
        Assert.Same(specification, observer.MessageConfigurator);
        Assert.Collection(first,
            result => Assert.Same(sagaResult, result),
            result => Assert.Same(injectedResult, result),
            result => Assert.Same(messageResult, result));
        Assert.Collection(second,
            result => Assert.Same(sagaResult, result),
            result => Assert.Same(injectedResult, result),
            result => Assert.Same(messageResult, result));
        Assert.Equal(2, sagaValidation.ValidateCalls);
        Assert.Equal(2, injectedValidation.ValidateCalls);
        Assert.Equal(2, messageValidation.ValidateCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONFIGURATION-OBSERVER-LIFECYCLE", "saga-message-adapter-cached-observer-failure")]
    public void Validate_WhenObserverFails_RethrowsTheSameFailureWithoutNotificationOrValidationRetry()
    {
        var specification = new SagaConnector<AdapterSaga, AdapterMessage>.SagaMessageSpecification();
        var validation = new RecordingSpecification<SagaConsumeContext<AdapterSaga, AdapterMessage>>();
        var failure = new InvalidOperationException("observer failed");
        var observer = new RecordingSagaObserver(failure: failure);
        specification.AddPipeSpecification(validation);
        specification.ConnectSagaConfigurationObserver(observer);

        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => specification.Validate().ToArray()));
        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => specification.Validate().ToArray()));
        Assert.Equal(1, observer.MessageCalls);
        Assert.Equal(0, validation.ValidateCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONFIGURATION-OBSERVER-LIFECYCLE", "saga-message-observer-disconnect-handle-lifecycle")]
    public void ObserverHandle_DisconnectsOnlyItsObserverBeforeOneShotNotification()
    {
        var specification = new SagaConnector<AdapterSaga, AdapterMessage>.SagaMessageSpecification();
        var disconnected = new RecordingSagaObserver();
        var retained = new RecordingSagaObserver();
        ConnectHandle disconnectedHandle = specification.ConnectSagaConfigurationObserver(disconnected);
        ConnectHandle retainedHandle = specification.ConnectSagaConfigurationObserver(retained);

        disconnectedHandle.Dispose();
        ValidationResult[] first = specification.Validate().ToArray();
        ValidationResult[] second = specification.Validate().ToArray();

        Assert.Empty(first);
        Assert.Empty(second);
        Assert.Equal(0, disconnected.MessageCalls);
        Assert.Equal(1, retained.MessageCalls);
        Assert.Same(specification, retained.MessageConfigurator);
        retainedHandle.Dispose();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-CONFIGURATION", "repeat-build-append-order-and-built-pipe-stability")]
    public void BuildMethods_AppendInOrderAcrossCallsWithoutMutatingPreviouslyBuiltPipes()
    {
        var specification = new SagaConnector<AdapterSaga, AdapterMessage>.SagaMessageSpecification();
        var configured = new RecordingFilter<SagaConsumeContext<AdapterSaga, AdapterMessage>>();
        var firstTerminal = new RecordingFilter<SagaConsumeContext<AdapterSaga, AdapterMessage>>();
        var secondTerminal = new RecordingFilter<SagaConsumeContext<AdapterSaga, AdapterMessage>>();
        specification.AddPipeSpecification(new FilterPipeSpecification<SagaConsumeContext<AdapterSaga, AdapterMessage>>(configured));

        IPipe<SagaConsumeContext<AdapterSaga, AdapterMessage>> first = specification.BuildConsumerPipe(firstTerminal);
        IPipe<SagaConsumeContext<AdapterSaga, AdapterMessage>> second = specification.BuildConsumerPipe(secondTerminal);

        Assert.Equal([configured, firstTerminal], ReadPipeFilters(first));
        Assert.Equal([configured, firstTerminal, secondTerminal], ReadPipeFilters(second));
        Assert.Equal([configured, firstTerminal], ReadPipeFilters(first));

        var firstMessage = new RecordingFilter<ConsumeContext<AdapterMessage>>();
        var secondMessage = new RecordingFilter<ConsumeContext<AdapterMessage>>();
        IPipeConfigurator<ConsumeContext<AdapterMessage>>? firstConfigurator = null;
        IPipeConfigurator<ConsumeContext<AdapterMessage>>? secondConfigurator = null;
        IPipe<ConsumeContext<AdapterMessage>> firstMessagePipe = specification.BuildMessagePipe(configurator =>
        {
            firstConfigurator = configurator;
            configurator.AddPipeSpecification(new FilterPipeSpecification<ConsumeContext<AdapterMessage>>(firstMessage));
        });
        IPipe<ConsumeContext<AdapterMessage>> secondMessagePipe = specification.BuildMessagePipe(configurator =>
        {
            secondConfigurator = configurator;
            configurator.AddPipeSpecification(new FilterPipeSpecification<ConsumeContext<AdapterMessage>>(secondMessage));
        });

        Assert.Same(firstConfigurator, secondConfigurator);
        Assert.Equal([firstMessage], ReadPipeFilters(firstMessagePipe));
        Assert.Equal([firstMessage, secondMessage], ReadPipeFilters(secondMessagePipe));
        Assert.Equal([firstMessage], ReadPipeFilters(firstMessagePipe));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-CONFIGURATION", "specification-proxy-both-paths-apply-validate-forwarding")]
    public void ProxyConstructorPaths_ForwardApplyAndValidationWithExactIdentities()
    {
        var sagaFilter = new RecordingFilter<SagaConsumeContext<AdapterSaga>>();
        var messageFilter = new RecordingFilter<ConsumeContext<AdapterMessage>>();
        var sagaSource = new RecordingSpecification<SagaConsumeContext<AdapterSaga>> { Filter = sagaFilter };
        var messageSource = new RecordingSpecification<ConsumeContext<AdapterMessage>> { Filter = messageFilter };
        ValidationResult sagaResult = sagaSource.Failure("saga-proxy", "failure");
        ValidationResult messageResult = messageSource.Failure("message-proxy", "failure");
        sagaSource.Results.Add(sagaResult);
        messageSource.Results.Add(messageResult);
        var sagaProxy = new SagaConnector<AdapterSaga, AdapterMessage>.SagaPipeSpecificationProxy(sagaSource);
        var messageProxy = new SagaConnector<AdapterSaga, AdapterMessage>.SagaPipeSpecificationProxy(messageSource);
        var sagaBuilder = new RecordingBuilder<SagaConsumeContext<AdapterSaga, AdapterMessage>>();
        var messageBuilder = new RecordingBuilder<SagaConsumeContext<AdapterSaga, AdapterMessage>>();

        Assert.Same(sagaResult, Assert.Single(sagaProxy.Validate()));
        Assert.Same(messageResult, Assert.Single(messageProxy.Validate()));
        sagaProxy.Apply(sagaBuilder);
        messageProxy.Apply(messageBuilder);

        Assert.Equal(1, sagaSource.ValidateCalls);
        Assert.Equal(1, messageSource.ValidateCalls);
        Assert.Equal(1, sagaSource.ApplyCalls);
        Assert.Equal(1, messageSource.ApplyCalls);
        Assert.NotSame(sagaBuilder, sagaSource.AppliedBuilder);
        Assert.NotSame(messageBuilder, messageSource.AppliedBuilder);
        var sagaWrapper = Assert.IsType<SagaSplitFilter<AdapterSaga, AdapterMessage>>(Assert.Single(sagaBuilder.Filters));
        var messageWrapper = Assert.IsType<SagaMessageSplitFilter<AdapterSaga, AdapterMessage>>(Assert.Single(messageBuilder.Filters));
        Assert.Same(sagaFilter, ReadField(sagaWrapper, "_next"));
        Assert.Same(messageFilter, ReadField(messageWrapper, "_next"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-CONFIGURATION", "adapter-validation-filter-context-and-failure-identity")]
    public async Task Adapters_PreserveValidationFilterContextAndFailureIdentityAsync()
    {
        var sagaFilter = new ForwardingFilter<SagaConsumeContext<AdapterSaga>>();
        var messageFilter = new ForwardingFilter<ConsumeContext<AdapterMessage>>();
        var sagaResultOwner = new RecordingSpecification<SagaConsumeContext<AdapterSaga>>();
        var messageResultOwner = new RecordingSpecification<ConsumeContext<AdapterMessage>>();
        ValidationResult sagaResult = sagaResultOwner.Failure("saga-adapter", "failure");
        ValidationResult messageResult = messageResultOwner.Failure("message-adapter", "failure");
        sagaResultOwner.Results.Add(sagaResult);
        sagaResultOwner.Filter = sagaFilter;
        messageResultOwner.Results.Add(messageResult);
        messageResultOwner.Filter = messageFilter;
        var sagaSplit = new SagaConnector<AdapterSaga, AdapterMessage>.SagaSplitFilterSpecification(sagaResultOwner);
        var messageSplit = new SagaConnector<AdapterSaga, AdapterMessage>.SagaMessageSplitFilterSpecification(messageResultOwner);
        var sagaBuilder = new RecordingBuilder<SagaConsumeContext<AdapterSaga, AdapterMessage>>();
        var messageBuilder = new RecordingBuilder<SagaConsumeContext<AdapterSaga, AdapterMessage>>();

        Assert.Same(sagaResult, Assert.Single(sagaSplit.Validate()));
        Assert.Same(messageResult, Assert.Single(messageSplit.Validate()));
        sagaSplit.Apply(sagaBuilder);
        messageSplit.Apply(messageBuilder);

        var sagaWrapper = Assert.IsType<SagaSplitFilter<AdapterSaga, AdapterMessage>>(Assert.Single(sagaBuilder.Filters));
        var messageWrapper = Assert.IsType<SagaMessageSplitFilter<AdapterSaga, AdapterMessage>>(Assert.Single(messageBuilder.Filters));
        Assert.Same(sagaFilter, ReadField(sagaWrapper, "_next"));
        Assert.Same(messageFilter, ReadField(messageWrapper, "_next"));
        Assert.NotSame(sagaBuilder, sagaResultOwner.AppliedBuilder);
        Assert.NotSame(messageBuilder, messageResultOwner.AppliedBuilder);

        SagaConsumeContext<AdapterSaga, AdapterMessage> context = StrictStub<SagaConsumeContext<AdapterSaga, AdapterMessage>>();
        var sagaOutput = new RecordingPipe<SagaConsumeContext<AdapterSaga, AdapterMessage>>();
        var messageOutput = new RecordingPipe<SagaConsumeContext<AdapterSaga, AdapterMessage>>();
        await sagaWrapper.SendAsync(context, sagaOutput);
        await messageWrapper.SendAsync(context, messageOutput);

        Assert.Same(context, sagaFilter.Context);
        Assert.Same(context, messageFilter.Context);
        Assert.Same(context, sagaOutput.Context);
        Assert.Same(context, messageOutput.Context);

        var builderFailure = new InvalidOperationException("builder failed");
        var failingBuilder = new RecordingBuilder<SagaConsumeContext<AdapterSaga, AdapterMessage>>(builderFailure);
        Assert.Same(builderFailure, Assert.Throws<InvalidOperationException>(() => sagaSplit.Apply(failingBuilder)));
        Assert.Equal(1, failingBuilder.AddCalls);

        var sourceFailure = new InvalidOperationException("source failed");
        var failingSource = new RecordingSpecification<SagaConsumeContext<AdapterSaga>>(sourceFailure);
        var untouchedBuilder = new RecordingBuilder<SagaConsumeContext<AdapterSaga, AdapterMessage>>();
        var failingSplit = new SagaConnector<AdapterSaga, AdapterMessage>.SagaSplitFilterSpecification(failingSource);
        Assert.Same(sourceFailure, Assert.Throws<InvalidOperationException>(() => failingSplit.Apply(untouchedBuilder)));
        Assert.Equal(1, failingSource.ApplyCalls);
        Assert.Equal(0, untouchedBuilder.AddCalls);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(3, true)]
    [RequirementCoverage("REQ-VSB-CONFIGURATION-OBSERVER-LIFECYCLE", "saga-constructor-rolls-back-acquired-observer-handles")]
    public void Constructor_PartialObserverConnectionsAreRolledBackWithoutLosingFailures(int failureAt, bool cleanupThrows)
    {
        ISagaMessageSpecification<AdapterSaga>[] sources =
        [
            new SagaConnector<AdapterSaga, AdapterMessage>.SagaMessageSpecification(),
            new SagaConnector<AdapterSaga, OtherMessage>.SagaMessageSpecification(),
            new SagaConnector<AdapterSaga, ThirdMessage>.SagaMessageSpecification(),
        ];
        var primary = new IOException("Saga observer connection admission failed");
        var cleanup = new IOException("Saga observer connection cleanup failed");
        var state = new ConstructorConnectionState(failureAt, primary, cleanupThrows ? cleanup : null);
        ISagaMessageSpecification<AdapterSaga>[] delegated = sources.Select(source =>
        {
            var proxy = DispatchProxy.Create<ISagaMessageSpecification<AdapterSaga>, ConstructorMessageSpecificationProxy>();
            var implementation = (ConstructorMessageSpecificationProxy)(object)proxy;
            implementation.Source = source;
            implementation.State = state;
            return proxy;
        }).ToArray();
        SagaSpecification<AdapterSaga>? constructed = null;
        ConnectHandle? observedHandle = null;
        var observer = new RecordingSagaObserver();
        try
        {
            Exception? failure = Record.Exception(() => constructed = new SagaSpecification<AdapterSaga>(delegated));
            Assert.Equal(failureAt == 0 ? 3 : failureAt, state.ConnectCalls);
            Assert.Equal(failureAt == 0 ? 3 : failureAt - 1, state.Handles.Count);
            SagaConfigurationObservable supplied = Assert.IsType<SagaConfigurationObservable>(state.Observer);
            if (failureAt == 0)
            {
                Assert.Null(failure);
                Assert.NotNull(constructed);
                observedHandle = constructed.ConnectSagaConfigurationObserver(observer);
            }
            else
            {
                Assert.Null(constructed);
                Assert.NotNull(failure);
                if (failure is AggregateException aggregate)
                    Assert.Same(primary, Assert.Single(aggregate.Flatten().InnerExceptions, cause => ReferenceEquals(cause, primary)));
                else
                    Assert.Same(primary, failure);
                // The real constructor supplied this observer through the public connector SPI.
                observedHandle = supplied.Connect(observer);
            }

            foreach (ISagaMessageSpecification<AdapterSaga> source in sources)
                Assert.Empty(source.Validate());

            Assert.Equal(failureAt == 0 ? 3 : 0, observer.MessageCalls);
            Assert.All(state.Handles, handle => Assert.Equal(failureAt == 0 ? 0 : 1, handle.DisconnectCalls));
            if (failureAt != 0)
            {
                if (cleanupThrows)
                {
                    var aggregate = Assert.IsType<AggregateException>(failure);
                    IReadOnlyList<Exception> causes = aggregate.Flatten().InnerExceptions;
                    Assert.Equal(2, causes.Count);
                    Assert.Same(primary, Assert.Single(causes, cause => ReferenceEquals(cause, primary)));
                    Assert.Same(cleanup, Assert.Single(causes, cause => ReferenceEquals(cause, cleanup)));
                }
                else
                    Assert.Same(primary, failure);
            }
        }
        finally
        {
            try
            {
                observedHandle?.Dispose();
            }
            finally
            {
                foreach (ConstructorConnectionHandle handle in state.Handles)
                    handle.DisconnectWithoutInjectedFailure();
            }
        }
    }

    private sealed class ConstructorConnectionState(int failureAt, Exception primary, Exception? cleanup)
    {
        public int FailureAt { get; } = failureAt;
        public Exception Primary { get; } = primary;
        public Exception? Cleanup { get; } = cleanup;
        public int ConnectCalls { get; set; }
        public ISagaConfigurationObserver? Observer { get; set; }
        public List<ConstructorConnectionHandle> Handles { get; } = [];
    }

    private class ConstructorMessageSpecificationProxy : DispatchProxy
    {
        public ISagaMessageSpecification<AdapterSaga> Source { get; set; } = null!;
        public ConstructorConnectionState State { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            if (targetMethod.Name == nameof(ISagaConfigurationObserverConnector.ConnectSagaConfigurationObserver))
            {
                State.ConnectCalls++;
                var observer = (ISagaConfigurationObserver)args![0]!;
                State.Observer ??= observer;
                if (State.ConnectCalls == State.FailureAt)
                    throw State.Primary;
                ConnectHandle connected = Source.ConnectSagaConfigurationObserver(observer);
                var handle = new ConstructorConnectionHandle(connected, State.Handles.Count == 0 ? State.Cleanup : null);
                State.Handles.Add(handle);
                return handle;
            }

            try
            {
                return targetMethod.Invoke(Source, args);
            }
            catch (TargetInvocationException wrapper) when (wrapper.InnerException is not null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(wrapper.InnerException).Throw();
                throw;
            }
        }
    }

    private sealed class ConstructorConnectionHandle(ConnectHandle inner, Exception? cleanup) : ConnectHandle
    {
        int _disconnected;
        bool _injectCleanup = true;
        public int DisconnectCalls { get; private set; }

        public void Disconnect()
        {
            if (Interlocked.Exchange(ref _disconnected, 1) != 0)
                return;
            DisconnectCalls++;
            inner.Disconnect();
            if (_injectCleanup && cleanup is not null)
                throw cleanup;
        }

        public void Dispose() => Disconnect();

        public void DisconnectWithoutInjectedFailure()
        {
            _injectCleanup = false;
            Disconnect();
        }
    }

    private sealed record ThirdMessage;


    private static string[] PublicDeclaredMethodNames(Type type) =>
        type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(method => method.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

    private static void AssertDirectInterfaces(Type type, params Type[] expected)
    {
        Type[] all = type.GetInterfaces();
        Type[] direct = all.Where(candidate => !all.Any(
            other => other != candidate && other.GetInterfaces().Contains(candidate))).ToArray();

        Assert.Equal(
            expected.OrderBy(TypeIdentity, StringComparer.Ordinal),
            direct.OrderBy(TypeIdentity, StringComparer.Ordinal));
    }

    private static void AssertConstructor(Type type, params Type[] parameterTypes)
    {
        ConstructorInfo constructor = Assert.Single(type.GetConstructors(DeclaredPublicMembers), candidate =>
            candidate.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(parameterTypes));

        Assert.Equal(type, constructor.DeclaringType);
        Assert.True(constructor.IsPublic);
        AssertNonNullableParameters(constructor);
    }

    private static void AssertPublicProperty(Type type, string name, Type propertyType)
    {
        PropertyInfo property = Assert.Single(type.GetProperties(DeclaredPublicMembers), candidate => candidate.Name == name);

        Assert.Equal(type, property.DeclaringType);
        Assert.Equal(propertyType, property.PropertyType);
        Assert.True(property.CanRead);
        Assert.False(property.CanWrite);
        Assert.NotNull(property.GetMethod);
        Assert.True(property.GetMethod.IsPublic);
        Assert.Equal(NullabilityState.NotNull, Nullability.Create(property).ReadState);
    }

    private static void AssertPublicMethod(Type type, string name, Type returnType, params Type[] parameterTypes)
    {
        MethodInfo method = Assert.Single(type.GetMethods(DeclaredPublicMembers), candidate =>
            candidate.Name == name &&
            candidate.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(parameterTypes));

        Assert.Equal(type, method.DeclaringType);
        Assert.Equal(returnType, method.ReturnType);
        Assert.True(method.IsPublic);
        Assert.False(method.IsStatic);
        Assert.False(method.IsGenericMethod);
        Assert.False(method.IsSpecialName);
        AssertNonNullableParameters(method);

        if (!returnType.IsValueType)
            Assert.Equal(NullabilityState.NotNull, Nullability.Create(method.ReturnParameter).ReadState);
    }

    private static void AssertNonNullableParameters(MethodBase method)
    {
        Assert.All(method.GetParameters(), parameter =>
        {
            if (!parameter.ParameterType.IsValueType)
                Assert.Equal(NullabilityState.NotNull, Nullability.Create(parameter).ReadState);
        });
    }

    private static string TypeIdentity(Type type) => type.AssemblyQualifiedName ?? type.FullName ?? type.Name;

    private static void AssertPrivateConstructorNullGuard(Type owner, string nestedTypeName, string parameterName)
    {
        Type nestedType = owner.GetNestedType(nestedTypeName, BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Nested type '{nestedTypeName}' was not found on {owner}.");
        if (nestedType.ContainsGenericParameters)
            nestedType = nestedType.MakeGenericType(owner.GetGenericArguments());

        ConstructorInfo constructor = Assert.Single(nestedType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        TargetInvocationException wrapper = Assert.Throws<TargetInvocationException>(() => constructor.Invoke([null]));
        var exception = Assert.IsType<ArgumentNullException>(wrapper.InnerException);
        Assert.Equal(parameterName, exception.ParamName);
    }

    private static object ReadField(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Field '{fieldName}' was not found on {target.GetType()}.");
        return field.GetValue(target)
            ?? throw new InvalidOperationException($"Field '{fieldName}' on {target.GetType()} was null.");
    }

    private static IReadOnlyList<object> ReadSpecifications(object configurator)
    {
        object specifications = ReadField(configurator, "_specifications");
        return Assert.IsAssignableFrom<IReadOnlyList<object>>(specifications);
    }

    private static object[] ReadPipeFilters<TContext>(IPipe<TContext> pipe)
        where TContext : class, PipeContext
    {
        var filters = new List<object>();
        object current = pipe;

        while (current.GetType().Name == "FilterPipe")
        {
            filters.Add(ReadField(current, "_filter"));
            current = ReadField(current, "_next");
        }

        if (current.GetType().Name == "LastPipe")
            filters.Add(ReadField(current, "_filter"));

        return filters.ToArray();
    }

    private static T StrictStub<T>()
        where T : class => DispatchProxy.Create<T, StrictDispatchProxy>();

    private class StrictDispatchProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"Unexpected collaborator call: {targetMethod?.Name}.");
    }

    private sealed class RecordingSpecification<TContext>(Exception? applyFailure = null) : IPipeSpecification<TContext>
        where TContext : class, PipeContext
    {
        public List<ValidationResult> Results { get; } = [];

        public IFilter<TContext>? Filter { get; set; }

        public bool AddNullFilter { get; set; }

        public int ApplyCalls { get; private set; }

        public int ValidateCalls { get; private set; }

        public IPipeBuilder<TContext>? AppliedBuilder { get; private set; }

        public void Apply(IPipeBuilder<TContext> builder)
        {
            ApplyCalls++;
            AppliedBuilder = builder;

            if (applyFailure != null)
                throw applyFailure;

            if (Filter != null || AddNullFilter)
                builder.AddFilter(Filter!);
        }

        public IEnumerable<ValidationResult> Validate()
        {
            ValidateCalls++;
            return Results;
        }
    }

    private sealed class RecordingBuilder<TContext>(Exception? addFailure = null) : IPipeBuilder<TContext>
        where TContext : class, PipeContext
    {
        public List<IFilter<TContext>> Filters { get; } = [];

        public int AddCalls { get; private set; }

        public void AddFilter(IFilter<TContext> filter)
        {
            AddCalls++;
            Filters.Add(filter);

            if (addFailure != null)
                throw addFailure;
        }
    }

    private sealed class RecordingFilter<TContext> : IFilter<TContext>
        where TContext : class, PipeContext
    {
        public void Probe(ProbeContext context)
        {
        }

        public Task SendAsync(TContext context, IPipe<TContext> next) => next.SendAsync(context);
    }

    private sealed class ForwardingFilter<TContext> : IFilter<TContext>
        where TContext : class, PipeContext
    {
        public TContext? Context { get; private set; }

        public void Probe(ProbeContext context)
        {
        }

        public Task SendAsync(TContext context, IPipe<TContext> next)
        {
            Context = context;
            return next.SendAsync(context);
        }
    }

    private sealed class RecordingPipe<TContext> : IPipe<TContext>
        where TContext : class, PipeContext
    {
        public TContext? Context { get; private set; }

        public void Probe(ProbeContext context)
        {
        }

        public Task SendAsync(TContext context)
        {
            Context = context;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingSagaObserver(
        IPipeSpecification<SagaConsumeContext<AdapterSaga, AdapterMessage>>? injected = null,
        Exception? failure = null) : ISagaConfigurationObserver
    {
        public int MessageCalls { get; private set; }

        public object? MessageConfigurator { get; private set; }

        public void SagaConfigured<TSaga>(ISagaConfigurator<TSaga> configurator)
            where TSaga : class
        {
        }

        public void StateMachineSagaConfigured<TInstance>(ISagaConfigurator<TInstance> configurator, object stateMachine)
            where TInstance : class
        {
        }

        public void SagaMessageConfigured<TSaga, TMessage>(ISagaMessageConfigurator<TSaga, TMessage> configurator)
            where TSaga : class
            where TMessage : class
        {
            MessageCalls++;
            MessageConfigurator = configurator;

            if (failure != null)
                throw failure;

            if (injected != null && configurator is ISagaMessageConfigurator<AdapterSaga, AdapterMessage> typed)
                typed.AddPipeSpecification(injected);
        }
    }

    private sealed class AdapterSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    private sealed record AdapterMessage;

    private sealed record OtherMessage;
}

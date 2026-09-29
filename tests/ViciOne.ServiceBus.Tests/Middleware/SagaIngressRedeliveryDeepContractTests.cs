using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Observers;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware;

public sealed class SagaIngressRedeliveryDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INGRESS-REDELIVERY", "filter-constructor-probe-and-send-null-boundaries")]
    public async Task Filters_RejectMissingDependenciesAndCallArgumentsAtTheirOwnBoundariesAsync()
    {
        var events = new List<string>();
        var repository = new RecordingSagaRepository(events);
        var policy = new RecordingSagaPolicy();
        var queryFactory = new RecordingQueryFactory(events, true, new SagaQuery<IngressSaga>(_ => true));
        var sagaPipe = new RecordingPipe<SagaConsumeContext<IngressSaga, IngressMessage>>(events, "saga-pipe");
        var next = new RecordingPipe<ConsumeContext<IngressMessage>>(events, "next");
        FilterContext fixture = CreateFilterContext();

        AssertArgument("sagaRepository", () => new CorrelatedSagaFilter<IngressSaga, IngressMessage>(null!, policy, sagaPipe));
        AssertArgument("policy", () => new CorrelatedSagaFilter<IngressSaga, IngressMessage>(repository, null!, sagaPipe));
        AssertArgument("messagePipe", () => new CorrelatedSagaFilter<IngressSaga, IngressMessage>(repository, policy, null!));
        AssertArgument("sagaRepository", () => new QuerySagaFilter<IngressSaga, IngressMessage>(null!, policy, queryFactory, sagaPipe));
        AssertArgument("policy", () => new QuerySagaFilter<IngressSaga, IngressMessage>(repository, null!, queryFactory, sagaPipe));
        AssertArgument("queryFactory", () => new QuerySagaFilter<IngressSaga, IngressMessage>(repository, policy, null!, sagaPipe));
        AssertArgument("messagePipe", () => new QuerySagaFilter<IngressSaga, IngressMessage>(repository, policy, queryFactory, null!));

        var correlated = new CorrelatedSagaFilter<IngressSaga, IngressMessage>(repository, policy, sagaPipe);
        var queried = new QuerySagaFilter<IngressSaga, IngressMessage>(repository, policy, queryFactory, sagaPipe);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => correlated.Probe(null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => queried.Probe(null!)).ParamName);
        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() => correlated.SendAsync(null!, next))).ParamName);
        Assert.Equal("next", (await Assert.ThrowsAsync<ArgumentNullException>(() => correlated.SendAsync(fixture.Context, null!))).ParamName);
        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() => queried.SendAsync(null!, next))).ParamName);
        Assert.Equal("next", (await Assert.ThrowsAsync<ArgumentNullException>(() => queried.SendAsync(fixture.Context, null!))).ParamName);
        Assert.Empty(events);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INGRESS-REDELIVERY", "query-bypass-does-not-attribute-downstream-failure")]
    public async Task QueryWithoutAQuery_BypassesTheRepositoryWithoutAttributingDownstreamFailureAsync()
    {
        var events = new List<string>();
        var failure = new IngressFailureException("downstream bypass failure");
        var repository = new RecordingSagaRepository(events);
        var filter = new QuerySagaFilter<IngressSaga, IngressMessage>(
            repository,
            new RecordingSagaPolicy(),
            new RecordingQueryFactory(events, false, null),
            new RecordingPipe<SagaConsumeContext<IngressSaga, IngressMessage>>(events, "saga-pipe"));
        var next = new RecordingPipe<ConsumeContext<IngressMessage>>(events, "downstream", _ => Task.FromException(failure));
        FilterContext fixture = CreateFilterContext(events);

        IngressFailureException actual = await Assert.ThrowsAsync<IngressFailureException>(() => filter.SendAsync(fixture.Context, next));

        Assert.Same(failure, actual);
        Assert.Equal(["query", "downstream"], events);
        Assert.Equal(0, repository.QueryCalls);
        Assert.Equal(0, fixture.Proxy.ConsumedCalls);
        Assert.Equal(0, fixture.Proxy.FaultedCalls);

        var successEvents = new List<string>();
        var successRepository = new RecordingSagaRepository(successEvents);
        var successFilter = new QuerySagaFilter<IngressSaga, IngressMessage>(
            successRepository,
            new RecordingSagaPolicy(),
            new RecordingQueryFactory(successEvents, false, null),
            new RecordingPipe<SagaConsumeContext<IngressSaga, IngressMessage>>(successEvents, "saga-pipe"));
        var successNext = new RecordingPipe<ConsumeContext<IngressMessage>>(successEvents, "downstream");
        FilterContext successFixture = CreateFilterContext(successEvents);

        await successFilter.SendAsync(successFixture.Context, successNext);

        Assert.Equal(["query", "downstream"], successEvents);
        Assert.Equal(0, successRepository.QueryCalls);
        Assert.Equal(0, successFixture.Proxy.ConsumedCalls);
        Assert.Equal(0, successFixture.Proxy.FaultedCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INGRESS-REDELIVERY", "query-success-with-null-query-is-stable-failure")]
    public async Task QueryFactorySuccessWithoutAQuery_FailsDeterministicallyAndIsObservedAsync()
    {
        var events = new List<string>();
        var repository = new RecordingSagaRepository(events);
        var filter = new QuerySagaFilter<IngressSaga, IngressMessage>(
            repository,
            new RecordingSagaPolicy(),
            new RecordingQueryFactory(events, true, null),
            new RecordingPipe<SagaConsumeContext<IngressSaga, IngressMessage>>(events, "saga-pipe"));
        var next = new RecordingPipe<ConsumeContext<IngressMessage>>(events, "downstream");
        FilterContext fixture = CreateFilterContext(events);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() => filter.SendAsync(fixture.Context, next));

        Assert.Equal("The saga query factory reported success without returning a query.", actual.Message);
        Assert.Equal(["query", "faulted"], events);
        Assert.Same(actual, fixture.Proxy.Faults.Single());
        Assert.Equal(0, repository.QueryCalls);
        Assert.Equal(0, next.SendCalls);
        Assert.Equal(0, fixture.Proxy.ConsumedCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INGRESS-REDELIVERY", "query-factory-failure-is-observed-before-repository-effects")]
    public async Task QueryFactoryFailure_IsObservedAndPreservedBeforeRepositoryOrDownstreamEffectsAsync()
    {
        var events = new List<string>();
        var queryFailure = new IngressFailureException("query factory failed");
        var observerFailure = new InvalidOperationException("fault observer failed");
        var repository = new RecordingSagaRepository(events);
        var queryFactory = new RecordingQueryFactory(events, false, null) { Failure = queryFailure };
        var filter = new QuerySagaFilter<IngressSaga, IngressMessage>(
            repository,
            new RecordingSagaPolicy(),
            queryFactory,
            new RecordingPipe<SagaConsumeContext<IngressSaga, IngressMessage>>(events, "saga-pipe"));
        var next = new RecordingPipe<ConsumeContext<IngressMessage>>(events, "downstream");
        FilterContext fixture = CreateFilterContext(events);
        fixture.Proxy.FaultNotificationFailure = observerFailure;

        AggregateException actual = await Assert.ThrowsAsync<AggregateException>(() => filter.SendAsync(fixture.Context, next));

        Assert.Collection(
            actual.InnerExceptions,
            exception => Assert.Same(queryFailure, exception),
            exception => Assert.Same(observerFailure, exception));
        Assert.Equal(["query", "faulted"], events);
        Assert.Same(queryFailure, Assert.Single(fixture.Proxy.Faults));
        Assert.Equal(0, repository.QueryCalls);
        Assert.Equal(0, next.SendCalls);
        Assert.Equal(0, fixture.Proxy.ConsumedCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INGRESS-REDELIVERY", "filter-probe-shape-and-child-order")]
    public void Filters_ProbeExactCorrelationShapeAndChildOrder()
    {
        var correlatedEvents = new List<string>();
        var correlatedProbe = new RecordingProbeContext(correlatedEvents);
        new CorrelatedSagaFilter<IngressSaga, IngressMessage>(
                new RecordingSagaRepository(correlatedEvents),
                new RecordingSagaPolicy(),
                new RecordingPipe<SagaConsumeContext<IngressSaga, IngressMessage>>(correlatedEvents, "saga-pipe"))
            .Probe(correlatedProbe);

        Assert.Equal(["scope:filters", "repository-probe", "saga-pipe-probe"], correlatedEvents);
        Assert.Contains("filters", correlatedProbe.ScopeKeys);
        Assert.Equal("saga", correlatedProbe.Values["filterType"]);
        Assert.Equal("Id", correlatedProbe.Values["Correlation"]);

        var queryEvents = new List<string>();
        var queryProbe = new RecordingProbeContext(queryEvents);
        new QuerySagaFilter<IngressSaga, IngressMessage>(
                new RecordingSagaRepository(queryEvents),
                new RecordingSagaPolicy(),
                new RecordingQueryFactory(queryEvents, false, null),
                new RecordingPipe<SagaConsumeContext<IngressSaga, IngressMessage>>(queryEvents, "saga-pipe"))
            .Probe(queryProbe);

        Assert.Equal(["scope:filters", "query-probe", "repository-probe", "saga-pipe-probe"], queryEvents);
        Assert.Contains("filters", queryProbe.ScopeKeys);
        Assert.Equal("saga", queryProbe.Values["filterType"]);
        Assert.Equal("Query", queryProbe.Values["Correlation"]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INGRESS-REDELIVERY", "query-repository-downstream-consumed-order")]
    public async Task QueriedSaga_CompletesRepositoryThenDownstreamThenConsumedAsync()
    {
        var events = new List<string>();
        var repository = new RecordingSagaRepository(events);
        ISagaQuery<IngressSaga> query = new SagaQuery<IngressSaga>(_ => true);
        var filter = new QuerySagaFilter<IngressSaga, IngressMessage>(
            repository,
            new RecordingSagaPolicy(),
            new RecordingQueryFactory(events, true, query),
            new RecordingPipe<SagaConsumeContext<IngressSaga, IngressMessage>>(events, "saga-pipe"));
        var next = new RecordingPipe<ConsumeContext<IngressMessage>>(events, "downstream");
        FilterContext fixture = CreateFilterContext(events);

        await filter.SendAsync(fixture.Context, next);

        Assert.Equal(["query", "repository-query", "downstream", "consumed"], events);
        Assert.Same(query, repository.Query);
        Assert.Equal(1, fixture.Proxy.ConsumedCalls);
        Assert.Equal(0, fixture.Proxy.FaultedCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INGRESS-REDELIVERY", "correlated-repository-downstream-consumed-order")]
    public async Task CorrelatedSaga_CompletesRepositoryThenDownstreamThenConsumedAsync()
    {
        var events = new List<string>();
        var repository = new RecordingSagaRepository(events);
        var filter = new CorrelatedSagaFilter<IngressSaga, IngressMessage>(
            repository,
            new RecordingSagaPolicy(),
            new RecordingPipe<SagaConsumeContext<IngressSaga, IngressMessage>>(events, "saga-pipe"));
        var next = new RecordingPipe<ConsumeContext<IngressMessage>>(events, "downstream");
        FilterContext fixture = CreateFilterContext(events);

        await filter.SendAsync(fixture.Context, next);

        Assert.Equal(["repository-correlated", "downstream", "consumed"], events);
        Assert.Equal(1, fixture.Proxy.ConsumedCalls);
        Assert.Equal(0, fixture.Proxy.FaultedCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-INGRESS-REDELIVERY", "queried-and-correlated-downstream-failure-is-fault-only")]
    public async Task SagaDownstreamFailure_IsObservedAsFaultAndNeverAsConsumedAsync(bool queried)
    {
        var events = new List<string>();
        var failure = new IngressFailureException("downstream failed");
        var repository = new RecordingSagaRepository(events);
        var next = new RecordingPipe<ConsumeContext<IngressMessage>>(events, "downstream", _ => Task.FromException(failure));
        FilterContext fixture = CreateFilterContext(events);

        Task OperationAsync() => queried
            ? new QuerySagaFilter<IngressSaga, IngressMessage>(
                    repository,
                    new RecordingSagaPolicy(),
                    new RecordingQueryFactory(events, true, new SagaQuery<IngressSaga>(_ => true)),
                    new RecordingPipe<SagaConsumeContext<IngressSaga, IngressMessage>>(events, "saga-pipe"))
                .SendAsync(fixture.Context, next)
            : new CorrelatedSagaFilter<IngressSaga, IngressMessage>(
                    repository,
                    new RecordingSagaPolicy(),
                    new RecordingPipe<SagaConsumeContext<IngressSaga, IngressMessage>>(events, "saga-pipe"))
                .SendAsync(fixture.Context, next);

        IngressFailureException actual = await Assert.ThrowsAsync<IngressFailureException>(OperationAsync);

        Assert.Same(failure, actual);
        Assert.Equal("faulted", events[^1]);
        Assert.DoesNotContain("consumed", events);
        Assert.Same(failure, fixture.Proxy.Faults.Single());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-INGRESS-REDELIVERY", "consumed-notification-failure-is-not-reclassified-as-saga-fault")]
    public async Task ConsumedNotificationFailure_IsNeverFollowedByAFaultNotificationAsync(bool queried)
    {
        var events = new List<string>();
        var observerFailure = new IngressFailureException("consume observer failed");
        var repository = new RecordingSagaRepository(events);
        FilterContext fixture = CreateFilterContext(events);
        fixture.Proxy.ConsumedNotificationFailure = observerFailure;
        var next = new RecordingPipe<ConsumeContext<IngressMessage>>(events, "downstream");

        Task OperationAsync() => queried
            ? new QuerySagaFilter<IngressSaga, IngressMessage>(
                    repository,
                    new RecordingSagaPolicy(),
                    new RecordingQueryFactory(events, true, new SagaQuery<IngressSaga>(_ => true)),
                    new RecordingPipe<SagaConsumeContext<IngressSaga, IngressMessage>>(events, "saga-pipe"))
                .SendAsync(fixture.Context, next)
            : new CorrelatedSagaFilter<IngressSaga, IngressMessage>(
                    repository,
                    new RecordingSagaPolicy(),
                    new RecordingPipe<SagaConsumeContext<IngressSaga, IngressMessage>>(events, "saga-pipe"))
                .SendAsync(fixture.Context, next);

        IngressFailureException actual = await Assert.ThrowsAsync<IngressFailureException>(OperationAsync);

        Assert.Same(observerFailure, actual);
        Assert.Equal("consumed", events[^1]);
        Assert.Equal(1, fixture.Proxy.ConsumedCalls);
        Assert.Equal(0, fixture.Proxy.FaultedCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-INGRESS-REDELIVERY", "operation-and-fault-observer-failures-preserve-order")]
    public async Task FaultNotificationFailure_PreservesTheOperationAsTheFirstFailureAsync(bool queried)
    {
        var events = new List<string>();
        var operationFailure = new IngressFailureException("repository failed");
        var observerFailure = new IngressFailureException("fault observer failed");
        var repository = new RecordingSagaRepository(events)
        {
            CorrelatedTask = Task.FromException(operationFailure),
            QueryTask = Task.FromException(operationFailure),
        };
        FilterContext fixture = CreateFilterContext(events);
        fixture.Proxy.FaultNotificationFailure = observerFailure;
        var next = new RecordingPipe<ConsumeContext<IngressMessage>>(events, "downstream");

        Task OperationAsync() => queried
            ? new QuerySagaFilter<IngressSaga, IngressMessage>(
                    repository,
                    new RecordingSagaPolicy(),
                    new RecordingQueryFactory(events, true, new SagaQuery<IngressSaga>(_ => true)),
                    new RecordingPipe<SagaConsumeContext<IngressSaga, IngressMessage>>(events, "saga-pipe"))
                .SendAsync(fixture.Context, next)
            : new CorrelatedSagaFilter<IngressSaga, IngressMessage>(
                    repository,
                    new RecordingSagaPolicy(),
                    new RecordingPipe<SagaConsumeContext<IngressSaga, IngressMessage>>(events, "saga-pipe"))
                .SendAsync(fixture.Context, next);

        AggregateException actual = await Assert.ThrowsAsync<AggregateException>(OperationAsync);

        Assert.Collection(actual.InnerExceptions,
            exception => Assert.Same(operationFailure, exception),
            exception => Assert.Same(observerFailure, exception));
        Assert.Same(operationFailure, fixture.Proxy.Faults.Single());
        Assert.Equal(0, next.SendCalls);
        Assert.Equal(0, fixture.Proxy.ConsumedCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-INGRESS-REDELIVERY", "dependency-cancellation-retains-original-inner-cause")]
    public async Task DependencyCancellation_IsWrappedWithTheOriginalCancellationAsInnerCauseAsync(bool queried)
    {
        using var dependencyCancellation = new CancellationTokenSource();
        dependencyCancellation.Cancel();
        var original = new OperationCanceledException("dependency canceled", dependencyCancellation.Token);
        var events = new List<string>();
        var repository = new RecordingSagaRepository(events)
        {
            CorrelatedTask = Task.FromException(original),
            QueryTask = Task.FromException(original),
        };
        FilterContext fixture = CreateFilterContext(events);
        var next = new RecordingPipe<ConsumeContext<IngressMessage>>(events, "downstream");

        Task OperationAsync() => queried
            ? new QuerySagaFilter<IngressSaga, IngressMessage>(
                    repository,
                    new RecordingSagaPolicy(),
                    new RecordingQueryFactory(events, true, new SagaQuery<IngressSaga>(_ => true)),
                    new RecordingPipe<SagaConsumeContext<IngressSaga, IngressMessage>>(events, "saga-pipe"))
                .SendAsync(fixture.Context, next)
            : new CorrelatedSagaFilter<IngressSaga, IngressMessage>(
                    repository,
                    new RecordingSagaPolicy(),
                    new RecordingPipe<SagaConsumeContext<IngressSaga, IngressMessage>>(events, "saga-pipe"))
                .SendAsync(fixture.Context, next);

        ConsumerCanceledException actual = await Assert.ThrowsAsync<ConsumerCanceledException>(OperationAsync);

        Assert.Same(original, actual.InnerException);
        Assert.Same(original, fixture.Proxy.Faults.Single());
        Assert.Equal(0, next.SendCalls);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 0)]
    [InlineData(false, 1)]
    [InlineData(true, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 2)]
    [InlineData(false, 3)]
    [InlineData(true, 3)]
    [RequirementCoverage("REQ-VSB-SAGA-INGRESS-REDELIVERY", "nested-and-unsafe-cancellation-preserves-fault-observation")]
    public async Task SagaFailure_ClassifiesOnlyPureCancellationAndAlwaysReportsTheOriginalFaultAsync(bool queried, int kind)
    {
        Exception failure = kind switch
        {
            0 => new UnsafeBaseFailure(false, new OperationCanceledException("dependency canceled")),
            1 => new UnsafeBaseFailure(true, new OperationCanceledException("dependency canceled")),
            2 => new InvalidOperationException("outer", new AggregateException(
                new OperationCanceledException("first"), new OperationCanceledException("second"))),
            _ => new InvalidOperationException("outer", new AggregateException(
                new OperationCanceledException("first"), new InvalidOperationException("business failure")))
        };
        var events = new List<string>();
        var repository = new RecordingSagaRepository(events)
        {
            CorrelatedTask = Task.FromException(failure),
            QueryTask = Task.FromException(failure)
        };
        FilterContext fixture = CreateFilterContext(events);
        var next = new RecordingPipe<ConsumeContext<IngressMessage>>(events, "downstream");

        Task OperationAsync() => queried
            ? new QuerySagaFilter<IngressSaga, IngressMessage>(
                    repository,
                    new RecordingSagaPolicy(),
                    new RecordingQueryFactory(events, true, new SagaQuery<IngressSaga>(_ => true)),
                    new RecordingPipe<SagaConsumeContext<IngressSaga, IngressMessage>>(events, "saga-pipe"))
                .SendAsync(fixture.Context, next)
            : new CorrelatedSagaFilter<IngressSaga, IngressMessage>(
                    repository,
                    new RecordingSagaPolicy(),
                    new RecordingPipe<SagaConsumeContext<IngressSaga, IngressMessage>>(events, "saga-pipe"))
                .SendAsync(fixture.Context, next);

        Exception actual = await Assert.ThrowsAnyAsync<Exception>(OperationAsync);

        if (kind == 3)
            Assert.Same(failure, actual);
        else
            Assert.Same(failure, Assert.IsType<ConsumerCanceledException>(actual).InnerException);
        Assert.Same(failure, Assert.Single(fixture.Proxy.Faults));
        Assert.Equal(0, fixture.Proxy.ConsumedCalls);
        Assert.Equal(0, next.SendCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-INGRESS-REDELIVERY", "canceled-delivery-preserves-nested-original-fault")]
    public async Task SagaFailure_CanceledDeliveryDoesNotReclassifyNestedDependencyCancellationAsync(bool queried)
    {
        using var delivery = new CancellationTokenSource();
        delivery.Cancel();
        var failure = new InvalidOperationException("outer", new AggregateException(
            new OperationCanceledException("first"), new OperationCanceledException("second")));
        var events = new List<string>();
        var repository = new RecordingSagaRepository(events)
        {
            CorrelatedTask = Task.FromException(failure),
            QueryTask = Task.FromException(failure)
        };
        FilterContext fixture = CreateFilterContextWithCancellation(delivery.Token, events);
        var next = new RecordingPipe<ConsumeContext<IngressMessage>>(events, "downstream");

        Task OperationAsync() => queried
            ? new QuerySagaFilter<IngressSaga, IngressMessage>(
                    repository,
                    new RecordingSagaPolicy(),
                    new RecordingQueryFactory(events, true, new SagaQuery<IngressSaga>(_ => true)),
                    new RecordingPipe<SagaConsumeContext<IngressSaga, IngressMessage>>(events, "saga-pipe"))
                .SendAsync(fixture.Context, next)
            : new CorrelatedSagaFilter<IngressSaga, IngressMessage>(
                    repository,
                    new RecordingSagaPolicy(),
                    new RecordingPipe<SagaConsumeContext<IngressSaga, IngressMessage>>(events, "saga-pipe"))
                .SendAsync(fixture.Context, next);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(OperationAsync);

        Assert.Same(failure, actual);
        Assert.Same(failure, Assert.Single(fixture.Proxy.Faults));
        Assert.Equal(0, fixture.Proxy.ConsumedCalls);
        Assert.Equal(0, next.SendCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-INGRESS-REDELIVERY", "classified-consumer-cancellation-preserves-identity")]
    public async Task ClassifiedConsumerCancellation_IsNeverWrappedAgainAsync(bool queried)
    {
        var original = new ConsumerCanceledException("already classified");
        var events = new List<string>();
        var repository = new RecordingSagaRepository(events)
        {
            CorrelatedTask = Task.FromException(original),
            QueryTask = Task.FromException(original),
        };
        FilterContext fixture = CreateFilterContext(events);
        var next = new RecordingPipe<ConsumeContext<IngressMessage>>(events, "downstream");

        Task OperationAsync() => queried
            ? new QuerySagaFilter<IngressSaga, IngressMessage>(
                    repository,
                    new RecordingSagaPolicy(),
                    new RecordingQueryFactory(events, true, new SagaQuery<IngressSaga>(_ => true)),
                    new RecordingPipe<SagaConsumeContext<IngressSaga, IngressMessage>>(events, "saga-pipe"))
                .SendAsync(fixture.Context, next)
            : new CorrelatedSagaFilter<IngressSaga, IngressMessage>(
                    repository,
                    new RecordingSagaPolicy(),
                    new RecordingPipe<SagaConsumeContext<IngressSaga, IngressMessage>>(events, "saga-pipe"))
                .SendAsync(fixture.Context, next);

        ConsumerCanceledException actual = await Assert.ThrowsAsync<ConsumerCanceledException>(OperationAsync);

        Assert.Same(original, actual);
        Assert.Same(original, fixture.Proxy.Faults.Single());
        Assert.Equal(0, next.SendCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INGRESS-REDELIVERY", "redelivery-constructor-send-probe-and-option-boundaries")]
    public async Task RedeliveryPipe_RejectsMissingInputsAndUnsupportedOptionsDeterministicallyAsync()
    {
        var policy = new RecordingRetryPolicy([false]);
        var finalPipe = new RecordingPipe<ConsumeContext<IngressMessage>>([], "final");
        object observers = CreateRetryObservable();

        AssertReflectedArgument("retryPolicy", () => CreateRedeliveryPipe(null!, observers, finalPipe, RedeliveryOptions.None));
        AssertReflectedArgument("observers", () => CreateRedeliveryPipe(policy, null!, finalPipe, RedeliveryOptions.None));
        AssertReflectedArgument("finalPipe", () => CreateRedeliveryPipe(policy, observers, null!, RedeliveryOptions.None));
        AssertReflectedArgument("options", () => CreateRedeliveryPipe(policy, observers, finalPipe, (RedeliveryOptions)4), outOfRange: true);

        IPipe<ConsumeContext<IngressMessage>> pipe = CreateRedeliveryPipe(policy, observers, finalPipe, RedeliveryOptions.None);
        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() => pipe.SendAsync(null!))).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => pipe.Probe(null!)).ParamName);
        Assert.Equal(0, policy.CreateCalls);
        Assert.Equal(0, finalPipe.SendCalls);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [RequirementCoverage("REQ-VSB-SAGA-INGRESS-REDELIVERY", "null-policy-context-and-null-consume-context-disposal")]
    public async Task RedeliveryPipe_RejectsImpossiblePolicyContextsAndDisposesEveryAcquiredContextAsync(bool nullPolicyContext)
    {
        var policy = new RecordingRetryPolicy([false])
        {
            ReturnNullPolicyContext = nullPolicyContext,
            ReturnNullConsumeContext = !nullPolicyContext,
        };
        IPipe<ConsumeContext<IngressMessage>> pipe = CreateRedeliveryPipe(
            policy,
            CreateRetryObservable(),
            new RecordingPipe<ConsumeContext<IngressMessage>>([], "final"),
            RedeliveryOptions.None);
        FilterContext fixture = CreateFilterContext();

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() => pipe.SendAsync(fixture.Context));

        Assert.Equal(nullPolicyContext
            ? "The retry policy returned a null policy context."
            : "The retry policy returned a policy context without a consume context.", actual.Message);
        Assert.Equal(1, policy.CreateCalls);
        Assert.Equal(nullPolicyContext ? 0 : 1, policy.DisposeCalls);
        Assert.Equal(0, policy.DecisionCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INGRESS-REDELIVERY", "redelivery-operation-and-policy-disposal-failures-preserve-order")]
    public async Task RedeliveryPipe_PreservesOperationBeforePolicyContextDisposalFailureAsync()
    {
        var operationFailure = new IngressFailureException("terminal pipe failed");
        var disposalFailure = new InvalidOperationException("policy context disposal failed");
        var policy = new RecordingRetryPolicy([false]) { DisposeFailure = disposalFailure };
        var finalPipe = new RecordingPipe<ConsumeContext<IngressMessage>>(
            [],
            "final",
            _ => Task.FromException(operationFailure));
        IPipe<ConsumeContext<IngressMessage>> pipe = CreateRedeliveryPipe(
            policy,
            CreateRetryObservable(),
            finalPipe,
            RedeliveryOptions.None);
        FilterContext fixture = CreateFilterContext();

        AggregateException actual = await Assert.ThrowsAsync<AggregateException>(() => pipe.SendAsync(fixture.Context));

        Assert.Collection(
            actual.InnerExceptions,
            exception => Assert.Same(operationFailure, exception),
            exception => Assert.Same(disposalFailure, exception));
        Assert.Equal(1, finalPipe.SendCalls);
        Assert.Equal(1, policy.DisposeCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INGRESS-REDELIVERY", "redelivery-observer-order-and-failure-cleanup")]
    public async Task RedeliveryPipe_NotifiesObserversInOrderAndDisposesAfterObserverFailureAsync()
    {
        var scheduledEvents = new List<string>();
        var scheduledObserver = new RecordingRetryObserver(scheduledEvents);
        var scheduledPolicy = new RecordingRetryPolicy([true]);
        IPipe<ConsumeContext<IngressMessage>> scheduledPipe = CreateRedeliveryPipe(
            scheduledPolicy,
            CreateRetryObservable(scheduledObserver),
            new RecordingPipe<ConsumeContext<IngressMessage>>(scheduledEvents, "final"),
            RedeliveryOptions.None);

        await scheduledPipe.SendAsync(CreateFilterContext(outgoing: new OutgoingMessageRecorder()).Context);

        Assert.Equal(["observer-post-create", "observer-post-fault"], scheduledEvents);
        Assert.Equal(1, scheduledPolicy.DisposeCalls);

        var exhaustedEvents = new List<string>();
        var exhaustedObserver = new RecordingRetryObserver(exhaustedEvents);
        var exhaustedPolicy = new RecordingRetryPolicy([false], events: exhaustedEvents);
        IPipe<ConsumeContext<IngressMessage>> exhaustedPipe = CreateRedeliveryPipe(
            exhaustedPolicy,
            CreateRetryObservable(exhaustedObserver),
            new RecordingPipe<ConsumeContext<IngressMessage>>(exhaustedEvents, "final"),
            RedeliveryOptions.None);

        await exhaustedPipe.SendAsync(CreateFilterContext().Context);

        Assert.Equal(["observer-post-create", "retry-fault", "observer-retry-fault", "final"], exhaustedEvents);
        Assert.Equal(1, exhaustedPolicy.DisposeCalls);

        var observerFailure = new IngressFailureException("observer post-create failed");
        var failingObserver = new RecordingRetryObserver([]) { PostCreateFailure = observerFailure };
        var failurePolicy = new RecordingRetryPolicy([true]);
        IPipe<ConsumeContext<IngressMessage>> failingPipe = CreateRedeliveryPipe(
            failurePolicy,
            CreateRetryObservable(failingObserver),
            new RecordingPipe<ConsumeContext<IngressMessage>>([], "final"),
            RedeliveryOptions.None);

        IngressFailureException actual = await Assert.ThrowsAsync<IngressFailureException>(() =>
            failingPipe.SendAsync(CreateFilterContext().Context));

        Assert.Same(observerFailure, actual);
        Assert.Equal(1, failurePolicy.DisposeCalls);
        Assert.Equal(0, failurePolicy.DecisionCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INGRESS-REDELIVERY", "redelivery-defensive-retry-context-and-unhandled-paths")]
    public async Task RedeliveryPipe_RejectsNullRetryContextAndSkipsFaultNotificationWhenUnhandledAsync()
    {
        var nullRetryPolicy = new RecordingRetryPolicy([true]) { ReturnNullRetryContext = true };
        var nullRetryFinal = new RecordingPipe<ConsumeContext<IngressMessage>>([], "final");
        IPipe<ConsumeContext<IngressMessage>> nullRetryPipe = CreateRedeliveryPipe(
            nullRetryPolicy,
            CreateRetryObservable(),
            nullRetryFinal,
            RedeliveryOptions.None);

        InvalidOperationException nullRetryFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            nullRetryPipe.SendAsync(CreateFilterContext().Context));

        Assert.Equal("The retry policy returned a null retry context.", nullRetryFailure.Message);
        Assert.Equal(1, nullRetryPolicy.DisposeCalls);
        Assert.Equal(0, nullRetryFinal.SendCalls);

        var unhandledPolicy = new RecordingRetryPolicy([false]) { IsHandledResult = false };
        var unhandledFinal = new RecordingPipe<ConsumeContext<IngressMessage>>([], "final");
        IPipe<ConsumeContext<IngressMessage>> unhandledPipe = CreateRedeliveryPipe(
            unhandledPolicy,
            CreateRetryObservable(),
            unhandledFinal,
            RedeliveryOptions.None);

        await unhandledPipe.SendAsync(CreateFilterContext().Context);

        Assert.Equal(0, unhandledPolicy.RetryFaultCalls);
        Assert.Equal(1, unhandledFinal.SendCalls);
        Assert.Equal(1, unhandledPolicy.DisposeCalls);

        var disposalFailure = new IngressFailureException("dispose only failed");
        var disposalPolicy = new RecordingRetryPolicy([false]) { DisposeFailure = disposalFailure };
        IPipe<ConsumeContext<IngressMessage>> disposalPipe = CreateRedeliveryPipe(
            disposalPolicy,
            CreateRetryObservable(),
            new RecordingPipe<ConsumeContext<IngressMessage>>([], "final"),
            RedeliveryOptions.None);

        IngressFailureException actual = await Assert.ThrowsAsync<IngressFailureException>(() =>
            disposalPipe.SendAsync(CreateFilterContext().Context));

        Assert.Same(disposalFailure, actual);
        Assert.Equal(1, disposalPolicy.DisposeCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INGRESS-REDELIVERY", "pre-cancellation-has-zero-redelivery-effects")]
    public async Task RedeliveryPipe_PreCanceledContextStopsBeforePolicyAndTerminalEffectsAsync()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var policy = new RecordingRetryPolicy([false]);
        var finalPipe = new RecordingPipe<ConsumeContext<IngressMessage>>([], "final");
        var scheduler = CreateScheduler(out RecordingSchedulerProxy schedulerRecorder);
        FilterContext fixture = CreateFilterContextWithCancellation(cancellation.Token, scheduler: scheduler);
        IPipe<ConsumeContext<IngressMessage>> pipe = CreateRedeliveryPipe(
            policy,
            CreateRetryObservable(),
            finalPipe,
            RedeliveryOptions.ConfigureMessageScheduler);

        OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pipe.SendAsync(fixture.Context));

        Assert.Equal(cancellation.Token, actual.CancellationToken);
        Assert.Equal(0, policy.CreateCalls);
        Assert.Equal(0, policy.DecisionCalls);
        Assert.Equal(0, finalPipe.SendCalls);
        Assert.Equal(0, schedulerRecorder.CallCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INGRESS-REDELIVERY", "cancellation-after-policy-acquisition-disposes-before-effects")]
    public async Task RedeliveryPipe_CancellationDuringPolicyAcquisitionDisposesAndStopsBeforeRetryEffectsAsync()
    {
        using var cancellation = new CancellationTokenSource();
        var policy = new RecordingRetryPolicy([true]) { OnCreate = cancellation.Cancel };
        var finalPipe = new RecordingPipe<ConsumeContext<IngressMessage>>([], "final");
        FilterContext fixture = CreateFilterContextWithCancellation(cancellation.Token);
        IPipe<ConsumeContext<IngressMessage>> pipe = CreateRedeliveryPipe(
            policy,
            CreateRetryObservable(),
            finalPipe,
            RedeliveryOptions.None);

        OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pipe.SendAsync(fixture.Context));

        Assert.Equal(cancellation.Token, actual.CancellationToken);
        Assert.Equal(1, policy.CreateCalls);
        Assert.Equal(1, policy.DisposeCalls);
        Assert.Equal(0, policy.DecisionCalls);
        Assert.Equal(0, finalPipe.SendCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INGRESS-REDELIVERY", "exact-replay-schedule-and-exhaustion-with-absent-correlation")]
    public async Task RedeliveryPipe_ReplaysEveryPreviousAttemptExactlyAndTerminatesAtThePolicyBoundaryAsync()
    {
        var scheduledPolicy = new RecordingRetryPolicy([true, true, true], TimeSpan.FromSeconds(7));
        var scheduledFinal = new RecordingPipe<ConsumeContext<IngressMessage>>([], "scheduled-final");
        var outgoing = new OutgoingMessageRecorder();
        FilterContext scheduledContext = CreateFilterContext(redeliveryCount: 2, outgoing: outgoing, correlationId: null);
        IPipe<ConsumeContext<IngressMessage>> scheduledPipe = CreateRedeliveryPipe(
            scheduledPolicy,
            CreateRetryObservable(),
            scheduledFinal,
            RedeliveryOptions.None);

        await scheduledPipe.SendAsync(scheduledContext.Context);

        Assert.Equal(3, scheduledPolicy.DecisionCalls);
        Assert.Equal(1, scheduledPolicy.DisposeCalls);
        Assert.Equal(0, scheduledFinal.SendCalls);
        SagaException missing = Assert.IsType<SagaException>(scheduledPolicy.LastException);
        Assert.Null(missing.CorrelationId);
        OutgoingMessageRecorder.SendObservation send = Assert.Single(outgoing.SendObservations);
        Assert.Equal(TimeSpan.FromSeconds(7), send.Delay);
        Assert.Equal(3, send.RedeliveryCount);

        var terminalEvents = new List<string>();
        var exhaustedPolicy = new RecordingRetryPolicy([true, true, false], TimeSpan.FromSeconds(7), terminalEvents);
        var exhaustedFinal = new RecordingPipe<ConsumeContext<IngressMessage>>(terminalEvents, "final");
        var exhaustedOutgoing = new OutgoingMessageRecorder();
        FilterContext exhaustedContext = CreateFilterContext(redeliveryCount: 2, outgoing: exhaustedOutgoing);
        IPipe<ConsumeContext<IngressMessage>> exhaustedPipe = CreateRedeliveryPipe(
            exhaustedPolicy,
            CreateRetryObservable(),
            exhaustedFinal,
            RedeliveryOptions.None);

        await exhaustedPipe.SendAsync(exhaustedContext.Context);

        Assert.Equal(3, exhaustedPolicy.DecisionCalls);
        Assert.Equal(1, exhaustedPolicy.RetryFaultCalls);
        Assert.Equal(1, exhaustedPolicy.DisposeCalls);
        Assert.Equal(1, exhaustedFinal.SendCalls);
        Assert.Empty(exhaustedOutgoing.Messages);
        Assert.Equal(["retry-fault", "final"], terminalEvents);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-SAGA-INGRESS-REDELIVERY", "scheduler-and-message-identity-option-matrix")]
    public async Task RedeliveryPipe_UsesTheSelectedSchedulerAndMessageIdentityOptionsAsync(bool useScheduler, bool replaceMessageId)
    {
        Guid originalMessageId = Guid.NewGuid();
        var outgoing = new OutgoingMessageRecorder();
        IAdvancedMessageScheduler scheduler = CreateScheduler(out RecordingSchedulerProxy schedulerRecorder);
        FilterContext fixture = CreateFilterContext(
            scheduler: scheduler,
            outgoing: outgoing,
            messageId: originalMessageId,
            redeliveryCount: 0);
        var options = useScheduler ? RedeliveryOptions.ConfigureMessageScheduler : RedeliveryOptions.None;
        if (replaceMessageId)
            options |= RedeliveryOptions.ReplaceMessageId;
        IPipe<ConsumeContext<IngressMessage>> pipe = CreateRedeliveryPipe(
            new RecordingRetryPolicy([true], TimeSpan.FromSeconds(4)),
            CreateRetryObservable(),
            new RecordingPipe<ConsumeContext<IngressMessage>>([], "final"),
            options);

        await pipe.SendAsync(fixture.Context);

        Assert.Equal(useScheduler ? 1 : 0, schedulerRecorder.CallCount);
        Assert.Equal(useScheduler ? 0 : 1, outgoing.SendObservations.Count);
        Guid? emittedMessageId = useScheduler
            ? schedulerRecorder.MessageId
            : Assert.Single(outgoing.SendObservations).MessageId;
        int? redeliveryCount = useScheduler
            ? schedulerRecorder.RedeliveryCount
            : Assert.Single(outgoing.SendObservations).RedeliveryCount;
        if (replaceMessageId)
        {
            Assert.NotNull(emittedMessageId);
            Assert.NotEqual(originalMessageId, emittedMessageId.Value);
        }
        else
            Assert.Equal(originalMessageId, emittedMessageId);
        Assert.Equal(1, redeliveryCount);
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(10_000, false)]
    [InlineData(10_001, true)]
    [RequirementCoverage("REQ-VSB-SAGA-INGRESS-REDELIVERY", "bounded-hostile-previous-delivery-count")]
    public async Task RedeliveryPipe_BoundsUntrustedPreviousDeliveryCountsBeforeReplayAsync(int previousDeliveryCount, bool rejected)
    {
        var policy = new RecordingRetryPolicy([false]);
        var finalPipe = new RecordingPipe<ConsumeContext<IngressMessage>>([], "final");
        FilterContext fixture = CreateFilterContext(redeliveryCount: previousDeliveryCount);
        IPipe<ConsumeContext<IngressMessage>> pipe = CreateRedeliveryPipe(
            policy,
            CreateRetryObservable(),
            finalPipe,
            RedeliveryOptions.None);

        if (rejected)
        {
            InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() => pipe.SendAsync(fixture.Context));
            Assert.Equal("The previous redelivery count must be between 0 and 10000.", actual.Message);
            Assert.Equal(0, policy.DecisionCalls);
            Assert.Equal(0, finalPipe.SendCalls);
        }
        else
        {
            await pipe.SendAsync(fixture.Context);
            Assert.Equal(1, policy.DecisionCalls);
            Assert.Equal(1, finalPipe.SendCalls);
        }

        Assert.Equal(1, policy.DisposeCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INGRESS-REDELIVERY", "redelivery-probe-includes-policy-terminal-pipe-and-bounds")]
    public void RedeliveryPipe_ProbesEveryOwnerAndReportsStableMetadata()
    {
        var events = new List<string>();
        var policy = new RecordingRetryPolicy([false], events: events);
        var finalPipe = new RecordingPipe<ConsumeContext<IngressMessage>>(events, "final-probe");
        IPipe<ConsumeContext<IngressMessage>> pipe = CreateRedeliveryPipe(
            policy,
            CreateRetryObservable(),
            finalPipe,
            RedeliveryOptions.ReplaceMessageId | RedeliveryOptions.ConfigureMessageScheduler);
        var probe = new RecordingProbeContext(events);

        pipe.Probe(probe);

        Assert.Equal(["policy-probe", "final-probe"], events.Where(x => x.EndsWith("probe", StringComparison.Ordinal)).ToArray());
        Assert.Contains("missingInstanceRedelivery", probe.ScopeKeys);
        Assert.Equal(TypeCache<IngressSaga>.ShortName, probe.Values["sagaType"]);
        Assert.Equal(TypeCache<IngressMessage>.ShortName, probe.Values["messageType"]);
        Assert.Equal(true, probe.Values["replaceMessageId"]);
        Assert.Equal(true, probe.Values["configureMessageScheduler"]);
        Assert.Equal(10_000, probe.Values["maximumPreviousDeliveryCount"]);
        Assert.Equal(1, policy.ProbeCalls);
        Assert.Equal(1, finalPipe.ProbeCalls);
    }

    static FilterContext CreateFilterContext(
        List<string>? events = null,
        IMessageScheduler? scheduler = null,
        OutgoingMessageRecorder? outgoing = null,
        Guid? messageId = null,
        int? redeliveryCount = null,
        Guid? correlationId = null) =>
        CreateFilterContextWithCancellation(
            TestContext.Current.CancellationToken,
            events,
            scheduler,
            outgoing,
            messageId,
            redeliveryCount,
            correlationId);

    static FilterContext CreateFilterContextWithCancellation(
        CancellationToken cancellationToken,
        List<string>? events = null,
        IMessageScheduler? scheduler = null,
        OutgoingMessageRecorder? outgoing = null,
        Guid? messageId = null,
        int? redeliveryCount = null,
        Guid? correlationId = null)
    {
        ConsumeContext<IngressMessage> inner = InMemoryOutboxTestContextFactory.Create(
            new IngressMessage(),
            cancellationToken,
            scheduler,
            outgoing,
            messageId: messageId,
            correlationId: correlationId);
        TestConsumeContext context = DispatchProxy.Create<TestConsumeContext, FilterConsumeContextProxy>();
        var proxy = (FilterConsumeContextProxy)(object)context;
        proxy.Configure(inner, events ?? []);
        if (redeliveryCount.HasValue)
            proxy.Headers.Set(MessageHeaders.RedeliveryCount, redeliveryCount.Value);

        return new FilterContext(context, proxy);
    }

    static object CreateRetryObservable(IRetryObserver? observer = null)
    {
        Type? type = typeof(IRetryObserver).Assembly.GetType("ViciOne.ServiceBus.Observables.RetryObservable");
        Assert.NotNull(type);
        object observable = Assert.IsAssignableFrom<object>(Activator.CreateInstance(type, nonPublic: true));
        if (observer is not null)
        {
            MethodInfo connect = Assert.Single(
                type.GetMethods(BindingFlags.Instance | BindingFlags.Public),
                method => method.Name == "Connect");
            Assert.IsAssignableFrom<ConnectHandle>(connect.Invoke(observable, [observer]));
        }

        return observable;
    }

    static IPipe<ConsumeContext<IngressMessage>> CreateRedeliveryPipe(
        IRetryPolicy retryPolicy,
        object observers,
        IPipe<ConsumeContext<IngressMessage>> finalPipe,
        RedeliveryOptions options)
    {
        Type? openType = typeof(CorrelatedSagaFilter<,>).Assembly.GetType(
            "ViciOne.ServiceBus.Middleware.MissingInstanceRedeliveryPipe`2");
        Assert.NotNull(openType);
        Type type = openType.MakeGenericType(typeof(IngressSaga), typeof(IngressMessage));
        ConstructorInfo constructor = Assert.Single(type.GetConstructors(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));
        return Assert.IsAssignableFrom<IPipe<ConsumeContext<IngressMessage>>>(
            constructor.Invoke([retryPolicy, observers, finalPipe, options]));
    }

    static MessageSchedulerContext CreateScheduler(out RecordingSchedulerProxy recorder)
    {
        MessageSchedulerContext scheduler = DispatchProxy.Create<MessageSchedulerContext, RecordingSchedulerProxy>();
        recorder = (RecordingSchedulerProxy)(object)scheduler;
        return scheduler;
    }

    static void AssertArgument(string parameterName, Action action)
    {
        ArgumentNullException actual = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(parameterName, actual.ParamName);
    }

    static void AssertReflectedArgument(string parameterName, Action action, bool outOfRange = false)
    {
        TargetInvocationException invocation = Assert.Throws<TargetInvocationException>(action);
        ArgumentException actual = outOfRange
            ? Assert.IsType<ArgumentOutOfRangeException>(invocation.InnerException)
            : Assert.IsType<ArgumentNullException>(invocation.InnerException);
        Assert.Equal(parameterName, actual.ParamName);
    }

    public interface TestConsumeContext :
        ConsumeContext<IngressMessage>,
        ConsumeContext;

    private sealed record FilterContext(ConsumeContext<IngressMessage> Context, FilterConsumeContextProxy Proxy);

    private class FilterConsumeContextProxy : DispatchProxy
    {
        private ConsumeContext<IngressMessage> _inner = null!;
        private List<string> _events = null!;

        public DictionarySendHeaders Headers { get; } = new();
        public List<Exception> Faults { get; } = [];
        public Exception? ConsumedNotificationFailure { get; set; }
        public Exception? FaultNotificationFailure { get; set; }
        public int ConsumedCalls { get; private set; }
        public int FaultedCalls { get; private set; }

        public void Configure(ConsumeContext<IngressMessage> inner, List<string> events)
        {
            _inner = inner;
            _events = events;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            ArgumentNullException.ThrowIfNull(args);

            switch (targetMethod.Name)
            {
                case "get_Headers":
                    return Headers;

                case "NotifyConsumedAsync":
                    ConsumedCalls++;
                    _events.Add("consumed");
                    return ConsumedNotificationFailure is null
                        ? Task.CompletedTask
                        : Task.FromException(ConsumedNotificationFailure);

                case "NotifyFaultedAsync" when targetMethod.IsGenericMethod:
                    FaultedCalls++;
                    _events.Add("faulted");
                    Faults.Add(Assert.IsAssignableFrom<Exception>(args[3]));
                    return FaultNotificationFailure is null
                        ? Task.CompletedTask
                        : Task.FromException(FaultNotificationFailure);
            }

            try
            {
                return targetMethod.Invoke(_inner, args);
            }
            catch (TargetInvocationException exception) when (exception.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
        }
    }

    private sealed class RecordingSagaRepository(List<string> events) : ISagaRepository<IngressSaga>
    {
        public Task CorrelatedTask { get; init; } = Task.CompletedTask;
        public Task QueryTask { get; init; } = Task.CompletedTask;
        public int CorrelatedCalls { get; private set; }
        public int QueryCalls { get; private set; }
        public ISagaQuery<IngressSaga>? Query { get; private set; }

        public Task SendAsync<T>(
            ConsumeContext<T> context,
            ISagaPolicy<IngressSaga, T> policy,
            IPipe<SagaConsumeContext<IngressSaga, T>> next)
            where T : class
        {
            CorrelatedCalls++;
            events.Add("repository-correlated");
            return CorrelatedTask;
        }

        public Task SendQueryAsync<T>(
            ConsumeContext<T> context,
            ISagaQuery<IngressSaga> query,
            ISagaPolicy<IngressSaga, T> policy,
            IPipe<SagaConsumeContext<IngressSaga, T>> next)
            where T : class
        {
            QueryCalls++;
            Query = query;
            events.Add("repository-query");
            return QueryTask;
        }

        public void Probe(ProbeContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            events.Add("repository-probe");
        }
    }

    private sealed class RecordingQueryFactory(
        List<string> events,
        bool result,
        ISagaQuery<IngressSaga>? query) : ISagaQueryFactory<IngressSaga, IngressMessage>
    {
        public Exception? Failure { get; init; }

        public bool TryCreateQuery(
            ConsumeContext<IngressMessage> context,
            [NotNullWhen(true)] out ISagaQuery<IngressSaga>? created)
        {
            events.Add("query");
            if (Failure is not null)
                throw Failure;

            created = query;
            return result;
        }

        public void Probe(ProbeContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            events.Add("query-probe");
        }
    }

    private sealed class RecordingSagaPolicy : ISagaPolicy<IngressSaga, IngressMessage>
    {
        public bool IsReadOnly => false;

        public bool PreInsertInstance(ConsumeContext<IngressMessage> context, [NotNullWhen(true)] out IngressSaga? instance)
        {
            instance = null;
            return false;
        }

        public Task ExistingAsync(
            SagaConsumeContext<IngressSaga, IngressMessage> context,
            IPipe<SagaConsumeContext<IngressSaga, IngressMessage>> next) =>
            throw new NotSupportedException();

        public Task MissingAsync(
            ConsumeContext<IngressMessage> context,
            IPipe<SagaConsumeContext<IngressSaga, IngressMessage>> next) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingPipe<TContext>(
        List<string> events,
        string eventName,
        Func<TContext, Task>? callback = null) : IPipe<TContext>
        where TContext : class, PipeContext
    {
        public int SendCalls { get; private set; }
        public int ProbeCalls { get; private set; }

        public Task SendAsync(TContext context)
        {
            SendCalls++;
            events.Add(eventName);
            return callback?.Invoke(context) ?? Task.CompletedTask;
        }

        public void Probe(ProbeContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            ProbeCalls++;
            events.Add(eventName.EndsWith("probe", StringComparison.Ordinal) ? eventName : $"{eventName}-probe");
        }
    }

    private sealed class RecordingRetryPolicy(
        IReadOnlyList<bool> decisions,
        TimeSpan? delay = null,
        List<string>? events = null) : IRetryPolicy
    {
        public bool ReturnNullPolicyContext { get; init; }
        public bool ReturnNullConsumeContext { get; init; }
        public bool ReturnNullRetryContext { get; init; }
        public bool IsHandledResult { get; init; } = true;
        public Exception? DisposeFailure { get; init; }
        public Action? OnCreate { get; init; }
        public int CreateCalls { get; private set; }
        public int DecisionCalls { get; private set; }
        public int DisposeCalls { get; private set; }
        public int RetryFaultCalls { get; private set; }
        public int ProbeCalls { get; private set; }
        public Exception? LastException { get; private set; }

        public void Probe(ProbeContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            ProbeCalls++;
            events?.Add("policy-probe");
        }

        public bool IsHandled(Exception exception)
        {
            ArgumentNullException.ThrowIfNull(exception);
            return IsHandledResult;
        }

        public RetryPolicyContext<T> CreatePolicyContext<T>(T context)
            where T : class, PipeContext
        {
            CreateCalls++;
            OnCreate?.Invoke();
            if (ReturnNullPolicyContext)
                return null!;

            return new RecordingRetryPolicyContext<T>(this, context, decisions, delay, ReturnNullConsumeContext, events);
        }

        private sealed class RecordingRetryPolicyContext<T>(
            RecordingRetryPolicy owner,
            T context,
            IReadOnlyList<bool> decisions,
            TimeSpan? delay,
            bool nullContext,
            List<string>? events) : RetryPolicyContext<T>
            where T : class, PipeContext
        {
            private int _index;

            public T Context => nullContext ? null! : context;

            public bool CanRetry(Exception exception, out RetryContext<T> retryContext) =>
                Next(exception, out retryContext);

            public Task RetryFaultedAsync(Exception exception, CancellationToken cancellationToken = default) =>
                Task.CompletedTask;

            public void Cancel()
            {
            }

            public void Dispose()
            {
                owner.DisposeCalls++;
                if (owner.DisposeFailure is not null)
                    throw owner.DisposeFailure;
            }

            public bool Next(Exception exception, out RetryContext<T> retryContext)
            {
                owner.DecisionCalls++;
                owner.LastException = exception;
                bool result = _index < decisions.Count && decisions[_index];
                _index++;
                retryContext = owner.ReturnNullRetryContext
                    ? null!
                    : new RecordingRetryContext<T>(this, context, exception, _index, delay, owner, events);
                return result;
            }
        }

        private sealed class RecordingRetryContext<T>(
            RecordingRetryPolicyContext<T> ownerContext,
            T context,
            Exception exception,
            int attempt,
            TimeSpan? delay,
            RecordingRetryPolicy owner,
            List<string>? events) : RetryContext<T>
            where T : class, PipeContext
        {
            public T Context => context;
            public Exception Exception => exception;
            public int RetryCount => attempt - 1;
            public int RetryAttempt => attempt;
            public Type ContextType => typeof(T);
            public TimeSpan? Delay => delay;
            public CancellationToken CancellationToken => context.CancellationToken;

            public Task PreRetryAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

            public Task RetryFaultedAsync(Exception fault, CancellationToken cancellationToken = default)
            {
                owner.RetryFaultCalls++;
                events?.Add("retry-fault");
                return Task.CompletedTask;
            }

            public bool CanRetry(Exception fault, out RetryContext<T> retryContext) =>
                ownerContext.Next(fault, out retryContext);
        }
    }

    private sealed class RecordingRetryObserver(List<string> events) : IRetryObserver
    {
        public Exception? PostCreateFailure { get; init; }

        public Task PostCreateAsync<T>(RetryPolicyContext<T> context)
            where T : class, PipeContext
        {
            events.Add("observer-post-create");
            return PostCreateFailure is null ? Task.CompletedTask : Task.FromException(PostCreateFailure);
        }

        public Task PostFaultAsync<T>(RetryContext<T> context)
            where T : class, PipeContext
        {
            events.Add("observer-post-fault");
            return Task.CompletedTask;
        }

        public Task PreRetryAsync<T>(RetryContext<T> context)
            where T : class, PipeContext
        {
            events.Add("observer-pre-retry");
            return Task.CompletedTask;
        }

        public Task RetryFaultAsync<T>(RetryContext<T> context)
            where T : class, PipeContext
        {
            events.Add("observer-retry-fault");
            return Task.CompletedTask;
        }

        public Task RetryCompleteAsync<T>(RetryContext<T> context)
            where T : class, PipeContext
        {
            events.Add("observer-retry-complete");
            return Task.CompletedTask;
        }
    }

    private class RecordingSchedulerProxy : DispatchProxy
    {
        public int CallCount { get; private set; }
        public Guid? MessageId { get; private set; }
        public int? RedeliveryCount { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            ArgumentNullException.ThrowIfNull(args);
            if (targetMethod.Name == "get_TimeProvider")
                return TimeProvider.System;

            if (targetMethod.Name == "ScheduleSendAsync"
                && targetMethod.IsGenericMethod
                && targetMethod.GetGenericArguments()[0] == typeof(IngressMessage)
                && args.Length == 4
                && args[2] is IPipe<SendContext> pipe)
            {
                return RecordAsync(
                    new Uri("loopback://localhost/in-memory-outbox-test"),
                    Assert.IsType<DateTimeOffset>(args[0]),
                    Assert.IsType<IngressMessage>(args[1]),
                    pipe,
                    Assert.IsType<CancellationToken>(args[3]));
            }

            throw new NotSupportedException(targetMethod.ToString());
        }

        private async Task<ScheduledMessage<IngressMessage>> RecordAsync(
            Uri destination,
            DateTimeOffset dueAt,
            IngressMessage message,
            IPipe<SendContext> pipe,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sendContext = new MessageSendContext<IngressMessage>(message, cancellationToken);
            await pipe.SendAsync(sendContext);
            CallCount++;
            MessageId = sendContext.MessageId;
            RedeliveryCount = sendContext.Headers.Get(MessageHeaders.RedeliveryCount, default(int?));
            return new RecordedScheduledMessage<IngressMessage>(Guid.NewGuid(), dueAt, destination, message);
        }
    }

    private sealed record RecordedScheduledMessage<T>(Guid TokenId, DateTimeOffset DueAt, Uri Destination, T Payload) :
        ScheduledMessage<T>
        where T : class;

    private sealed class RecordingProbeContext(
        List<string> events,
        List<string>? scopeKeys = null,
        Dictionary<string, object?>? values = null) : ProbeContext
    {
        public CancellationToken CancellationToken => default;
        public List<string> ScopeKeys { get; } = scopeKeys ?? [];
        public Dictionary<string, object?> Values { get; } = values ?? [];

        public void Add(string key, string? value) => Values[key] = value;
        public void Add(string key, object? value) => Values[key] = value;

        public void Set(object values)
        {
            foreach (PropertyInfo property in values.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
                Values[property.Name] = property.GetValue(values);
        }

        public void Set(IEnumerable<KeyValuePair<string, object?>> values)
        {
            foreach (KeyValuePair<string, object?> pair in values)
                Values[pair.Key] = pair.Value;
        }

        public ProbeContext CreateScope(string key)
        {
            ScopeKeys.Add(key);
            events.Add($"scope:{key}");
            return new RecordingProbeContext(events, ScopeKeys, Values);
        }
    }

    public sealed class IngressSaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed record IngressMessage;

    private sealed class UnsafeBaseFailure(bool nullBase, Exception inner) : Exception("outer", inner)
    {
        public override Exception GetBaseException() => nullBase
            ? null!
            : throw new InvalidOperationException("base lookup failed");
    }

    private sealed class IngressFailureException(string message) : Exception(message);
}

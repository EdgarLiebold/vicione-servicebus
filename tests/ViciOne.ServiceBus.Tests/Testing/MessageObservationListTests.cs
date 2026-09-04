using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Testing.Implementations;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Testing;

public sealed class MessageObservationListTests
{
    private static readonly DateTimeOffset ObservationTime =
        new(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-OBSERVATION-LIST", "sent-query-surface")]
    public async Task SentList_QuerySurfaceAppliesTypePredicatesAndConfiguredFiltersExactlyAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        harness.OnConfigureInMemoryReceiveEndpoint += configurator =>
        {
            configurator.Handler<ObservedMessage>(_ => Task.CompletedTask);
            configurator.Handler<OtherMessage>(_ => Task.CompletedTask);
        };

        await harness.StartAsync(cancellationToken);
        try
        {
            var expected = new ObservedMessage(NewId.NextGuid(), "expected");
            var other = new OtherMessage(NewId.NextGuid(), "other");
            await harness.InputQueueSendEndpoint.SendAsync(expected, cancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync(other, cancellationToken);
            Assert.True(await harness.Consumed.AnyAsync<ObservedMessage>(cancellationToken));
            Assert.True(await harness.Consumed.AnyAsync<OtherMessage>(cancellationToken));

            CancellationToken snapshotOnly = new(canceled: true);
            Assert.Equal(expected, Assert.Single(harness.Sent.Select<ObservedMessage>(snapshotOnly)).Context.Message);
            Assert.Equal(expected, Assert.Single(harness.Sent.Select<ObservedMessage>(
                message => message.Context.Message.Value == "expected", snapshotOnly)).Context.Message);
            Assert.Equal(expected, (await harness.Sent.SelectAsync(filter =>
            {
                filter.Includes.Add<ObservedMessage>();
                filter.Excludes.Add<OtherMessage>();
            }, cancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken)).MessageObject);
            Assert.Equal(expected, (await harness.Sent.SelectAsync<ObservedMessage>(cancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken)).Context.Message);
            Assert.Equal(expected, (await harness.Sent.SelectAsync<ObservedMessage>(
                message => message.Context.Message.CorrelationId == expected.CorrelationId, cancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken)).Context.Message);
            Assert.True(await harness.Sent.AnyAsync(filter =>
            {
                filter.Includes.Add<ObservedMessage>();
                filter.Excludes.Add<OtherMessage>();
            }, cancellationToken));
            Assert.True(await harness.Sent.AnyAsync<ObservedMessage>(cancellationToken));
            Assert.True(await harness.Sent.AnyAsync<ObservedMessage>(
                message => message.Context.Message.Value == "expected", cancellationToken));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-OBSERVATION-LIST", "published-query-surface")]
    public async Task PublishedList_QuerySurfaceAppliesTypePredicatesAndConfiguredFiltersExactlyAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);

        await harness.StartAsync(cancellationToken);
        try
        {
            var expected = new ObservedMessage(NewId.NextGuid(), "expected");
            var other = new OtherMessage(NewId.NextGuid(), "other");
            await harness.Bus.PublishAsync(expected, cancellationToken);
            await harness.Bus.PublishAsync(other, cancellationToken);

            CancellationToken snapshotOnly = new(canceled: true);
            Assert.Equal(expected, Assert.Single(harness.Published.Select<ObservedMessage>(snapshotOnly)).Context.Message);
            Assert.Equal(expected, Assert.Single(harness.Published.Select<ObservedMessage>(
                message => message.Context.Message.Value == "expected", snapshotOnly)).Context.Message);
            Assert.Equal(expected, (await harness.Published.SelectAsync(filter =>
            {
                filter.Includes.Add<ObservedMessage>();
                filter.Excludes.Add<OtherMessage>();
            }, cancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken)).MessageObject);
            Assert.Equal(expected, (await harness.Published.SelectAsync<ObservedMessage>(cancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken)).Context.Message);
            Assert.Equal(expected, (await harness.Published.SelectAsync<ObservedMessage>(
                message => message.Context.Message.CorrelationId == expected.CorrelationId, cancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken)).Context.Message);
            Assert.True(await harness.Published.AnyAsync(filter =>
            {
                filter.Includes.Add<ObservedMessage>();
                filter.Excludes.Add<OtherMessage>();
            }, cancellationToken));
            Assert.True(await harness.Published.AnyAsync<ObservedMessage>(cancellationToken));
            Assert.True(await harness.Published.AnyAsync<ObservedMessage>(
                message => message.Context.Message.Value == "expected", cancellationToken));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-OBSERVATION-LIST", "received-query-surface-and-typed-facade")]
    public async Task ReceivedLists_QuerySurfaceAndTypedFacadePreserveTheExactConsumeContextAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var consumed = new TaskCompletionSource<ConsumeContext<ObservedMessage>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = CreateHarness(timeout);
        harness.OnConfigureInMemoryReceiveEndpoint += configurator =>
        {
            configurator.Handler<ObservedMessage>(context =>
            {
                consumed.TrySetResult(context);
                return Task.CompletedTask;
            });
            configurator.Handler<OtherMessage>(_ => Task.CompletedTask);
        };

        await harness.StartAsync(cancellationToken);
        try
        {
            var expected = new ObservedMessage(NewId.NextGuid(), "expected");
            var other = new OtherMessage(NewId.NextGuid(), "other");
            await harness.InputQueueSendEndpoint.SendAsync(expected, cancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync(other, cancellationToken);
            ConsumeContext<ObservedMessage> expectedContext = await consumed.Task.WaitAsync(timeout, cancellationToken);
            Assert.True(await harness.Consumed.AnyAsync<OtherMessage>(cancellationToken));

            CancellationToken snapshotOnly = new(canceled: true);
            Assert.Same(expectedContext, Assert.Single(harness.Consumed.Select<ObservedMessage>(snapshotOnly)).Context);
            Assert.Same(expectedContext, Assert.Single(harness.Consumed.Select<ObservedMessage>(
                message => message.Context.Message.Value == "expected", snapshotOnly)).Context);
            Assert.Same(expectedContext, (await harness.Consumed.SelectAsync(filter =>
            {
                filter.Includes.Add<ObservedMessage>();
                filter.Excludes.Add<OtherMessage>();
            }, cancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken)).Context);
            Assert.Same(expectedContext, (await harness.Consumed.SelectAsync<ObservedMessage>(cancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken)).Context);
            Assert.Same(expectedContext, (await harness.Consumed.SelectAsync<ObservedMessage>(
                message => message.Context.Message.CorrelationId == expected.CorrelationId, cancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken)).Context);
            Assert.True(await harness.Consumed.AnyAsync(filter =>
            {
                filter.Includes.Add<ObservedMessage>();
                filter.Excludes.Add<OtherMessage>();
            }, cancellationToken));
            Assert.True(await harness.Consumed.AnyAsync<ObservedMessage>(cancellationToken));
            Assert.True(await harness.Consumed.AnyAsync<ObservedMessage>(
                message => message.Context.Message.Value == "expected", cancellationToken));

            var typed = new ReceivedMessageList<ObservedMessage>(timeout, snapshotOnly, new FakeTimeProvider(ObservationTime));
            typed.Add(expectedContext);

            Assert.Same(expectedContext, Assert.Single(typed.Select(snapshotOnly)).Context);
            Assert.Same(expectedContext, (await typed.SelectAsync(cancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken)).Context);
            Assert.True(await typed.AnyAsync(cancellationToken));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-OBSERVATION-LIST", "observer-success-fault-and-clock")]
    public async Task SendAndPublishObservers_RecordExactSuccessFaultAndConfiguredTimeAsync()
    {
        TimeSpan timeout = OperationTimeout();
        var success = new ObservedMessage(NewId.NextGuid(), "success");
        var failure = new ObservedMessage(NewId.NextGuid(), "failure");
        var expectedFailure = new InvalidOperationException("expected failure");
        var sendSuccess = CreateSendContext(success);
        var sendFault = CreateSendContext(failure);
        var publishSuccess = CreatePublishContext(success);
        var publishFault = CreatePublishContext(failure);
        var timeProvider = new FakeTimeProvider(sendSuccess.SentTime!.Value);
        using var sendObserver = new BusTestSendObserver(timeout, timeout, CancellationToken.None, timeProvider);
        using var publishObserver = new BusTestPublishObserver(timeout, timeout, CancellationToken.None, timeProvider);
        ISendObserver send = sendObserver;
        IPublishObserver publish = publishObserver;

        timeProvider.SetUtcNow(sendSuccess.SentTime!.Value + TimeSpan.FromSeconds(1));
        await send.PreSendAsync(sendSuccess);
        await send.PostSendAsync(sendSuccess);
        timeProvider.SetUtcNow(sendFault.SentTime!.Value + TimeSpan.FromSeconds(2));
        await send.SendFaultAsync(sendFault, expectedFailure);
        timeProvider.SetUtcNow(publishSuccess.SentTime!.Value + TimeSpan.FromSeconds(3));
        await publish.PrePublishAsync(publishSuccess);
        await publish.PostPublishAsync(publishSuccess);
        timeProvider.SetUtcNow(publishFault.SentTime!.Value + TimeSpan.FromSeconds(4));
        await publish.PublishFaultAsync(publishFault, expectedFailure);

        CancellationToken snapshotOnly = new(canceled: true);
        ISentMessage<ObservedMessage>[] sent = sendObserver.Messages.Select<ObservedMessage>(snapshotOnly).ToArray();
        IPublishedMessage<ObservedMessage>[] published = publishObserver.Messages.Select<ObservedMessage>(snapshotOnly).ToArray();

        Assert.Equal(2, sent.Length);
        Assert.Equal(2, published.Length);
        Assert.Same(sendSuccess, sent[0].Context);
        Assert.Null(sent[0].Exception);
        Assert.Equal(TimeSpan.FromSeconds(1), sent[0].ElapsedTime);
        Assert.Same(sendFault, sent[1].Context);
        Assert.Same(expectedFailure, sent[1].Exception);
        Assert.Equal(TimeSpan.FromSeconds(2), sent[1].ElapsedTime);
        Assert.Same(publishSuccess, published[0].Context);
        Assert.Null(published[0].Exception);
        Assert.Equal(TimeSpan.FromSeconds(3), published[0].ElapsedTime);
        Assert.Same(publishFault, published[1].Context);
        Assert.Same(expectedFailure, published[1].Exception);
        Assert.Equal(TimeSpan.FromSeconds(4), published[1].ElapsedTime);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASYNC-ELEMENT-LIST", "missing-and-duplicate-identifiers")]
    public void AsyncElementList_DropsMissingIdentifiersAndKeepsTheFirstDuplicate()
    {
        Guid duplicateId = NewId.NextGuid();
        var list = new TestElementList(new CancellationToken(canceled: true));

        list.Record(new TestElement(null, "missing"));
        list.Record(new TestElement(duplicateId, "first"));
        list.Record(new TestElement(duplicateId, "duplicate"));

        TestElement element = Assert.Single(list.Select(_ => true, new CancellationToken(canceled: true)));
        Assert.Equal(duplicateId, element.ElementId);
        Assert.Equal("first", element.Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASYNC-ELEMENT-LIST", "filter-failure-propagation-and-recovery")]
    public async Task AsyncElementList_PropagatesFilterFailureAndRemainsUsableAfterwardAsync()
    {
        Guid expectedId = NewId.NextGuid();
        var list = new TestElementList(CancellationToken.None);
        list.Record(new TestElement(expectedId, "expected"));

        var expected = new InvalidOperationException("filter failed");
        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            list.SelectAsync(_ => throw expected, TestContext.Current.CancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken));
        TestElement recovered = await list.SelectAsync(_ => true, TestContext.Current.CancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Same(expected, actual);
        Assert.Equal(expectedId, recovered.ElementId);
        Assert.Equal("expected", recovered.Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASYNC-ELEMENT-LIST", "empty-async-extension-contract")]
    public async Task EmptyAsyncSequence_ExtensionsReturnExactEmptyResultsAsync()
    {
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Empty<TestElement>().FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal("Message List was empty, or timed out", exception.Message);
        Assert.Null(await Empty<TestElement>().FirstObservedOrDefaultAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, await Empty<TestElement>().CountObservedAsync(TestContext.Current.CancellationToken));
        Assert.False(await Empty<TestElement>().AnyObservedAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASYNC-ELEMENT-LIST", "sent-message-deconstruction")]
    public void SentMessageDeconstruction_ReturnsExactTypedAndUntypedPayloadAndContext()
    {
        var message = new ObservedMessage(NewId.NextGuid(), "expected");
        var context = CreateSendContext(message);
        var recorded = new SentMessage<ObservedMessage>(context, null, new FakeTimeProvider(ObservationTime));
        ISentMessage untyped = recorded;
        ISentMessage<ObservedMessage> typed = recorded;

        (object untypedMessage, SendContext untypedContext) = untyped;
        (ObservedMessage typedMessage, SendContext typedContext) = typed;

        Assert.Same(message, untypedMessage);
        Assert.Same(context, untypedContext);
        Assert.Same(message, typedMessage);
        Assert.Same(context, typedContext);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-OBSERVATION-LIST", "dynamic-endpoint-publish-observation")]
    public async Task ReceiveEndpointObserver_RecordsPublicationsFromADynamicallyConnectedEndpointAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);

        await harness.StartAsync(cancellationToken);
        try
        {
            var endpointPublished = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var published = new BusTestPublishObserver(
                timeout,
                timeout,
                CancellationToken.None,
                harness.TimeProvider);
            using ConnectHandle observerHandle = harness.Bus.ConnectReceiveEndpointObserver(
                new TestReceiveEndpointObserver(published));
            HostReceiveEndpointHandle endpoint = harness.Bus.ConnectReceiveEndpoint(
                $"observed-endpoint-{NewId.NextGuid():N}",
                configurator => configurator.Handler<EndpointRequest>(async context =>
                {
                    await context.Advanced().PublishAsync(new EndpointEvent(context.Message.CorrelationId), context.CancellationToken);
                    endpointPublished.TrySetResult(true);
                }));
            ReceiveEndpointReady ready = await endpoint.Ready.WaitAsync(timeout, cancellationToken);

            try
            {
                var request = new EndpointRequest(NewId.NextGuid());
                ISendEndpoint sendEndpoint = await harness.GetSendEndpointAsync(ready.InputAddress, TestContext.Current.CancellationToken);

                await sendEndpoint.SendAsync(request, cancellationToken);
                await endpointPublished.Task.WaitAsync(timeout, cancellationToken);

                IPublishedMessage<EndpointEvent> observation = Assert.Single(
                    published.Messages.Select<EndpointEvent>(new CancellationToken(canceled: true)));
                Assert.Equal(request.CorrelationId, observation.Context.Message.CorrelationId);
                Assert.Equal(request.CorrelationId, observation.Context.CorrelationId);
                Assert.Null(observation.Exception);
            }
            finally
            {
                await endpoint.StopAsync(cancellationToken);
            }
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-OBSERVATION-LIST", "required-constructor-dependencies")]
    public void ProviderAwareListsAndEndpointObserver_RejectEveryMissingRequiredDependency()
    {
        TimeSpan timeout = OperationTimeout();

        Assert.Equal("timeProvider", Assert.Throws<ArgumentNullException>(() =>
            new SentMessageList(timeout, CancellationToken.None, null!)).ParamName);
        Assert.Equal("timeProvider", Assert.Throws<ArgumentNullException>(() =>
            new PublishedMessageList(timeout, CancellationToken.None, null!)).ParamName);
        Assert.Equal("timeProvider", Assert.Throws<ArgumentNullException>(() =>
            new ReceivedMessageList(timeout, CancellationToken.None, null!)).ParamName);
        Assert.Equal("timeProvider", Assert.Throws<ArgumentNullException>(() =>
            new ReceivedMessageList<ObservedMessage>(timeout, CancellationToken.None, null!)).ParamName);
        Assert.Equal("publishObserver", Assert.Throws<ArgumentNullException>(() =>
            new TestReceiveEndpointObserver(null!)).ParamName);
    }

    private static InMemoryTestHarness CreateHarness(TimeSpan timeout) =>
        new(new FakeTimeProvider(ObservationTime), $"message-lists-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };

    private static MessageSendContext<T> CreateSendContext<T>(T message)
        where T : class
        => new(message)
        {
            MessageId = NewId.NextGuid(),
        };

    private static TestPublishContext<T> CreatePublishContext<T>(T message)
        where T : class
        => new(message)
        {
            MessageId = NewId.NextGuid(),
        };

    private static async IAsyncEnumerable<T> Empty<T>()
        where T : class
    {
        await Task.CompletedTask;
        yield break;
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed record ObservedMessage(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    private sealed record OtherMessage(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    private sealed record EndpointRequest(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed record EndpointEvent(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed record TestElement(Guid? ElementId, string Value) : IAsyncListElement;

    private sealed class TestElementList(CancellationToken testCompleted) : AsyncElementList<TestElement>(
        TimeSpan.FromMinutes(1), testCompleted, new FakeTimeProvider(ObservationTime))
    {
        public void Record(TestElement element) => Add(element);
    }

    private sealed class TestPublishContext<T>(T message) : MessageSendContext<T>(message), PublishContext<T>
        where T : class;
}

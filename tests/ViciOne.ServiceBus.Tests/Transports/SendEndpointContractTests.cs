using System.Net.Mime;
using System.Reflection;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports;

public sealed class SendEndpointContractTests
{
    static readonly Uri SourceAddress = new("loopback://localhost/dispatch-input");
    static readonly Uri DestinationAddress = new("loopback://localhost/dispatch-output");
    static readonly Uri ExistingSourceAddress = new("loopback://localhost/existing-input");
    static readonly Guid ExistingConversation = Guid.Parse("a280dc85-c66a-4385-8e25-cb04f2234d51");
    static readonly ContentType SerializerContentType = new("application/json");

    public static TheoryData<int> SendForms => [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];

    public static IEnumerable<object[]> FormsAndOutcomes()
    {
        for (int form = 0; form < 10; form++)
            for (int outcome = 0; outcome < 3; outcome++)
                yield return [form, outcome];
    }

    public static IEnumerable<object[]> FormsAndExistingMetadata()
    {
        for (int form = 0; form < 10; form++)
        {
            yield return [form, false];
            yield return [form, true];
        }
    }

    public static IEnumerable<object[]> StagesAndOutcomes()
    {
        for (int form = 0; form < 10; form++)
            for (int stage = 0; stage < 3; stage++)
            {
                if (stage != 1 && !HasAdditionalPipe(form))
                    continue;
                for (int outcome = 0; outcome < 3; outcome++)
                    yield return [form, stage, outcome];
            }
    }

    [Theory]
    [MemberData(nameof(SendForms))]
    [RequirementCoverage("REQ-VSB-SEND-ENDPOINT-DISPATCH", "every-send-form-rejects-null-transport-task")]
    public async Task EverySendForm_RejectsNullTransportTaskAsync(int form)
    {
        await using var fixture = new Fixture();
        fixture.Transport.ReturnNullSendTask = true;

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            StartSendAsync(fixture, form, TestContext.Current.CancellationToken));

        Assert.Equal("The send transport returned no send task.", failure.Message);
        AssertCall(fixture, form, TestContext.Current.CancellationToken);
        Assert.Empty(fixture.Stages);
        Assert.Empty(fixture.StageTokens);
    }

    [Theory]
    [MemberData(nameof(SendForms))]
    [RequirementCoverage("REQ-VSB-SEND-ENDPOINT-DISPATCH", "every-send-form-pre-cancellation-has-no-transport-effect")]
    public async Task EverySendForm_PreCancellationNeverInvokesTransportAsync(int form)
    {
        await using var fixture = new Fixture();
        using var caller = new CancellationTokenSource();
        caller.Cancel();
        fixture.Transport.ReturnNullSendTask = true;

        OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            StartSendAsync(fixture, form, caller.Token));

        Assert.Equal(caller.Token, failure.CancellationToken);
        Assert.Empty(fixture.Calls);
        Assert.Empty(fixture.Stages);
        Assert.Empty(fixture.StageTokens);
    }

    [Theory]
    [MemberData(nameof(FormsAndOutcomes))]
    [RequirementCoverage("REQ-VSB-SEND-ENDPOINT-DISPATCH", "every-send-form-preserves-immediate-provider-outcome")]
    public async Task EverySendForm_PreservesImmediateTransportFailureOrCancellationAsync(int form, int outcome)
    {
        await using var fixture = new Fixture();
        using var providerCancellation = new CancellationTokenSource();
        providerCancellation.Cancel();
        var originalFailure = new InvalidOperationException("immediate transport failure");
        fixture.Transport.ImmediateFailure = outcome == 0 ? originalFailure : null;
        fixture.Transport.ImmediateOutcome = outcome switch
        {
            1 => Task.FromException(originalFailure),
            2 => Task.FromCanceled(providerCancellation.Token),
            _ => null,
        };

        if (outcome == 2)
        {
            OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                StartSendAsync(fixture, form, TestContext.Current.CancellationToken));
            Assert.Equal(providerCancellation.Token, failure.CancellationToken);
        }
        else
        {
            InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                StartSendAsync(fixture, form, TestContext.Current.CancellationToken));
            Assert.Same(originalFailure, failure);
        }
        AssertCall(fixture, form, TestContext.Current.CancellationToken);
        Assert.Empty(fixture.Stages);
    }

    [Theory]
    [MemberData(nameof(FormsAndOutcomes))]
    [RequirementCoverage("REQ-VSB-SEND-ENDPOINT-DISPATCH", "every-send-form-owns-held-transport-and-original-outcome")]
    public async Task EverySendForm_OwnsHeldTransportAndItsOriginalOutcomeAsync(int form, int outcome)
    {
        await using var fixture = new Fixture();
        using var caller = new CancellationTokenSource();
        using var providerCancellation = new CancellationTokenSource();
        providerCancellation.Cancel();
        var provider = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var originalFailure = new InvalidOperationException("held transport failure");
        fixture.Transport.DispatchTask = provider.Task;
        Task? send = null;
        try
        {
            send = StartSendAsync(fixture, form, caller.Token);
            await fixture.Transport.SendEntered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            Assert.False(send.IsCompleted);
            AssertCall(fixture, form, caller.Token);
            if (form < 7)
                Assert.Same(fixture.Transport.LastProvidedSendTask, send);
            caller.Cancel();
            Assert.False(send.IsCompleted);
            if (outcome == 0)
            {
                provider.SetResult();
                await send.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
                Assert.True(send.IsCompletedSuccessfully);
            }
            else if (outcome == 1)
            {
                provider.SetException(originalFailure);
                Assert.Same(originalFailure, await Assert.ThrowsAsync<InvalidOperationException>(() => send));
                Assert.True(send.IsFaulted);
            }
            else
            {
                provider.SetCanceled(providerCancellation.Token);
                OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => send);
                Assert.Equal(providerCancellation.Token, failure.CancellationToken);
                Assert.True(send.IsCanceled);
            }
        }
        finally
        {
            provider.TrySetResult();
            await ObserveCompletionAsync(provider.Task);
            if (send is not null)
                await ObserveCompletionAsync(send);
            if (fixture.Transport.LastProvidedSendTask is { } provided)
                await ObserveCompletionAsync(provided);
        }
    }

    [Theory]
    [MemberData(nameof(FormsAndExistingMetadata))]
    [RequirementCoverage("REQ-VSB-SEND-ENDPOINT-DISPATCH", "every-send-form-preserves-metadata-tokens-and-pipeline-order")]
    public async Task EverySendForm_AppliesExactMetadataTokensAndPipelineOrderAsync(int form, bool existingMetadata)
    {
        await using var fixture = new Fixture { ExistingMetadata = existingMetadata };

        await StartSendAsync(fixture, form, TestContext.Current.CancellationToken);

        AssertCall(fixture, form, TestContext.Current.CancellationToken);
        AssertMetadata(fixture, Assert.Single(fixture.Calls).Context, existingMetadata);
        Assert.Equal(ExpectedStages(form), fixture.Stages);
        Assert.All(fixture.StageTokens, token => Assert.Equal(TestContext.Current.CancellationToken, token));
        Assert.Equal(HasAdditionalPipe(form) ? 2 : 1, fixture.StageTokens.Count);
        Assert.True(fixture.Transport.SendEntered.Task.IsCompletedSuccessfully);
    }

    [Theory]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [RequirementCoverage("REQ-VSB-SEND-ENDPOINT-DISPATCH", "real-task-values-and-headers-initialize-before-transport")]
    public async Task PendingValues_AreReallyInitializedBeforeTransportAsync(int form)
    {
        await using var fixture = new Fixture();
        var value = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task? send = null;
        try
        {
            send = StartSendAsync(fixture, form, TestContext.Current.CancellationToken,
                new { Text = value.Task, __Header_Dispatch_Proof = "initialized-header" });
            Assert.False(send.IsCompleted);
            Assert.Empty(fixture.Calls);
            Assert.Empty(fixture.Stages);
            value.SetResult("initialized");
            await send.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            Call call = Assert.Single(fixture.Calls);
            Assert.Equal("initialized", call.Message.Text);
            Assert.Equal("initialized-header", call.Context.Headers.Get<string>("Dispatch-Proof"));
            Assert.Equal(ExpectedStages(form), fixture.Stages);
            Assert.False(call.CreatingContext);
        }
        finally
        {
            value.TrySetResult("initialized");
            if (send is not null)
                await ObserveCompletionAsync(send);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SEND-ENDPOINT-DISPATCH", "all-required-arguments-win-before-cancellation")]
    public async Task RequiredArguments_WinBeforeCancellationAcrossEveryFormAsync(bool cancel)
    {
        await using var fixture = new Fixture();
        using var caller = new CancellationTokenSource();
        if (cancel)
            caller.Cancel();
        for (int form = 0; form < 10; form++)
        {
            ArgumentNullException synchronousFailure = Assert.Throws<ArgumentNullException>(() =>
            {
                _ = StartSendAsync(fixture, form, caller.Token, missingMessage: true);
            });
            Assert.Equal(form < 7 ? "message" : "values", synchronousFailure.ParamName);
            ArgumentNullException failure = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                StartSendAsync(fixture, form, caller.Token, missingMessage: true));
            Assert.Equal(form < 7 ? "message" : "values", failure.ParamName);
        }
        Func<Task>[] missingPipes =
        [
            () => fixture.Endpoint.SendAsync(fixture.Message, (IPipe<SendContext<DispatchMessage>>)null!, caller.Token),
            () => fixture.Endpoint.SendAsync(fixture.Message, (IPipe<SendContext>)null!, caller.Token),
            () => fixture.Endpoint.SendAsync((object)fixture.Message, (IPipe<SendContext>)null!, caller.Token),
            () => fixture.Endpoint.SendAsync((object)fixture.Message, typeof(DispatchMessage), null!, caller.Token),
            () => fixture.Endpoint.SendAsync<DispatchMessage>(new { Text = "values" }, (IPipe<SendContext<DispatchMessage>>)null!, caller.Token),
            () => fixture.Endpoint.SendAsync<DispatchMessage>(new { Text = "values" }, (IPipe<SendContext>)null!, caller.Token),
            () => fixture.Endpoint.CreateSendContextAsync(fixture.Message, null!, caller.Token),
        ];
        foreach (Func<Task> call in missingPipes)
        {
            Assert.Equal("pipe", Assert.Throws<ArgumentNullException>(() => { _ = call(); }).ParamName);
            Assert.Equal("pipe", (await Assert.ThrowsAsync<ArgumentNullException>(call)).ParamName);
        }
        Assert.Equal("messageType", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = fixture.Endpoint.SendAsync((object)fixture.Message, (Type)null!, caller.Token);
        }).ParamName);
        Assert.Equal("messageType", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = fixture.Endpoint.SendAsync((object)fixture.Message, null!, fixture.UntypedPipe, caller.Token);
        }).ParamName);
        Assert.Equal("message", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = fixture.Endpoint.CreateSendContextAsync<DispatchMessage>(null!, fixture.TypedPipe, caller.Token);
        }).ParamName);
        Assert.Equal("messageType", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            fixture.Endpoint.SendAsync((object)fixture.Message, (Type)null!, caller.Token))).ParamName);
        Assert.Equal("messageType", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            fixture.Endpoint.SendAsync((object)fixture.Message, null!, fixture.UntypedPipe, caller.Token))).ParamName);
        Assert.Equal("message", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            fixture.Endpoint.CreateSendContextAsync<DispatchMessage>(null!, fixture.TypedPipe, caller.Token))).ParamName);
        Assert.Empty(fixture.Calls);
        Assert.Empty(fixture.Stages);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-SEND-ENDPOINT-DISPATCH", "context-creation-rejects-null-task-and-fast-or-held-null-result")]
    public async Task ContextCreation_RejectsNullTaskAndImmediateOrHeldNullResultAsync(int phase)
    {
        await using var fixture = new Fixture();
        var provider = new TaskCompletionSource<SendContext<DispatchMessage>>(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Transport.CreateTask = phase switch
        {
            0 => null,
            1 => Task.FromResult<SendContext<DispatchMessage>>(null!),
            _ => provider.Task,
        };
        Task<SendContext<DispatchMessage>>? creation = null;
        try
        {
            if (phase == 2)
            {
                creation = fixture.Endpoint.CreateSendContextAsync(fixture.Message, fixture.TypedPipe, TestContext.Current.CancellationToken);
                Assert.False(creation.IsCompleted);
                provider.SetResult(null!);
            }
            InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() => creation
                ?? fixture.Endpoint.CreateSendContextAsync(fixture.Message, fixture.TypedPipe, TestContext.Current.CancellationToken));
            Assert.Equal(phase == 0
                ? "The send transport returned no context-creation task."
                : "The send transport created no send context.", failure.Message);
            Assert.True(Assert.Single(fixture.Calls).CreatingContext);
            Assert.Empty(fixture.Stages);
            Assert.False(fixture.Transport.SendEntered.Task.IsCompleted);
        }
        finally
        {
            provider.TrySetResult(null!);
            if (creation is not null)
                await ObserveCompletionAsync(creation);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-SEND-ENDPOINT-DISPATCH", "context-creation-owns-held-provider-and-original-outcome")]
    public async Task ContextCreation_OwnsHeldProviderAndItsOriginalOutcomeAsync(int outcome)
    {
        await using var fixture = new Fixture();
        using var caller = new CancellationTokenSource();
        using var providerCancellation = new CancellationTokenSource();
        providerCancellation.Cancel();
        var provider = new TaskCompletionSource<SendContext<DispatchMessage>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var originalFailure = new InvalidOperationException("context creation failed");
        fixture.Transport.CreateTask = provider.Task;
        Task<SendContext<DispatchMessage>> creation = fixture.Endpoint.CreateSendContextAsync(fixture.Message, fixture.TypedPipe, caller.Token);
        try
        {
            Assert.False(creation.IsCompleted);
            caller.Cancel();
            Assert.False(creation.IsCompleted);
            Assert.Equal(caller.Token, Assert.Single(fixture.Calls).CancellationToken);
            if (outcome == 0)
            {
                provider.SetResult(fixture.CreatedContext);
                Assert.Same(fixture.CreatedContext, await creation.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None));
                Assert.True(creation.IsCompletedSuccessfully);
            }
            else if (outcome == 1)
            {
                provider.SetException(originalFailure);
                Assert.Same(originalFailure, await Assert.ThrowsAsync<InvalidOperationException>(() => creation));
                Assert.True(creation.IsFaulted);
            }
            else
            {
                provider.SetCanceled(providerCancellation.Token);
                OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => creation);
                Assert.Equal(providerCancellation.Token, failure.CancellationToken);
                Assert.True(creation.IsCanceled);
            }
            Assert.Empty(fixture.Stages);
        }
        finally
        {
            provider.TrySetResult(fixture.CreatedContext);
            await ObserveCompletionAsync(provider.Task);
            await ObserveCompletionAsync(creation);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-ENDPOINT-DISPATCH", "context-creation-fast-path-preserves-exact-provider-task-and-context")]
    public async Task ContextCreation_FastPathPreservesExactProviderTaskAndContextAsync()
    {
        await using var fixture = new Fixture();

        Task<SendContext<DispatchMessage>> creation = fixture.Endpoint.CreateSendContextAsync(
            fixture.Message, fixture.TypedPipe, TestContext.Current.CancellationToken);

        Assert.Same(fixture.Transport.CreateTask, creation);
        Assert.Same(fixture.CreatedContext, await creation);
        Call call = Assert.Single(fixture.Calls);
        Assert.True(call.CreatingContext);
        Assert.Same(fixture.Message, call.Message);
        Assert.Equal(TestContext.Current.CancellationToken, call.CancellationToken);
        Assert.Empty(fixture.Stages);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-ENDPOINT-DISPATCH", "context-creation-captured-pipeline-configures-without-dispatch")]
    public async Task ContextCreation_CapturedPipelineConfiguresWithoutDispatchAsync()
    {
        await using var fixture = new Fixture();
        SendContext<DispatchMessage> context = await fixture.Endpoint.CreateSendContextAsync(
            fixture.Message, fixture.TypedPipe, TestContext.Current.CancellationToken);

        await Assert.IsAssignableFrom<IPipe<SendContext<DispatchMessage>>>(Assert.Single(fixture.Calls).Pipe).SendAsync(context);

        AssertMetadata(fixture, context, false);
        Assert.Equal(new[] { "general", "endpoint", "additional" }, fixture.Stages);
        Assert.All(fixture.StageTokens, token => Assert.Equal(context.CancellationToken, token));
        Assert.Equal(2, fixture.StageTokens.Count);
        Assert.True(Assert.Single(fixture.Calls).CreatingContext);
        Assert.False(fixture.Transport.SendEntered.Task.IsCompleted);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-SEND-ENDPOINT-DISPATCH", "every-pipeline-stage-rejects-null-configuration-task")]
    public async Task Pipeline_RejectsNullTaskWithExactStageDiagnosticAsync(int stage)
    {
        await using var fixture = new Fixture();
        fixture.StageTasks[stage] = null;

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            StartSendAsync(fixture, 1, TestContext.Current.CancellationToken));

        Assert.Equal(StageDiagnostic(stage), failure.Message);
        Assert.Equal(new[] { "general", "endpoint", "additional" }.Take(stage + 1), fixture.Stages);
        Assert.Null(Assert.Single(fixture.Calls).Context.ConversationId);
        Assert.False(fixture.Transport.SendEntered.Task.IsCompleted);
    }

    [Theory]
    [MemberData(nameof(StagesAndOutcomes))]
    [RequirementCoverage("REQ-VSB-SEND-ENDPOINT-DISPATCH", "every-pipeline-stage-owns-held-task-and-original-outcome")]
    public async Task Pipeline_OwnsEachHeldStageAndItsOriginalOutcomeAsync(int form, int stage, int outcome)
    {
        await using var fixture = new Fixture();
        using var caller = new CancellationTokenSource();
        using var providerCancellation = new CancellationTokenSource();
        providerCancellation.Cancel();
        var provider = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var originalFailure = new InvalidOperationException("held configuration failed");
        fixture.StageTasks[stage] = provider.Task;
        Task send = StartSendAsync(fixture, form, caller.Token);
        try
        {
            await fixture.StageEntered[stage].Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            Assert.False(send.IsCompleted);
            Assert.False(fixture.Transport.SendEntered.Task.IsCompleted);
            Assert.Equal(StagesThrough(form, stage), fixture.Stages);
            Assert.Null(Assert.Single(fixture.Calls).Context.ConversationId);
            caller.Cancel();
            Assert.False(send.IsCompleted);
            if (outcome == 0)
            {
                provider.SetResult();
                await send.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
                Assert.True(send.IsCompletedSuccessfully);
                Assert.Equal(ExpectedStages(form), fixture.Stages);
                Assert.NotNull(Assert.Single(fixture.Calls).Context.ConversationId);
                Assert.True(fixture.Transport.SendEntered.Task.IsCompletedSuccessfully);
            }
            else if (outcome == 1)
            {
                provider.SetException(originalFailure);
                Assert.Same(originalFailure, await Assert.ThrowsAsync<InvalidOperationException>(() => send));
                Assert.True(send.IsFaulted);
                Assert.Null(Assert.Single(fixture.Calls).Context.ConversationId);
                Assert.Equal(StagesThrough(form, stage), fixture.Stages);
                Assert.False(fixture.Transport.SendEntered.Task.IsCompleted);
            }
            else
            {
                provider.SetCanceled(providerCancellation.Token);
                OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => send);
                Assert.Equal(providerCancellation.Token, failure.CancellationToken);
                Assert.True(send.IsCanceled);
                Assert.Null(Assert.Single(fixture.Calls).Context.ConversationId);
                Assert.Equal(StagesThrough(form, stage), fixture.Stages);
                Assert.False(fixture.Transport.SendEntered.Task.IsCompleted);
            }
        }
        finally
        {
            provider.TrySetResult();
            await ObserveCompletionAsync(provider.Task);
            await ObserveCompletionAsync(send);
            if (fixture.Transport.LastProvidedSendTask is { } provided)
                await ObserveCompletionAsync(provided);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [RequirementCoverage("REQ-VSB-SEND-ENDPOINT-DISPATCH", "constructor-validates-every-required-dependency")]
    public async Task Constructor_ValidatesEveryDependencyWithoutDispatchAsync(int missing)
    {
        await using var fixture = new Fixture();
        ReceiveEndpointContext context = missing is >= 4 ? fixture.CreateReceiveContext(missing) : fixture.Receive;

        Exception failure = Assert.ThrowsAny<Exception>(() => new SendEndpoint(
            missing == 0 ? null! : fixture.Transport,
            missing == 1 ? null! : context,
            missing == 2 ? null! : DestinationAddress,
            missing == 3 ? null! : fixture.EndpointPipe));

        if (missing < 4)
        {
            ArgumentNullException argument = Assert.IsType<ArgumentNullException>(failure);
            Assert.Equal(new[] { "transport", "context", "destinationAddress", "sendPipe" }[missing], argument.ParamName);
        }
        else
        {
            Assert.IsType<InvalidOperationException>(failure);
            Assert.Equal(new[]
            {
                "The receive endpoint context returned no source address.",
                "The receive endpoint context returned no serialization registry.",
                "The serialization registry returned no message serializer.",
            }[missing - 4], failure.Message);
        }
        Assert.Empty(fixture.Calls);
        Assert.Empty(fixture.Stages);
        Assert.Equal(0, fixture.Transport.DisposeCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-SEND-ENDPOINT-DISPATCH", "observer-connection-preserves-handle-and-original-failure-or-null-diagnostic")]
    public async Task ObserverConnection_PreservesHandleAndOriginalFailureOrNullDiagnosticAsync(int outcome)
    {
        await using var fixture = new Fixture();
        ISendObserver observer = StrictProxy.Create<ISendObserver>((method, _) => throw new InvalidOperationException(method.Name));
        var originalFailure = new InvalidOperationException("observer connection failed");
        fixture.Transport.ReturnNullConnection = outcome == 1;
        fixture.Transport.ConnectionFailure = outcome == 2 ? originalFailure : null;

        Assert.Equal("observer", Assert.Throws<ArgumentNullException>(() => fixture.Endpoint.ConnectSendObserver(null!)).ParamName);
        Assert.Equal(0, fixture.Transport.ConnectCount);
        if (outcome == 0)
            Assert.Same(fixture.Transport.Connection, fixture.Endpoint.ConnectSendObserver(observer));
        else
        {
            InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() => fixture.Endpoint.ConnectSendObserver(observer));
            if (outcome == 1)
                Assert.Equal("The send transport returned no observer connection handle.", failure.Message);
            else
                Assert.Same(originalFailure, failure);
        }
        Assert.Equal(1, fixture.Transport.ConnectCount);
        Assert.Same(observer, fixture.Transport.Observer);
        Assert.Equal(0, fixture.Transport.Connection.DisconnectCount);
        Assert.Empty(fixture.Calls);
        Assert.Empty(fixture.Stages);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-ENDPOINT-DISPATCH", "captured-pipeline-probe-validates-and-forwards-without-dispatch")]
    public async Task PipelineProbe_ValidatesAndForwardsTheExactProbeContextAsync()
    {
        await using var fixture = new Fixture();
        await fixture.Endpoint.CreateSendContextAsync(fixture.Message, fixture.TypedPipe, TestContext.Current.CancellationToken);
        IPipe<SendContext<DispatchMessage>> pipe = Assert.IsAssignableFrom<IPipe<SendContext<DispatchMessage>>>(Assert.Single(fixture.Calls).Pipe);
        ProbeContext probe = StrictProxy.Create<ProbeContext>((method, _) => throw new InvalidOperationException(method.Name));

        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => pipe.Probe(null!)).ParamName);
        pipe.Probe(probe);

        Assert.Same(probe, fixture.TypedPipe.LastProbe);
        Assert.Empty(fixture.Stages);
        Assert.False(fixture.Transport.SendEntered.Task.IsCompleted);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-ENDPOINT-DISPATCH", "valid-context-creation-pre-cancellation-has-no-transport-effect")]
    public async Task ContextCreation_PreCancellationNeverInvokesTransportAsync()
    {
        await using var fixture = new Fixture();
        using var caller = new CancellationTokenSource();
        caller.Cancel();
        fixture.Transport.CreateTask = null;

        OperationCanceledException failure = Assert.ThrowsAny<OperationCanceledException>(() =>
        {
            _ = fixture.Endpoint.CreateSendContextAsync(fixture.Message, fixture.TypedPipe, caller.Token);
        });

        Assert.Equal(caller.Token, failure.CancellationToken);
        Assert.Empty(fixture.Calls);
        Assert.Empty(fixture.Stages);
        Assert.Empty(fixture.StageTokens);
        Assert.False(fixture.Transport.SendEntered.Task.IsCompleted);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SEND-ENDPOINT-DISPATCH", "explicit-runtime-contract-differs-from-materialized-message-type")]
    public async Task ExplicitRuntimeContract_PreservesDeclaredTypeAndMessageIdentityAsync(bool additionalPipe)
    {
        await using var fixture = new Fixture();
        fixture.Transport.ExpectedMessageType = typeof(IDispatchContract);

        if (additionalPipe)
            await fixture.Endpoint.SendAsync((object)fixture.Message, typeof(IDispatchContract), fixture.UntypedPipe,
                TestContext.Current.CancellationToken);
        else
            await fixture.Endpoint.SendAsync((object)fixture.Message, typeof(IDispatchContract), TestContext.Current.CancellationToken);

        Call call = Assert.Single(fixture.Calls);
        Assert.Equal(typeof(IDispatchContract), call.MessageType);
        Assert.Same(fixture.Message, call.Message);
        Assert.IsAssignableFrom<SendContext<IDispatchContract>>(call.Context);
        Assert.Equal(TestContext.Current.CancellationToken, call.CancellationToken);
        Assert.False(call.CreatingContext);
        AssertMetadata(fixture, call.Context, false);
        Assert.Equal(additionalPipe ? new[] { "general", "endpoint", "additional" } : ["endpoint"], fixture.Stages);
        Assert.All(fixture.StageTokens, token => Assert.Equal(TestContext.Current.CancellationToken, token));
        Assert.Equal(additionalPipe ? 2 : 1, fixture.StageTokens.Count);
        Assert.True(fixture.Transport.SendEntered.Task.IsCompletedSuccessfully);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-SEND-ENDPOINT-DISPATCH", "context-creation-preserves-immediate-provider-failure-or-cancellation")]
    public async Task ContextCreation_PreservesImmediateProviderFailureOrCancellationAsync(int outcome)
    {
        await using var fixture = new Fixture();
        using var providerCancellation = new CancellationTokenSource();
        providerCancellation.Cancel();
        var originalFailure = new InvalidOperationException("immediate context creation failed");
        fixture.Transport.ImmediateCreationFailure = outcome == 0 ? originalFailure : null;
        fixture.Transport.CreateTask = outcome switch
        {
            0 => Task.FromResult(fixture.CreatedContext),
            1 => Task.FromException<SendContext<DispatchMessage>>(originalFailure),
            _ => Task.FromCanceled<SendContext<DispatchMessage>>(providerCancellation.Token),
        };

        if (outcome == 0)
        {
            Assert.Same(originalFailure, Assert.Throws<InvalidOperationException>(() =>
            {
                _ = fixture.Endpoint.CreateSendContextAsync(fixture.Message, fixture.TypedPipe, TestContext.Current.CancellationToken);
            }));
        }
        else
        {
            Task<SendContext<DispatchMessage>> creation = fixture.Endpoint.CreateSendContextAsync(
                fixture.Message, fixture.TypedPipe, TestContext.Current.CancellationToken);
            Assert.Same(fixture.Transport.CreateTask, creation);
            if (outcome == 1)
                Assert.Same(originalFailure, await Assert.ThrowsAsync<InvalidOperationException>(() => creation));
            else
                Assert.Equal(providerCancellation.Token,
                    (await Assert.ThrowsAnyAsync<OperationCanceledException>(() => creation)).CancellationToken);
        }
        Call call = Assert.Single(fixture.Calls);
        Assert.True(call.CreatingContext);
        Assert.Same(fixture.Message, call.Message);
        Assert.Equal(TestContext.Current.CancellationToken, call.CancellationToken);
        Assert.Empty(fixture.Stages);
        Assert.False(fixture.Transport.SendEntered.Task.IsCompleted);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-ENDPOINT-DISPATCH", "captured-pipeline-rejects-null-context-without-configuration")]
    public async Task Pipeline_RejectsNullContextBeforeConfigurationAsync()
    {
        await using var fixture = new Fixture();
        await fixture.Endpoint.CreateSendContextAsync(fixture.Message, fixture.TypedPipe, TestContext.Current.CancellationToken);
        IPipe<SendContext<DispatchMessage>> pipe = Assert.IsAssignableFrom<IPipe<SendContext<DispatchMessage>>>(Assert.Single(fixture.Calls).Pipe);

        ArgumentNullException failure = await Assert.ThrowsAsync<ArgumentNullException>(() => pipe.SendAsync(null!));

        Assert.Equal("context", failure.ParamName);
        Assert.Empty(fixture.Stages);
        Assert.Empty(fixture.StageTokens);
        Assert.False(fixture.Transport.SendEntered.Task.IsCompleted);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SEND-ENDPOINT-DISPATCH", "plain-additional-pipe-runs-after-endpoint-without-general-stage")]
    public async Task PlainAdditionalPipe_AppliesAfterEndpointWithoutGeneralConfigurationAsync(bool untyped)
    {
        await using var fixture = new Fixture();
        if (untyped)
            await fixture.Endpoint.SendAsync(fixture.Message, new PlainUntypedConfigurationPipe(fixture), TestContext.Current.CancellationToken);
        else
            await fixture.Endpoint.SendAsync(fixture.Message, new PlainTypedConfigurationPipe(fixture), TestContext.Current.CancellationToken);

        Call call = Assert.Single(fixture.Calls);
        Assert.Same(fixture.Message, call.Message);
        Assert.Equal(typeof(DispatchMessage), call.MessageType);
        Assert.False(call.CreatingContext);
        Assert.Equal(TestContext.Current.CancellationToken, call.CancellationToken);
        AssertMetadata(fixture, call.Context, false);
        Assert.Equal(new[] { "endpoint", "additional" }, fixture.Stages);
        Assert.Equal(TestContext.Current.CancellationToken, Assert.Single(fixture.StageTokens));
        Assert.True(fixture.Transport.SendEntered.Task.IsCompletedSuccessfully);
    }

    [Theory]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [RequirementCoverage("REQ-VSB-SEND-ENDPOINT-DISPATCH", "values-cancellation-retains-caller-owned-pending-property-without-dispatch")]
    public async Task PendingValues_CancellationLeavesCallerOwnedValueUnsettledWithoutDispatchAsync(int form)
    {
        await using var fixture = new Fixture();
        using var caller = new CancellationTokenSource();
        var value = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task? send = null;
        try
        {
            send = StartSendAsync(fixture, form, caller.Token,
                new { Text = value.Task, __Header_Dispatch_Proof = "initialized-header" });
            Assert.False(send.IsCompleted);
            Assert.Empty(fixture.Calls);
            caller.Cancel();

            OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => send);

            Assert.Equal(caller.Token, failure.CancellationToken);
            Assert.True(send.IsCanceled);
            Assert.False(value.Task.IsCompleted);
            Assert.Empty(fixture.Calls);
            Assert.Empty(fixture.Stages);
            Assert.Empty(fixture.StageTokens);
            Assert.False(fixture.Transport.SendEntered.Task.IsCompleted);
        }
        finally
        {
            value.TrySetResult("initialized");
            await ObserveCompletionAsync(value.Task);
            if (send is not null)
                await ObserveCompletionAsync(send);
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-SEND-ENDPOINT-DISPATCH", "initialized-headers-preserve-null-inner-diagnostic-and-owned-original-outcome")]
    public async Task InitializedHeaders_RejectNullInnerTaskAndOwnItsOriginalOutcomeAsync(int outcome)
    {
        using var caller = new CancellationTokenSource();
        using var providerCancellation = new CancellationTokenSource();
        providerCancellation.Cancel();
        var provider = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var originalFailure = new InvalidOperationException("initialized inner configuration failed");
        var inner = new InitializedConfigurationPipe { ConfigurationTask = outcome < 0 ? null : provider.Task };
        (DispatchMessage message, IPipe<SendContext<DispatchMessage>> pipe) = await MessageInitializerCache<DispatchMessage>
            .InitializeMessageAsync(new { Text = "initialized", __Header_Dispatch_Proof = "initialized-header" }, inner, caller.Token);
        var context = new MessageSendContext<DispatchMessage>(message, caller.Token);
        Task configuration = pipe.SendAsync(context);
        try
        {
            await inner.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            Assert.Same(context, inner.Context);
            Assert.Equal(1, inner.CallCount);
            Assert.Equal("initialized", context.Message.Text);
            Assert.Equal("initialized-header", context.Headers.Get<string>("Dispatch-Proof"));
            if (outcome < 0)
            {
                InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() => configuration);
                Assert.Equal("The initialized send pipe returned no configuration task.", failure.Message);
            }
            else
            {
                Assert.False(configuration.IsCompleted);
                caller.Cancel();
                Assert.False(configuration.IsCompleted);
                if (outcome == 0)
                {
                    provider.SetResult();
                    await configuration.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
                    Assert.True(configuration.IsCompletedSuccessfully);
                }
                else if (outcome == 1)
                {
                    provider.SetException(originalFailure);
                    Assert.Same(originalFailure, await Assert.ThrowsAsync<InvalidOperationException>(() => configuration));
                    Assert.True(configuration.IsFaulted);
                }
                else
                {
                    provider.SetCanceled(providerCancellation.Token);
                    Assert.Equal(providerCancellation.Token,
                        (await Assert.ThrowsAnyAsync<OperationCanceledException>(() => configuration)).CancellationToken);
                    Assert.True(configuration.IsCanceled);
                }
            }
            Assert.Equal(1, inner.CallCount);
        }
        finally
        {
            provider.TrySetResult();
            await ObserveCompletionAsync(provider.Task);
            await ObserveCompletionAsync(configuration);
        }
    }

    static Task StartSendAsync(Fixture fixture, int form, CancellationToken cancellationToken, object? values = null,
        bool missingMessage = false)
    {
        DispatchMessage message = missingMessage ? null! : fixture.Message;
        object input = missingMessage ? null! : values ?? new { Text = "initialized", __Header_Dispatch_Proof = "initialized-header" };
        return form switch
        {
            0 => fixture.Endpoint.SendAsync(message, cancellationToken),
            1 => fixture.Endpoint.SendAsync(message, fixture.TypedPipe, cancellationToken),
            2 => fixture.Endpoint.SendAsync(message, fixture.UntypedPipe, cancellationToken),
            3 => fixture.Endpoint.SendAsync((object)message, cancellationToken),
            4 => fixture.Endpoint.SendAsync((object)message, typeof(DispatchMessage), cancellationToken),
            5 => fixture.Endpoint.SendAsync((object)message, fixture.UntypedPipe, cancellationToken),
            6 => fixture.Endpoint.SendAsync((object)message, typeof(DispatchMessage), fixture.UntypedPipe, cancellationToken),
            7 => fixture.Endpoint.SendAsync<DispatchMessage>(input, cancellationToken),
            8 => fixture.Endpoint.SendAsync<DispatchMessage>(input, fixture.TypedPipe, cancellationToken),
            9 => fixture.Endpoint.SendAsync<DispatchMessage>(input, fixture.UntypedPipe, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(form)),
        };
    }

    static bool HasAdditionalPipe(int form) => form is 1 or 2 or 5 or 6 or 8 or 9;

    static string[] ExpectedStages(int form) => HasAdditionalPipe(form)
        ? ["general", "endpoint", "additional"]
        : ["endpoint"];

    static IEnumerable<string> StagesThrough(int form, int stage)
        => ExpectedStages(form).Take(HasAdditionalPipe(form) ? stage + 1 : 1);

    static string StageDiagnostic(int stage) => stage switch
    {
        0 => "The general send-context pipe returned no configuration task.",
        1 => "The endpoint send pipe returned no configuration task.",
        2 => "The additional send pipe returned no configuration task.",
        _ => throw new ArgumentOutOfRangeException(nameof(stage)),
    };

    static void AssertCall(Fixture fixture, int form, CancellationToken cancellationToken)
    {
        Call call = Assert.Single(fixture.Calls);
        Assert.False(call.CreatingContext);
        Assert.Equal(typeof(DispatchMessage), call.MessageType);
        Assert.Equal(cancellationToken, call.CancellationToken);
        Assert.Equal(form < 7 ? "materialized" : "initialized", call.Message.Text);
        if (form < 7)
            Assert.Same(fixture.Message, call.Message);
        else
            Assert.NotSame(fixture.Message, call.Message);
    }

    static void AssertMetadata(Fixture fixture, SendContext context, bool existingMetadata)
    {
        Assert.Same(fixture.Serializer, context.Serializer);
        Assert.Same(SerializerContentType, context.ContentType);
        Assert.Same(fixture.Serialization, context.Serialization);
        Assert.Equal(DestinationAddress, context.DestinationAddress);
        Assert.Equal(existingMetadata ? ExistingSourceAddress : SourceAddress, context.SourceAddress);
        Assert.NotNull(context.ConversationId);
        Assert.NotEqual(Guid.Empty, context.ConversationId);
        if (existingMetadata)
            Assert.Equal(ExistingConversation, context.ConversationId);
    }

    static async Task ObserveCompletionAsync(Task task)
    {
        try
        {
            await task.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
        }
        catch (Exception) when (task.IsCompleted)
        {
            // Settled failures are observed; incomplete cleanup operations still time out.
        }
    }

    sealed class Fixture : IAsyncDisposable
    {
        public Fixture()
        {
            Serializer = StrictProxy.Create<IMessageSerializer>((method, _) => method.Name switch
            {
                "get_ContentType" => SerializerContentType,
                _ => throw new InvalidOperationException($"Unexpected serializer operation: {method.Name}."),
            });
            Serialization = StrictProxy.Create<ISerialization>((method, _) => method.Name switch
            {
                nameof(ISerialization.GetMessageSerializer) => Serializer,
                _ => throw new InvalidOperationException($"Unexpected serialization operation: {method.Name}."),
            });
            CreatedContext = new MessageSendContext<DispatchMessage>(Message, TestContext.Current.CancellationToken);
            Transport = new ScriptedTransport(this);
            TypedPipe = new TypedConfigurationPipe(this);
            UntypedPipe = new UntypedConfigurationPipe(this);
            EndpointPipe = new EndpointConfigurationPipe(this);
            Receive = CreateReceiveContext();
            Endpoint = new SendEndpoint(Transport, Receive, DestinationAddress, EndpointPipe);
        }

        public DispatchMessage Message { get; } = new() { Text = "materialized" };
        public IMessageSerializer Serializer { get; }
        public ISerialization Serialization { get; }
        public SendContext<DispatchMessage> CreatedContext { get; }
        public ScriptedTransport Transport { get; }
        public TypedConfigurationPipe TypedPipe { get; }
        public UntypedConfigurationPipe UntypedPipe { get; }
        public EndpointConfigurationPipe EndpointPipe { get; }
        public ReceiveEndpointContext Receive { get; }
        public SendEndpoint Endpoint { get; }
        public bool ExistingMetadata { get; set; }
        public List<Call> Calls { get; } = [];
        public List<string> Stages { get; } = [];
        public List<CancellationToken> StageTokens { get; } = [];
        public Task?[] StageTasks { get; } = [Task.CompletedTask, Task.CompletedTask, Task.CompletedTask];
        public TaskCompletionSource[] StageEntered { get; } =
        [
            new(TaskCreationOptions.RunContinuationsAsynchronously),
            new(TaskCreationOptions.RunContinuationsAsynchronously),
            new(TaskCreationOptions.RunContinuationsAsynchronously),
        ];

        public ReceiveEndpointContext CreateReceiveContext(int missing = -1)
        {
            ISerialization serialization = missing == 6
                ? StrictProxy.Create<ISerialization>((method, _) => method.Name == nameof(ISerialization.GetMessageSerializer)
                    ? null : throw new InvalidOperationException(method.Name))
                : Serialization;
            return StrictProxy.Create<ReceiveEndpointContext>((method, _) => method.Name switch
            {
                "get_InputAddress" => missing == 4 ? null : SourceAddress,
                "get_Serialization" => missing == 5 ? null : serialization,
                _ => throw new InvalidOperationException($"Unexpected endpoint context operation: {method.Name}."),
            });
        }

        public Task ApplyStageAsync(int stage, SendContext context, CancellationToken? cancellationToken = null)
        {
            Assert.Same(Serializer, context.Serializer);
            Assert.Same(SerializerContentType, context.ContentType);
            Assert.Same(Serialization, context.Serialization);
            Assert.Equal(DestinationAddress, context.DestinationAddress);
            Assert.Equal(ExistingMetadata ? ExistingSourceAddress : SourceAddress, context.SourceAddress);
            if (cancellationToken is { } token)
                StageTokens.Add(token);
            Stages.Add(new[] { "general", "endpoint", "additional" }[stage]);
            StageEntered[stage].TrySetResult();
            return StageTasks[stage]!;
        }

        public ValueTask DisposeAsync() => Endpoint.DisposeAsync();
    }

    sealed class ScriptedTransport(Fixture fixture) : ISendTransport, IAsyncDisposable
    {
        public Task DispatchTask { get; set; } = Task.CompletedTask;
        public Task? ImmediateOutcome { get; set; }
        public Exception? ImmediateFailure { get; set; }
        public Exception? ImmediateCreationFailure { get; set; }
        public Type ExpectedMessageType { get; set; } = typeof(DispatchMessage);
        public Task<SendContext<DispatchMessage>>? CreateTask { get; set; } = Task.FromResult(fixture.CreatedContext);
        public bool ReturnNullSendTask { get; set; }
        public bool ReturnNullConnection { get; set; }
        public Exception? ConnectionFailure { get; set; }
        public Task? LastProvidedSendTask { get; private set; }
        public TaskCompletionSource SendEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TrackedConnection Connection { get; } = new();
        public ISendObserver? Observer { get; private set; }
        public int ConnectCount { get; private set; }
        public int DisposeCount { get; private set; }

        public Task SendAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
            where T : class
        {
            Type actualMessageType = typeof(T);
            Assert.Equal(ExpectedMessageType, actualMessageType);
            var context = new MessageSendContext<T>(message, cancellationToken);
            if (fixture.ExistingMetadata)
            {
                context.SourceAddress = ExistingSourceAddress;
                context.ConversationId = ExistingConversation;
            }
            fixture.Calls.Add(new Call((DispatchMessage)(object)message,
                context, pipe, cancellationToken, false, typeof(T)));
            if (ImmediateFailure is { } failure)
                throw failure;
            if (ReturnNullSendTask)
                return null!;
            LastProvidedSendTask = ImmediateOutcome ?? SendCoreAsync(context, pipe);
            return LastProvidedSendTask;
        }

        async Task SendCoreAsync<T>(SendContext<T> context, IPipe<SendContext<T>> pipe)
            where T : class
        {
            await pipe.SendAsync(context);
            SendEntered.TrySetResult();
            await DispatchTask;
        }

        public Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
            where T : class
        {
            Assert.Equal(typeof(DispatchMessage), typeof(T));
            fixture.Calls.Add(new Call((DispatchMessage)(object)message, fixture.CreatedContext,
                pipe, cancellationToken, true, typeof(T)));
            if (ImmediateCreationFailure is { } failure)
                throw failure;
            return (Task<SendContext<T>>)(object)CreateTask!;
        }

        public ConnectHandle ConnectSendObserver(ISendObserver observer)
        {
            Observer = observer;
            ConnectCount++;
            if (ConnectionFailure is { } failure)
                throw failure;
            return ReturnNullConnection ? null! : Connection;
        }

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return ValueTask.CompletedTask;
        }
    }

    sealed class TypedConfigurationPipe(Fixture fixture) : IPipe<SendContext<DispatchMessage>>, ISendContextPipe
    {
        public ProbeContext? LastProbe { get; private set; }
        public Task SendAsync(SendContext<DispatchMessage> context) => fixture.ApplyStageAsync(2, context);
        public Task SendAsync<T>(SendContext<T> context, CancellationToken cancellationToken) where T : class
            => fixture.ApplyStageAsync(0, context, cancellationToken);
        public void Probe(ProbeContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            LastProbe = context;
        }
    }

    sealed class UntypedConfigurationPipe(Fixture fixture) : IPipe<SendContext>, ISendContextPipe
    {
        public Task SendAsync(SendContext context) => fixture.ApplyStageAsync(2, context);
        public Task SendAsync<T>(SendContext<T> context, CancellationToken cancellationToken) where T : class
            => fixture.ApplyStageAsync(0, context, cancellationToken);
        public void Probe(ProbeContext context) => ArgumentNullException.ThrowIfNull(context);
    }

    sealed class EndpointConfigurationPipe(Fixture fixture) : ISendPipe
    {
        public Task SendAsync<T>(SendContext<T> context, CancellationToken cancellationToken) where T : class
            => fixture.ApplyStageAsync(1, context, cancellationToken);
        public void Probe(ProbeContext context) => ArgumentNullException.ThrowIfNull(context);
    }

    sealed class PlainTypedConfigurationPipe(Fixture fixture) : IPipe<SendContext<DispatchMessage>>
    {
        public Task SendAsync(SendContext<DispatchMessage> context) => fixture.ApplyStageAsync(2, context);
        public void Probe(ProbeContext context) => ArgumentNullException.ThrowIfNull(context);
    }

    sealed class PlainUntypedConfigurationPipe(Fixture fixture) : IPipe<SendContext>
    {
        public Task SendAsync(SendContext context) => fixture.ApplyStageAsync(2, context);
        public void Probe(ProbeContext context) => ArgumentNullException.ThrowIfNull(context);
    }

    sealed class InitializedConfigurationPipe : IPipe<SendContext<DispatchMessage>>
    {
        public Task? ConfigurationTask { get; init; }
        public SendContext<DispatchMessage>? Context { get; private set; }
        public int CallCount { get; private set; }
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task SendAsync(SendContext<DispatchMessage> context)
        {
            Context = context;
            CallCount++;
            Entered.TrySetResult();
            return ConfigurationTask!;
        }

        public void Probe(ProbeContext context) => ArgumentNullException.ThrowIfNull(context);
    }

    sealed class TrackedConnection : ConnectHandle
    {
        public int DisconnectCount { get; private set; }
        public void Disconnect() => DisconnectCount++;
        public void Dispose() => Disconnect();
    }

    class StrictProxy : DispatchProxy
    {
        Func<MethodInfo, object?[]?, object?>? _invoke;
        public static T Create<T>(Func<MethodInfo, object?[]?, object?> invoke) where T : class
        {
            T proxy = DispatchProxy.Create<T, StrictProxy>();
            ((StrictProxy)(object)proxy)._invoke = invoke;
            return proxy;
        }
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            return (_invoke ?? throw new InvalidOperationException("The proxy is not initialized."))(targetMethod, args);
        }
    }

    public interface IDispatchContract
    {
        string Text { get; }
    }

    public sealed class DispatchMessage : IDispatchContract
    {
        public string Text { get; set; } = string.Empty;
    }

    sealed record Call(DispatchMessage Message, SendContext Context,
        object Pipe, CancellationToken CancellationToken, bool CreatingContext, Type MessageType);
}

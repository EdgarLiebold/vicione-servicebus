using ViciOne.ServiceBus.Advanced.Initializers;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.Contexts;
using ViciOne.ServiceBus.Initializers.PropertyInitializers;
using ViciOne.ServiceBus.Initializers.PropertyProviders;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers;

public sealed class MessageInitializerLifetimeTests
{
    public static IEnumerable<object[]> Forms()
    {
        for (var form = 0; form < 5; form++)
            yield return [form];
    }

    public static IEnumerable<object[]> FormsAndOutcomes()
    {
        for (var form = 0; form < 5; form++)
            for (var outcome = 0; outcome < 3; outcome++)
                yield return [form, outcome];
    }

    public static IEnumerable<object[]> FormsAndCancellation()
    {
        for (var form = 0; form < 5; form++)
            foreach (bool cancel in new[] { false, true })
                yield return [form, cancel];
    }

    public static IEnumerable<object[]> CallbackFailures()
    {
        foreach (bool headers in new[] { false, true })
            for (var failure = 0; failure < 4; failure++)
                foreach (bool failingFirst in new[] { false, true })
                    yield return [headers, failure, failingFirst];
    }

    public static IEnumerable<object[]> MixedCallbackFailures()
    {
        foreach (bool headers in new[] { false, true })
            foreach (bool synchronousCancellation in new[] { false, true })
                foreach (bool synchronousFailure in new[] { false, true })
                    foreach (bool cancellationFirst in new[] { false, true })
                        yield return [headers, synchronousCancellation, synchronousFailure, cancellationFirst];
    }

    [Theory]
    [MemberData(nameof(FormsAndOutcomes))]
    [RequirementCoverage("REQ-VSB-INITIALIZER-LIFETIME", "all-entry-forms-own-started-property-callbacks-and-original-outcomes")]
    public async Task PropertyBatch_OwnsEveryStartedCallbackAndOriginalOutcomeAsync(int form, int outcome)
    {
        using var caller = new CancellationTokenSource();
        using var providerCancellation = new CancellationTokenSource();
        providerCancellation.Cancel();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var expected = new InvalidOperationException("owned property failed");
        var fixture = new Fixture(caller.Token);
        var callback = new Callback(0, fixture.Calls)
        {
            Body = async context =>
            {
                await gate.Task.ConfigureAwait(false);
                context.Message.Value = "applied";
            }
        };
        fixture.Properties = [callback];
        Task root = fixture.StartAsync(form, caller.Token);
        try
        {
            Assert.Equal(new[] { 0 }, fixture.Calls);
            Assert.Equal(caller.Token, callback.Token);
            Assert.Same(fixture.Input, callback.Context!.Input);
            Assert.False(root.IsCompleted);
            caller.Cancel();
            Assert.False(root.IsCompleted);
            Complete(gate, outcome, expected, providerCancellation.Token);
            await AssertOutcomeAsync(root, outcome, expected, providerCancellation.Token);
            Assert.True(callback.ReturnedTask!.IsCompleted);
            Assert.Equal(outcome == 0 ? "applied" : null, callback.Context.Message.Value);
            if (outcome == 0)
                Assert.Same(callback.Context.Message, await GetMessageAsync(root, form));
            Assert.Equal(0, fixture.Inner.TypedCalls);
        }
        finally
        {
            gate.TrySetResult();
            await ObserveAsync(gate.Task);
            await ObserveAsync(callback.ReturnedTask);
            await ObserveAsync(root);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-INITIALIZER-LIFETIME", "typed-header-batch-owns-original-outcome-and-accepted-downstream-chain")]
    public async Task HeaderBatch_OwnsStartedCallbacksAndOriginalOutcomeAsync(int outcome)
    {
        using var caller = new CancellationTokenSource();
        using var providerCancellation = new CancellationTokenSource();
        providerCancellation.Cancel();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var expected = new InvalidOperationException("owned header failed");
        var fixture = new Fixture(TestContext.Current.CancellationToken);
        var callback = new Callback(0, fixture.Calls)
        {
            Body = async _ => await gate.Task.ConfigureAwait(false)
        };
        fixture.Headers = [callback];
        InitializedMessage<Message> initialized = await fixture.PrepareAsync();
        var send = new MessageSendContext<Message>(initialized.Message, caller.Token);
        Task root = initialized.Pipe.SendAsync(send);
        try
        {
            Assert.Equal(new[] { 0 }, fixture.Calls);
            Assert.Equal(caller.Token, callback.Token);
            Assert.Same(send, callback.SendContext);
            Assert.Same(initialized.Message, callback.Context!.Message);
            Assert.Equal(0, fixture.Inner.TypedCalls);
            caller.Cancel();
            Assert.False(root.IsCompleted);
            Complete(gate, outcome, expected, providerCancellation.Token);
            await AssertOutcomeAsync(root, outcome, expected, providerCancellation.Token);
            Assert.True(callback.ReturnedTask!.IsCompleted);
            Assert.Equal(outcome == 0 ? 1 : 0, fixture.Inner.TypedCalls);
            if (outcome == 0)
                Assert.Same(send, fixture.Inner.TypedContext);
        }
        finally
        {
            gate.TrySetResult();
            await ObserveAsync(gate.Task);
            await ObserveAsync(callback.ReturnedTask);
            await ObserveAsync(root);
        }
    }

    [Theory]
    [MemberData(nameof(CallbackFailures))]
    [RequirementCoverage("REQ-VSB-INITIALIZER-LIFETIME", "parallel-property-or-header-callback-failures-observe-all-started-siblings")]
    public async Task ParallelCallbacks_ObserveHeldSiblingsAcrossEveryFailureFormAsync(bool headers, int failure, bool failingFirst)
    {
        using var providerCancellation = new CancellationTokenSource();
        providerCancellation.Cancel();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var fixture = new Fixture(TestContext.Current.CancellationToken);
        var expected = new InvalidOperationException("callback invocation failed");
        var canceled = new OperationCanceledException(providerCancellation.Token);
        int badIndex = failingFirst ? 0 : 1;
        var held = new Callback(1 - badIndex, fixture.Calls) { Body = _ => gate.Task };
        var bad = new Callback(badIndex, fixture.Calls)
        {
            Body = _ => failure switch
            {
                0 => throw expected,
                1 => Task.FromException(expected),
                2 => null!,
                3 => throw canceled,
                _ => throw new ArgumentOutOfRangeException(nameof(failure))
            }
        };
        var trailing = new Callback(2, fixture.Calls);
        Callback[] callbacks = failingFirst ? [bad, held, trailing] : [held, bad, trailing];
        if (headers)
            fixture.Headers = callbacks;
        else
            fixture.Properties = callbacks;
        Task root = headers
            ? (await fixture.PrepareAsync()).Pipe.SendAsync(new MessageSendContext<Message>(fixture.Factory.Last!.Message, fixture.Token))
            : fixture.StartAsync(0, fixture.Token);
        try
        {
            Assert.Equal(new[] { 0, 1, 2 }, fixture.Calls);
            Assert.Same(held.Context, bad.Context);
            Assert.Same(held.Context, trailing.Context);
            Assert.Equal(fixture.Token, held.Token);
            Assert.Equal(fixture.Token, bad.Token);
            Assert.Equal(fixture.Token, trailing.Token);
            Assert.False(root.IsCompleted);
            Assert.Equal(0, fixture.Inner.TypedCalls);
            gate.SetResult();
            if (failure == 3)
            {
                OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => root);
                Assert.Same(canceled, actual);
                Assert.Equal(providerCancellation.Token, actual.CancellationToken);
                Assert.True(root.IsCanceled);
            }
            else
            {
                InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() => root);
                if (failure == 2)
                    Assert.Equal($"A {(headers ? "header" : "property")} initializer returned a null task.", actual.Message);
                else
                    Assert.Same(expected, actual);
                Assert.True(root.IsFaulted);
            }
            Assert.True(held.ReturnedTask!.IsCompletedSuccessfully);
            Assert.True(trailing.ReturnedTask!.IsCompletedSuccessfully);
            Assert.Equal(0, fixture.Inner.TypedCalls);
        }
        finally
        {
            gate.TrySetResult();
            await ObserveAsync(gate.Task);
            foreach (Callback callback in callbacks)
                await ObserveAsync(callback.ReturnedTask);
            await ObserveAsync(root);
        }
    }

    [Theory]
    [MemberData(nameof(MixedCallbackFailures))]
    [RequirementCoverage("REQ-VSB-INITIALIZER-LIFETIME", "parallel-ordinary-failure-dominates-synchronous-or-task-cancellation-after-held-sibling")]
    public async Task ParallelCallbacks_OrdinaryFailureDominatesSynchronousOrTaskCancellationAsync(
        bool headers, bool synchronousCancellation, bool synchronousFailure, bool cancellationFirst)
    {
        using var caller = new CancellationTokenSource();
        using var providerCancellation = new CancellationTokenSource();
        providerCancellation.Cancel();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var fixture = new Fixture(caller.Token);
        var expected = new InvalidOperationException("ordinary callback failure must remain visible");
        var cancellation = new OperationCanceledException(providerCancellation.Token);
        var canceled = new Callback(cancellationFirst ? 0 : 1, fixture.Calls)
        {
            Body = _ => synchronousCancellation
                ? throw cancellation
                : Task.FromCanceled(providerCancellation.Token)
        };
        var failed = new Callback(cancellationFirst ? 1 : 0, fixture.Calls)
        {
            Body = _ => synchronousFailure ? throw expected : Task.FromException(expected)
        };
        var held = new Callback(2, fixture.Calls) { Body = _ => gate.Task };
        Callback[] callbacks = cancellationFirst ? [canceled, failed, held] : [failed, canceled, held];
        if (headers)
            fixture.Headers = callbacks;
        else
            fixture.Properties = callbacks;
        Task root;
        if (headers)
        {
            InitializedMessage<Message> initialized = await fixture.PrepareAsync();
            root = initialized.Pipe.SendAsync(new MessageSendContext<Message>(initialized.Message, caller.Token));
        }
        else
            root = fixture.StartAsync(0, caller.Token);

        try
        {
            Assert.Equal(new[] { 0, 1, 2 }, fixture.Calls);
            foreach (Callback callback in callbacks)
            {
                Assert.Same(held.Context, callback.Context);
                Assert.Equal(caller.Token, callback.Token);
            }
            Assert.Same(gate.Task, held.ReturnedTask);
            Assert.False(root.IsCompleted);
            Assert.Equal(0, fixture.Inner.TypedCalls);
            gate.SetResult();
            Assert.Same(expected, await Assert.ThrowsAsync<InvalidOperationException>(() => root));
            Assert.True(root.IsFaulted);
            Assert.False(root.IsCanceled);
            Assert.True(held.ReturnedTask!.IsCompletedSuccessfully);
            Assert.Equal(0, fixture.Inner.TypedCalls);
        }
        finally
        {
            gate.TrySetResult();
            await ObserveAsync(gate.Task);
            foreach (Callback callback in callbacks)
                await ObserveAsync(callback.ReturnedTask);
            await ObserveAsync(root);
        }
    }

    [Theory]
    [MemberData(nameof(Forms))]
    [RequirementCoverage("REQ-VSB-INITIALIZER-LIFETIME", "all-entry-forms-pre-cancellation-has-no-factory-or-callback-effects")]
    public async Task Initialization_PreCancellationHasNoFactoryOrCallbackEffectsAsync(int form)
    {
        using var caller = new CancellationTokenSource();
        caller.Cancel();
        var fixture = new Fixture(caller.Token);
        fixture.Properties = [new Callback(0, fixture.Calls)];
        Task root = fixture.StartAsync(form, caller.Token);
        OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => root);
        Assert.Equal(caller.Token, actual.CancellationToken);
        Assert.Equal(0, fixture.Factory.Calls);
        Assert.Empty(fixture.Calls);
        Assert.Equal(0, fixture.Inner.TypedCalls);
    }

    [Theory]
    [MemberData(nameof(FormsAndCancellation))]
    [RequirementCoverage("REQ-VSB-INITIALIZER-LIFETIME", "all-entry-forms-invalid-inputs-are-synchronous-before-factory-and-cancellation")]
    public void Initialization_InvalidInputsAreSynchronousBeforeFactoryAndCancellation(int form, bool cancel)
    {
        using var caller = new CancellationTokenSource();
        if (cancel)
            caller.Cancel();
        var fixture = new Fixture(caller.Token);
        fixture.Properties = [new Callback(0, fixture.Calls)];
        Assert.Equal("input", Assert.Throws<ArgumentNullException>(() => { _ = fixture.StartAsync(form, caller.Token, null); }).ParamName);
        ArgumentException incompatible = Assert.Throws<ArgumentException>(() => { _ = fixture.StartAsync(form, caller.Token, new object()); });
        Assert.Equal("input", incompatible.ParamName);
        Assert.Contains(typeof(Input).ToString(), incompatible.Message, StringComparison.Ordinal);
        Assert.Equal(0, fixture.Factory.Calls);
        Assert.Empty(fixture.Calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-INITIALIZER-LIFETIME", "multi-input-context-array-and-primary-guards-are-synchronous-before-factory")]
    public void MultiInput_ValidatesArrayAndContextSynchronouslyWithoutFactory(bool cancel)
    {
        using var caller = new CancellationTokenSource();
        if (cancel)
            caller.Cancel();
        var fixture = new Fixture(caller.Token);
        MessageInitializer<Message, Input> initializer = fixture.Create();
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = initializer.InitializeMessageAsync(null!, fixture.Input, [], fixture.Inner, caller.Token);
        }).ParamName);
        Assert.Equal("moreInputs", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = initializer.InitializeMessageAsync(fixture.Scope, fixture.Input, null!, fixture.Inner, caller.Token);
        }).ParamName);
        Assert.Equal(0, fixture.Factory.Calls);
        Assert.Empty(fixture.Calls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-LIFETIME", "actual-additional-input-snapshot-order-null-slots-and-primary-precedence")]
    public async Task AdditionalInputs_AreSnapshottedOrderedAndPrecedePrimaryAsync()
    {
        var gate = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var fixture = new Fixture(TestContext.Current.CancellationToken);
        string? valueBeforePrimary = null;
        fixture.Properties = [new Callback(0, fixture.Calls)
        {
            Body = context =>
            {
                valueBeforePrimary = context.Message.Value;
                context.Message.Value = "primary";
                return Task.CompletedTask;
            }
        }];
        object?[] inputs = [null, new TaskInput(gate.Task), null, new ScalarInput("second")];
        Task<InitializedMessage<Message>> root = fixture.Create().InitializeMessageAsync(
            fixture.Scope, fixture.Input, inputs, fixture.Inner, fixture.Token);
        try
        {
            Assert.False(root.IsCompleted);
            Assert.Empty(fixture.Calls);
            inputs[3] = new ScalarInput("replacement");
            gate.SetResult("first");
            InitializedMessage<Message> initialized = await root;
            Assert.Equal("second", valueBeforePrimary);
            Assert.Equal("primary", initialized.Message.Value);
            Assert.Same(fixture.Factory.Last!.Message, initialized.Message);
            Assert.Same(fixture.Input, fixture.Properties[0].Context!.Input);
            Assert.Equal(new[] { 0 }, fixture.Calls);
            Assert.Equal(0, fixture.Inner.TypedCalls);
        }
        finally
        {
            gate.TrySetResult("cleanup");
            await ObserveAsync(gate.Task);
            await ObserveAsync(root);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-INITIALIZER-LIFETIME", "actual-additional-input-original-failure-stops-before-primary")]
    public async Task AdditionalInputFailure_StopsBeforePrimaryWithOriginalOutcomeAsync(int outcome)
    {
        using var providerCancellation = new CancellationTokenSource();
        providerCancellation.Cancel();
        var gate = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var expected = new InvalidOperationException("additional input failed");
        var fixture = new Fixture(TestContext.Current.CancellationToken);
        fixture.Properties = [new Callback(0, fixture.Calls)];
        Task root = fixture.Create().InitializeMessageAsync(fixture.Scope, fixture.Input,
            [new TaskInput(gate.Task), new ScalarInput("forbidden")], fixture.Inner, fixture.Token);
        try
        {
            Assert.False(root.IsCompleted);
            if (outcome == 1)
                gate.SetException(expected);
            else
                gate.SetCanceled(providerCancellation.Token);
            await AssertOutcomeAsync(root, outcome, expected, providerCancellation.Token);
            Assert.Empty(fixture.Calls);
            Assert.Null(fixture.Factory.Last!.Message.Value);
            Assert.Equal(0, fixture.Inner.TypedCalls);
        }
        finally
        {
            gate.TrySetResult("cleanup");
            await ObserveAsync(gate.Task);
            await ObserveAsync(root);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-INITIALIZER-LIFETIME", "actual-caller-owned-task-input-cancels-promptly-without-settling-value")]
    public async Task CallerOwnedInput_CancellationRemainsPromptAndLeavesValueUnsettledAsync(int form)
    {
        using var caller = new CancellationTokenSource();
        var gate = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var fixture = new Fixture(caller.Token);
        Task root = form switch
        {
            0 => MessageInitializerCache<Message>.InitializeAsync(new TaskInput(gate.Task), caller.Token),
            1 => MessageInitializerCache<Message>.InitializeMessageAsync(new TaskInput(gate.Task), fixture.Inner, caller.Token),
            2 => fixture.Create().InitializeMessageAsync(fixture.Scope, fixture.Input, [new TaskInput(gate.Task)], fixture.Inner, caller.Token),
            _ => throw new ArgumentOutOfRangeException(nameof(form))
        };
        try
        {
            Assert.False(root.IsCompleted);
            caller.Cancel();
            OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                root.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
            Assert.Equal(caller.Token, actual.CancellationToken);
            Assert.False(gate.Task.IsCompleted);
            Assert.Empty(fixture.Calls);
            Assert.Equal(0, fixture.Inner.TypedCalls);
        }
        finally
        {
            gate.TrySetResult("cleanup");
            await ObserveAsync(gate.Task);
            await ObserveAsync(root);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-INITIALIZER-LIFETIME", "actual-input-task-cancels-locally-but-batch-drains-owned-sibling-and-preserves-failure")]
    public async Task CallerOwnedInput_CancelsLocallyWhileBatchOwnsHeldSiblingAndOriginalFailureAsync(
        bool inputFirst, bool ownedFailure)
    {
        using var caller = new CancellationTokenSource();
        var inputSource = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var ownedSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var expected = new InvalidOperationException("owned sibling failed after input cancellation");
        var input = new TaskInput(inputSource.Task);
        var calls = new List<int>();
        var factory = new Factory();
        var inputProvider = new InputPropertyProvider<TaskInput, Task<string?>>(
            typeof(TaskInput).GetProperty(nameof(TaskInput.Value)));
        var valueProvider = new AsyncPropertyProvider<TaskInput, string>(inputProvider);
        var inputInitializer = new ProviderPropertyInitializer<Message, TaskInput, string>(
            valueProvider, typeof(Message).GetProperty(nameof(Message.Value)));
        var inputCallback = new TaskInputCallback(inputFirst ? 0 : 1, calls, inputInitializer.ApplyAsync);
        var ownedCallback = new TaskInputCallback(inputFirst ? 1 : 0, calls, (_, _) => ownedSource.Task);
        IPropertyInitializer<Message, TaskInput>[] callbacks = inputFirst
            ? [inputCallback, ownedCallback]
            : [ownedCallback, inputCallback];
        var initializer = new MessageInitializer<Message, TaskInput>(factory, callbacks, []);
        Task<InitializeContext<Message>> root = initializer.InitializeAsync(input, caller.Token);
        try
        {
            Assert.Equal(new[] { 0, 1 }, calls);
            Assert.Same(inputCallback.Context, ownedCallback.Context);
            Assert.Same(input, inputCallback.Context!.Input);
            Assert.Equal(caller.Token, inputCallback.Token);
            Assert.Equal(caller.Token, ownedCallback.Token);
            Assert.Same(ownedSource.Task, ownedCallback.ReturnedTask);
            Assert.False(root.IsCompleted);
            caller.Cancel();
            OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                inputCallback.ReturnedTask!.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
            Assert.Equal(caller.Token, canceled.CancellationToken);
            Assert.True(inputCallback.ReturnedTask!.IsCanceled);
            Assert.False(inputSource.Task.IsCompleted);
            Assert.False(ownedSource.Task.IsCompleted);
            Assert.False(root.IsCompleted);
            if (ownedFailure)
                ownedSource.SetException(expected);
            else
                ownedSource.SetResult();
            await AssertOutcomeAsync(root, ownedFailure ? 1 : 2, expected, caller.Token);
            Assert.True(ownedCallback.ReturnedTask!.IsCompleted);
            Assert.False(inputSource.Task.IsCompleted);
            Assert.Null(factory.Last!.Message.Value);
        }
        finally
        {
            inputSource.TrySetResult("cleanup");
            ownedSource.TrySetResult();
            await ObserveAsync(inputSource.Task);
            await ObserveAsync(ownedSource.Task);
            await ObserveAsync(inputCallback.ReturnedTask);
            await ObserveAsync(ownedCallback.ReturnedTask);
            await ObserveAsync(root);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-INITIALIZER-LIFETIME", "initialized-general-pipe-null-task-diagnostic-and-completed-task-identity")]
    public async Task GenericInitializedPipe_RejectsNullTaskAndPreservesOriginalTaskAsync(int outcome)
    {
        using var explicitCaller = new CancellationTokenSource();
        using var sendCancellation = new CancellationTokenSource();
        using var providerCancellation = new CancellationTokenSource();
        providerCancellation.Cancel();
        var expected = new InvalidOperationException("general configuration failed");
        var fixture = new Fixture(TestContext.Current.CancellationToken);
        fixture.Headers = [new Callback(0, fixture.Calls)];
        fixture.Inner.GeneralTask = outcome switch
        {
            0 => null!,
            1 => Task.FromException(expected),
            2 => Task.FromCanceled(providerCancellation.Token),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome))
        };
        InitializedMessage<Message> initialized = await fixture.PrepareAsync();
        var send = new MessageSendContext<Message>(initialized.Message, sendCancellation.Token);
        ISendPipe pipe = Assert.IsAssignableFrom<ISendPipe>(initialized.Pipe);
        Task? root = null;
        try
        {
            if (outcome == 0)
            {
                InvalidOperationException actual = Assert.Throws<InvalidOperationException>(() =>
                {
                    root = pipe.SendAsync(send, explicitCaller.Token);
                });
                Assert.Equal("The initialized general send pipe returned no configuration task.", actual.Message);
            }
            else
            {
                root = pipe.SendAsync(send, explicitCaller.Token);
                Assert.Same(fixture.Inner.GeneralTask, root);
                await AssertOutcomeAsync(root, outcome, expected, providerCancellation.Token);
            }
            Assert.Equal(1, fixture.Inner.GeneralCalls);
            Assert.Same(send, fixture.Inner.GeneralContext);
            Assert.Equal(explicitCaller.Token, fixture.Inner.GeneralToken);
            Assert.Empty(fixture.Calls);
            Assert.Equal(0, fixture.Inner.TypedCalls);
        }
        finally
        {
            await ObserveAsync(fixture.Inner.GeneralTask);
            await ObserveAsync(root);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-INITIALIZER-LIFETIME", "initialized-general-pipe-owns-held-provider-task-and-original-outcome")]
    public async Task GenericInitializedPipe_OwnsHeldConfigurationAcrossOriginalOutcomesAsync(int outcome)
    {
        using var caller = new CancellationTokenSource();
        using var providerCancellation = new CancellationTokenSource();
        providerCancellation.Cancel();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var expected = new InvalidOperationException("held general configuration failed");
        var fixture = new Fixture(caller.Token);
        fixture.Headers = [new Callback(0, fixture.Calls)];
        fixture.Inner.GeneralTask = gate.Task;
        InitializedMessage<Message> initialized = await fixture.PrepareAsync();
        var send = new MessageSendContext<Message>(initialized.Message, TestContext.Current.CancellationToken);
        Task root = Assert.IsAssignableFrom<ISendPipe>(initialized.Pipe).SendAsync(send, caller.Token);
        try
        {
            Assert.Same(gate.Task, root);
            Assert.Equal(caller.Token, fixture.Inner.GeneralToken);
            Assert.Same(send, fixture.Inner.GeneralContext);
            caller.Cancel();
            Assert.False(root.IsCompleted);
            Complete(gate, outcome, expected, providerCancellation.Token);
            await AssertOutcomeAsync(root, outcome, expected, providerCancellation.Token);
            Assert.Empty(fixture.Calls);
            Assert.Equal(0, fixture.Inner.TypedCalls);
        }
        finally
        {
            gate.TrySetResult();
            await ObserveAsync(gate.Task);
            await ObserveAsync(root);
        }
    }

    [Theory]
    [MemberData(nameof(Forms))]
    [RequirementCoverage("REQ-VSB-INITIALIZER-LIFETIME", "all-entry-forms-return-exact-populated-message-without-dispatch")]
    public async Task Initialization_ReturnsCorrectMessageAndContextAcrossFormsAsync(int form)
    {
        using var caller = new CancellationTokenSource();
        var fixture = new Fixture(caller.Token);
        fixture.Properties = [new Callback(0, fixture.Calls)
        {
            Body = context =>
            {
                context.Message.Value = "ready";
                return Task.CompletedTask;
            }
        }];
        Task root = fixture.StartAsync(form, fixture.Token);
        Message message = await GetMessageAsync(root, form);
        Assert.Equal("ready", message.Value);
        Assert.Same(form == 1 ? fixture.Existing.Message : fixture.Factory.Last!.Message, message);
        if (form is 0 or 1)
            Assert.Same(form == 1 ? fixture.Existing : fixture.Factory.Last,
                await Assert.IsAssignableFrom<Task<InitializeContext<Message>>>(root));
        Assert.Same(fixture.Input, fixture.Properties[0].Context!.Input);
        Assert.Equal(fixture.Token, fixture.Properties[0].Token);
        Assert.Equal(form == 1 ? 0 : 1, fixture.Factory.Calls);
        Assert.Equal(new[] { 0 }, fixture.Calls);
        Assert.Equal(0, fixture.Inner.TypedCalls);
    }

    static void Complete(TaskCompletionSource source, int outcome, Exception failure, CancellationToken providerToken)
    {
        if (outcome == 0)
            source.SetResult();
        else if (outcome == 1)
            source.SetException(failure);
        else
            source.SetCanceled(providerToken);
    }

    static async Task AssertOutcomeAsync(Task task, int outcome, InvalidOperationException expected, CancellationToken providerToken)
    {
        if (outcome == 0)
        {
            await task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            Assert.True(task.IsCompletedSuccessfully);
        }
        else if (outcome == 1)
        {
            Assert.Same(expected, await Assert.ThrowsAsync<InvalidOperationException>(() => task));
            Assert.True(task.IsFaulted);
        }
        else
        {
            OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
            Assert.Equal(providerToken, actual.CancellationToken);
            Assert.True(task.IsCanceled);
        }
    }

    static async Task<Message> GetMessageAsync(Task task, int form) => form is 0 or 1
        ? (await Assert.IsAssignableFrom<Task<InitializeContext<Message>>>(task)).Message
        : (await Assert.IsAssignableFrom<Task<InitializedMessage<Message>>>(task)).Message;

    static async Task ObserveAsync(Task? task)
    {
        if (task is not null)
        {
            await Record.ExceptionAsync(() => task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
            Assert.True(task.IsCompleted);
        }
    }

    sealed class Fixture
    {
        public Fixture(CancellationToken token)
        {
            Token = token;
            Scope = new BaseInitializeContext(TestContext.Current.CancellationToken);
            Existing = Scope.CreateMessageContext(new Message());
        }

        public CancellationToken Token { get; }
        public BaseInitializeContext Scope { get; }
        public InitializeContext<Message> Existing { get; }
        public Input Input { get; } = new();
        public Factory Factory { get; } = new();
        public InnerPipe Inner { get; } = new();
        public List<int> Calls { get; } = [];
        public Callback[] Properties { get; set; } = [];
        public Callback[] Headers { get; set; } = [];

        public MessageInitializer<Message, Input> Create() => new(Factory, Properties, Headers);

        public Task StartAsync(int form, CancellationToken token) => StartAsync(form, token, Input);

        public Task StartAsync(int form, CancellationToken token, object? input)
        {
            MessageInitializer<Message, Input> initializer = Create();
            return form switch
            {
                0 => initializer.InitializeAsync(input!, token),
                1 => initializer.InitializeAsync(Existing, input!, token),
                2 => initializer.InitializeMessageAsync(Scope, input!, Inner, token),
                3 => initializer.InitializeMessageAsync(input!, Inner, token),
                4 => initializer.InitializeMessageAsync(Scope, input!, [], Inner, token),
                _ => throw new ArgumentOutOfRangeException(nameof(form))
            };
        }

        public Task<InitializedMessage<Message>> PrepareAsync() => Create().InitializeMessageAsync(Input, Inner, Token);
    }

    sealed class Factory : IMessageFactory<Message>
    {
        public int Calls { get; private set; }
        public InitializeContext<Message>? Last { get; private set; }

        public InitializeContext<Message> Create(InitializeContext context)
        {
            Calls++;
            return Last = context.CreateMessageContext(new Message());
        }
    }

    sealed class Callback(int index, List<int> calls) : IPropertyInitializer<Message, Input>, IHeaderInitializer<Message, Input>
    {
        public Func<InitializeContext<Message, Input>, Task> Body { get; init; } = _ => Task.CompletedTask;
        public InitializeContext<Message, Input>? Context { get; private set; }
        public SendContext? SendContext { get; private set; }
        public CancellationToken Token { get; private set; }
        public Task? ReturnedTask { get; private set; }

        public Task ApplyAsync(InitializeContext<Message, Input> context, CancellationToken cancellationToken = default) =>
            ApplyAsync(context, null, cancellationToken);

        public Task ApplyAsync(InitializeContext<Message, Input> context, SendContext? sendContext, CancellationToken cancellationToken = default)
        {
            Context = context;
            SendContext = sendContext;
            Token = cancellationToken;
            calls.Add(index);
            return ReturnedTask = Body(context);
        }
    }

    sealed class TaskInputCallback(
        int index, List<int> calls, Func<InitializeContext<Message, TaskInput>, CancellationToken, Task> apply)
        : IPropertyInitializer<Message, TaskInput>
    {
        public InitializeContext<Message, TaskInput>? Context { get; private set; }
        public CancellationToken Token { get; private set; }
        public Task? ReturnedTask { get; private set; }

        public Task ApplyAsync(InitializeContext<Message, TaskInput> context, CancellationToken cancellationToken = default)
        {
            Context = context;
            Token = cancellationToken;
            calls.Add(index);
            return ReturnedTask = apply(context, cancellationToken);
        }
    }

    sealed class InnerPipe : IPipe<SendContext<Message>>, ISendContextPipe
    {
        public int TypedCalls { get; private set; }
        public int GeneralCalls { get; private set; }
        public SendContext? TypedContext { get; private set; }
        public object? GeneralContext { get; private set; }
        public CancellationToken GeneralToken { get; private set; }
        public Task GeneralTask { get; set; } = Task.CompletedTask;

        public Task SendAsync(SendContext<Message> context)
        {
            TypedCalls++;
            TypedContext = context;
            return Task.CompletedTask;
        }

        public Task SendAsync<T>(SendContext<T> context, CancellationToken cancellationToken = default) where T : class
        {
            GeneralCalls++;
            GeneralContext = context;
            GeneralToken = cancellationToken;
            return GeneralTask;
        }

        public void Probe(ProbeContext context) => ArgumentNullException.ThrowIfNull(context);
    }

    public sealed class Message
    {
        public string? Value { get; set; }
    }

    sealed class Input;
    public sealed record TaskInput(Task<string?> Value);
    public sealed record ScalarInput(string? Value);
}

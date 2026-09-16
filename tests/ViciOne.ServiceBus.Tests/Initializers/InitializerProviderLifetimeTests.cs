using System.Reflection;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.Contexts;
using ViciOne.ServiceBus.Initializers.HeaderInitializers;
using ViciOne.ServiceBus.Initializers.PropertyInitializers;
using ViciOne.ServiceBus.Initializers.PropertyProviders;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers;

public sealed class InitializerProviderLifetimeTests
{
    public static TheoryData<int, int, bool> AssignmentOutcomes => CreateOutcomes(3, 3);
    public static TheoryData<int, int, bool> OuterOutcomes => CreateOutcomes(2, 8);
    public static TheoryData<int, int, bool> InnerOutcomes => CreateOutcomes(2, 3);
    public static TheoryData<int, bool> AssignmentGuards => CreateForms(3);
    public static TheoryData<int, bool> AsyncAbsence => CreateForms(2);
    public static TheoryData<int, bool> SingleStageOutcomes => CreateForms(3);

    [Theory]
    [MemberData(nameof(AssignmentOutcomes))]
    [RequirementCoverage("REQ-VSB-INITIALIZER-PROVIDER-LIFETIME", "assignment-owns-held-provider-original-outcomes")]
    public async Task Initializers_OwnHeldProviderAndOriginalOutcomeAsync(int form, int outcome, bool cancelCaller)
    {
        using var caller = new CancellationTokenSource();
        using var providerCancellation = new CancellationTokenSource();
        providerCancellation.Cancel();
        var original = NewSource<Guid?>();
        var provider = new DelegateProvider<Guid?>(() => original.Task);
        var before = Guid.NewGuid();
        var after = Guid.NewGuid();
        var message = new Message { Value = before };
        var context = CreateContext(message);
        var send = CreateSend(message, before);
        var failure = new InvalidOperationException("held assignment provider failed");
        Task root = StartAssignmentAsync(form, provider, context, send, caller.Token);
        try
        {
            Assert.Same(context, provider.Context);
            Assert.Equal(caller.Token, provider.Token);
            Assert.Equal(1, provider.Calls);
            Assert.Equal(before, ReadAssignment(form, message, send));
            if (cancelCaller)
                caller.Cancel();
            await AssertPendingAsync(root);
            Assert.False(original.Task.IsCompleted);
            Complete(original, outcome, after, failure, providerCancellation.Token);
            await AssertOutcomeAsync(root, outcome, failure, providerCancellation.Token);
            Assert.Equal(outcome == 0 ? after : before, ReadAssignment(form, message, send));
            Assert.Equal(form == 0 && outcome == 0 ? after : before, message.Value);
            Assert.Equal(form == 1 && outcome == 0 ? after : before, send.CorrelationId);
        }
        finally
        {
            original.TrySetResult(after);
            await ObserveAsync(original.Task);
            await ObserveAsync(root);
        }
    }

    [Theory]
    [MemberData(nameof(AssignmentGuards))]
    [RequirementCoverage("REQ-VSB-INITIALIZER-PROVIDER-LIFETIME", "assignment-synchronous-guards-and-precancellation-effects")]
    public async Task Initializers_ValidateArgumentsAndPreCancellationBeforeProviderEffectsAsync(int form, bool cancelCaller)
    {
        using var caller = new CancellationTokenSource();
        if (cancelCaller)
            caller.Cancel();
        var provider = new DelegateProvider<Guid?>(() => throw new InvalidOperationException("provider must not start"));
        var before = Guid.NewGuid();
        var message = new Message { Value = before };
        var context = CreateContext(message);
        var send = CreateSend(message, before);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = StartAssignmentAsync(form, provider, null!, send, caller.Token);
        }).ParamName);
        if (form != 0)
            Assert.Equal("sendContext", Assert.Throws<ArgumentNullException>(() =>
            {
                _ = StartAssignmentAsync(form, provider, context, null!, caller.Token);
            }).ParamName);
        Assert.Equal(0, provider.Calls);
        if (cancelCaller)
        {
            Task root = StartAssignmentAsync(form, provider, context, send, caller.Token);
            var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => root);
            Assert.Equal(caller.Token, failure.CancellationToken);
            Assert.True(root.IsCanceled);
        }
        else
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                _ = StartAssignmentAsync(form, provider, context, send, caller.Token);
            });
            Assert.Equal(1, provider.Calls);
            Assert.Same(context, provider.Context);
            Assert.Equal(caller.Token, provider.Token);
        }
        Assert.Equal(before, ReadAssignment(form, message, send));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-INITIALIZER-PROVIDER-LIFETIME", "assignment-immediate-value-null-fault-cancel-and-null-task")]
    public async Task Initializers_PreserveImmediateOutcomesAndNullTaskDiagnosticsAsync(int form)
    {
        using var providerCancellation = new CancellationTokenSource();
        providerCancellation.Cancel();
        var before = Guid.NewGuid();
        var after = Guid.NewGuid();
        var failure = new InvalidOperationException("immediate provider failed");
        Func<Task<Guid?>?>[] originals =
        [
            () => Task.FromResult<Guid?>(after), () => Task.FromResult<Guid?>(null),
            () => Task.FromException<Guid?>(failure), () => Task.FromCanceled<Guid?>(providerCancellation.Token), () => null
        ];
        foreach (Func<Task<Guid?>?> getOriginal in originals)
        {
            Task<Guid?>? original = getOriginal();
            var message = new Message { Value = before };
            var context = CreateContext(message);
            var send = CreateSend(message, before);
            var provider = new DelegateProvider<Guid?>(() => original!);
            Task? root = null;
            try
            {
                if (original == null)
                {
                    var missing = Assert.Throws<InvalidOperationException>(() =>
                    {
                        _ = StartAssignmentAsync(form, provider, context, send, TestContext.Current.CancellationToken);
                    });
                    Assert.Equal("The property provider returned a null task.", missing.Message);
                    Assert.Equal(before, ReadAssignment(form, message, send));
                }
                else
                {
                    root = StartAssignmentAsync(form, provider, context, send, TestContext.Current.CancellationToken);
                    if (original.IsCompletedSuccessfully)
                    {
                        await root;
                        Assert.Equal(await original, ReadAssignment(form, message, send));
                        Assert.True(root.IsCompletedSuccessfully);
                    }
                    else
                    {
                        await AssertOutcomeAsync(root, original.IsCanceled ? 2 : 1, failure, providerCancellation.Token);
                        Assert.Equal(before, ReadAssignment(form, message, send));
                    }
                }
                Assert.Equal(1, provider.Calls);
                Assert.Same(context, provider.Context);
                Assert.Equal(TestContext.Current.CancellationToken, provider.Token);
            }
            finally
            {
                if (original != null)
                    await ObserveAsync(original);
                if (root != null)
                    await ObserveAsync(root);
            }
        }
    }

    [Theory]
    [MemberData(nameof(OuterOutcomes))]
    [RequirementCoverage("REQ-VSB-INITIALIZER-PROVIDER-LIFETIME", "async-owns-outer-and-retains-inner-outcomes-after-caller-cancel")]
    public async Task AsyncAdapters_OwnOuterProviderAndOriginalOutcomeAsync(int form, int outcome, bool cancelCaller)
    {
        using var caller = new CancellationTokenSource();
        using var providerCancellation = new CancellationTokenSource();
        providerCancellation.Cancel();
        var outer = NewSource<Task<string?>>();
        var pendingInner = NewSource<string>();
        var failure = new InvalidOperationException("original task-property failure");
        var source = new DelegateProvider<Task<string?>>(() => outer.Task);
        var converter = new DelegateConverter(() => Task.FromResult<string?>("converted"));
        IPropertyProvider<Input, string> provider = CreateAsyncProvider(form, source, converter);
        var context = CreateContext(new Message());
        Task<string?> root = provider.GetPropertyAsync(context, caller.Token);
        Task<string?>? inner = null;
        try
        {
            Assert.Same(context, source.Context);
            Assert.Equal(caller.Token, source.Token);
            Assert.Equal(1, source.Calls);
            Assert.Equal(0, converter.Calls);
            if (cancelCaller)
                caller.Cancel();
            await AssertPendingAsync(root);
            if (outcome == 2)
                outer.SetException(failure);
            else if (outcome == 3)
                outer.SetCanceled(providerCancellation.Token);
            else
            {
                inner = outcome switch
                {
                    1 => null,
                    4 => Task.FromException<string?>(failure),
                    5 => Task.FromCanceled<string?>(providerCancellation.Token),
                    6 => pendingInner.Task,
                    7 => Task.FromResult<string?>(null),
                    _ => Task.FromResult<string?>("input")
                };
                outer.SetResult(inner);
            }
            if (outcome == 6 && !cancelCaller)
            {
                await AssertPendingAsync(root);
                pendingInner.SetResult("input");
            }
            if (outcome is 2 or 4)
                await AssertOutcomeAsync(root, 1, failure, providerCancellation.Token);
            else if (outcome is 3 or 5)
                await AssertOutcomeAsync(root, 2, failure, providerCancellation.Token);
            else if (outcome == 6 && cancelCaller)
            {
                var canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => BoundedAsync(root));
                Assert.Equal(caller.Token, canceled.CancellationToken);
                Assert.True(root.IsCanceled);
                Assert.False(pendingInner.Task.IsCompleted);
            }
            else
            {
                string? expectedValue = outcome == 1 || (form == 0 && outcome == 7)
                    ? null : form == 0 ? "input" : "converted";
                Assert.Equal(expectedValue, await BoundedAsync(root));
                Assert.True(root.IsCompletedSuccessfully);
            }
            bool converted = form == 1 && (outcome is 0 or 6 or 7) && !(outcome == 6 && cancelCaller);
            Assert.Equal(converted ? 1 : 0, converter.Calls);
            if (converted)
            {
                Assert.Same(context, converter.Context);
                Assert.Equal(outcome == 7 ? null : "input", converter.Input);
                Assert.Equal(caller.Token, converter.Token);
            }
        }
        finally
        {
            outer.TrySetResult(Task.FromResult<string?>("cleanup"));
            pendingInner.TrySetResult("cleanup");
            await ObserveAsync(outer.Task);
            await ObserveAsync(pendingInner.Task);
            if (inner != null)
                await ObserveAsync(inner);
            await ObserveAsync(root);
        }
    }

    [Theory]
    [MemberData(nameof(InnerOutcomes))]
    [RequirementCoverage("REQ-VSB-INITIALIZER-PROVIDER-LIFETIME", "caller-owned-inner-value-local-cancel-and-original-outcomes")]
    public async Task AsyncAdapters_KeepCallerOwnedInnerValuesLocallyCancellableAsync(int form, int outcome, bool cancelCaller)
    {
        using var caller = new CancellationTokenSource();
        using var inputCancellation = new CancellationTokenSource();
        inputCancellation.Cancel();
        var inner = NewSource<string>();
        var failure = new InvalidOperationException("caller input failed");
        var source = new DelegateProvider<Task<string?>>(() => Task.FromResult<Task<string?>?>(inner.Task));
        var converter = new DelegateConverter(() => Task.FromResult<string?>("converted"));
        var context = CreateContext(new Message());
        Task<string?> root = CreateAsyncProvider(form, source, converter).GetPropertyAsync(context, caller.Token);
        try
        {
            Assert.False(root.IsCompleted);
            Assert.Equal(caller.Token, source.Token);
            if (cancelCaller)
            {
                caller.Cancel();
                var canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => BoundedAsync(root));
                Assert.Equal(caller.Token, canceled.CancellationToken);
                Assert.True(root.IsCanceled);
                Assert.False(inner.Task.IsCompleted);
                Assert.Equal(0, converter.Calls);
                Complete(inner, outcome, "input", failure, inputCancellation.Token);
                Assert.True(root.IsCanceled);
            }
            else
            {
                Complete(inner, outcome, "input", failure, inputCancellation.Token);
                await AssertOutcomeAsync(root, outcome, failure, inputCancellation.Token);
                if (outcome == 0)
                    Assert.Equal(form == 0 ? "input" : "converted", await root);
                Assert.Equal(form == 1 && outcome == 0 ? 1 : 0, converter.Calls);
            }
        }
        finally
        {
            inner.TrySetResult("cleanup");
            await ObserveAsync(inner.Task);
            await ObserveAsync(root);
        }
    }

    [Theory]
    [MemberData(nameof(SingleStageOutcomes))]
    [RequirementCoverage("REQ-VSB-INITIALIZER-PROVIDER-LIFETIME", "async-converter-owns-held-original-outcomes")]
    public async Task AsyncConverter_OwnsHeldConversionAndOriginalOutcomeAsync(int outcome, bool cancelCaller)
    {
        using var caller = new CancellationTokenSource();
        using var converterCancellation = new CancellationTokenSource();
        converterCancellation.Cancel();
        var original = NewSource<string>();
        var failure = new InvalidOperationException("held converter failed");
        var source = new DelegateProvider<Task<string?>>(() => Task.FromResult<Task<string?>?>(Task.FromResult<string?>("input")));
        var converter = new DelegateConverter(() => original.Task);
        var context = CreateContext(new Message());
        Task<string?> root = CreateAsyncProvider(1, source, converter).GetPropertyAsync(context, caller.Token);
        try
        {
            Assert.Same(context, converter.Context);
            Assert.Equal("input", converter.Input);
            Assert.Equal(caller.Token, converter.Token);
            Assert.Equal(1, converter.Calls);
            if (cancelCaller)
                caller.Cancel();
            await AssertPendingAsync(root);
            Complete(original, outcome, "converted", failure, converterCancellation.Token);
            await AssertOutcomeAsync(root, outcome, failure, converterCancellation.Token);
            if (outcome == 0)
                Assert.Equal("converted", await root);
        }
        finally
        {
            original.TrySetResult("cleanup");
            await ObserveAsync(original.Task);
            await ObserveAsync(root);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-INITIALIZER-PROVIDER-LIFETIME", "held-converter-after-canceled-outer-resolution")]
    public async Task AsyncConverter_OwnsHeldConversionAfterCanceledOuterResolutionAsync(int outcome)
    {
        using var caller = new CancellationTokenSource();
        using var converterCancellation = new CancellationTokenSource();
        converterCancellation.Cancel();
        var outer = NewSource<Task<string?>>();
        var original = NewSource<string>();
        var converter = new DelegateConverter(() => original.Task);
        var source = new DelegateProvider<Task<string?>>(() => outer.Task);
        var context = CreateContext(new Message());
        var failure = new InvalidOperationException("accepted converter failed after caller cancellation");
        Task<string?> root = CreateAsyncProvider(1, source, converter).GetPropertyAsync(context, caller.Token);
        try
        {
            caller.Cancel();
            await AssertPendingAsync(root);
            outer.SetResult(Task.FromResult<string?>("input"));
            await BoundedAsync(converter.Started.Task);
            Assert.Equal(1, converter.Calls);
            Assert.Same(context, converter.Context);
            Assert.Equal("input", converter.Input);
            Assert.Equal(caller.Token, converter.Token);
            Assert.True(converter.Token.IsCancellationRequested);
            await AssertPendingAsync(root);
            Complete(original, outcome, "converted", failure, converterCancellation.Token);
            await AssertOutcomeAsync(root, outcome, failure, converterCancellation.Token);
            if (outcome == 0)
                Assert.Equal("converted", await root);
        }
        finally
        {
            outer.TrySetResult(Task.FromResult<string?>("cleanup"));
            original.TrySetResult("cleanup");
            converter.Started.TrySetResult();
            await ObserveAsync(outer.Task);
            await ObserveAsync(original.Task);
            await ObserveAsync(converter.Started.Task);
            await ObserveAsync(root);
        }
    }

    [Theory]
    [MemberData(nameof(SingleStageOutcomes))]
    [RequirementCoverage("REQ-VSB-INITIALIZER-PROVIDER-LIFETIME", "task-adapter-exact-original-export-without-flattening")]
    public async Task TaskAdapter_ExportsExactOriginalOperationWithoutAwaitingAsync(int outcome, bool cancelCaller)
    {
        using var caller = new CancellationTokenSource();
        using var providerCancellation = new CancellationTokenSource();
        providerCancellation.Cancel();
        var original = NewSource<string>();
        var failure = new InvalidOperationException("exported operation failed");
        if (outcome != 0)
            Complete(original, outcome, "input", failure, providerCancellation.Token);
        var source = new DelegateProvider<string>(() => original.Task);
        var context = CreateContext(new Message());
        var adapter = new TaskPropertyProvider<Input, string>(source);
        Task<Task<string?>?> outer = adapter.GetPropertyAsync(context, caller.Token);
        try
        {
            Assert.True(outer.IsCompletedSuccessfully);
            Assert.Same(original.Task, await outer);
            Assert.Same(context, source.Context);
            Assert.Equal(caller.Token, source.Token);
            Assert.Equal(1, source.Calls);
            if (cancelCaller)
                caller.Cancel();
            Assert.True(outer.IsCompletedSuccessfully);
            Assert.Same(original.Task, await outer);
            if (outcome == 0)
            {
                Assert.False(original.Task.IsCompleted);
                original.SetResult("input");
            }
            await AssertOutcomeAsync(original.Task, outcome, failure, providerCancellation.Token);
            Assert.True(outer.IsCompletedSuccessfully);
        }
        finally
        {
            original.TrySetResult("cleanup");
            await ObserveAsync(original.Task);
            await ObserveAsync(outer);
        }
    }

    [Theory]
    [MemberData(nameof(AsyncAbsence))]
    [RequirementCoverage("REQ-VSB-INITIALIZER-PROVIDER-LIFETIME", "async-absent-input-and-precancellation-zero-effects")]
    public async Task AsyncAdapters_PreserveAbsentInputAndPreCancellationEffectsAsync(int form, bool cancelCaller)
    {
        using var caller = new CancellationTokenSource();
        if (cancelCaller)
            caller.Cancel();
        var source = new DelegateProvider<Task<string?>>(() => throw new InvalidOperationException("provider must not start"));
        var converter = new DelegateConverter(() => throw new InvalidOperationException("converter must not start"));
        var context = DispatchProxy.Create<InitializeContext<Message, Input>, NoInputContext>();
        Task<string?> root = CreateAsyncProvider(form, source, converter).GetPropertyAsync(context, caller.Token);
        if (cancelCaller)
        {
            var canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => root);
            Assert.Equal(caller.Token, canceled.CancellationToken);
            Assert.True(root.IsCanceled);
            Assert.Equal(0, ((NoInputContext)(object)context).Reads);
        }
        else
        {
            Assert.Null(await root);
            Assert.True(root.IsCompletedSuccessfully);
            Assert.Equal(1, ((NoInputContext)(object)context).Reads);
        }
        Assert.Equal(0, source.Calls);
        Assert.Equal(0, converter.Calls);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [RequirementCoverage("REQ-VSB-INITIALIZER-PROVIDER-LIFETIME", "async-null-provider-and-converter-task-diagnostics")]
    public async Task AsyncAdapters_RejectNullOuterAndConverterTasksAsync(int form, bool nullConverter)
    {
        var context = CreateContext(new Message());
        var source = new DelegateProvider<Task<string?>>(() => nullConverter
            ? Task.FromResult<Task<string?>?>(Task.FromResult<string?>("input")) : null!);
        var converter = new DelegateConverter(() => null!);
        Task<string?> root = CreateAsyncProvider(form, source, converter)
            .GetPropertyAsync(context, TestContext.Current.CancellationToken);
        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => root);
        Assert.Equal(nullConverter ? "The property converter returned null." : "The task-property provider returned null.", failure.Message);
        Assert.True(root.IsFaulted);
        Assert.Same(context, source.Context);
        Assert.Equal(1, source.Calls);
        Assert.Equal(nullConverter ? 1 : 0, converter.Calls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-PROVIDER-LIFETIME", "async-construction-and-context-guards-before-dependencies")]
    public async Task AsyncAdapters_ValidateConstructionAndContextBeforeDependenciesAsync()
    {
        var source = new DelegateProvider<Task<string?>>(() => throw new InvalidOperationException("provider must not start"));
        var converter = new DelegateConverter(() => throw new InvalidOperationException("converter must not start"));
        Assert.Equal("provider", Assert.Throws<ArgumentNullException>(() => new AsyncPropertyProvider<Input, string>(null!)).ParamName);
        Assert.Equal("provider", Assert.Throws<ArgumentNullException>(() => new AsyncPropertyProvider<Input, string, string>(null!, converter)).ParamName);
        Assert.Equal("converter", Assert.Throws<ArgumentNullException>(() => new AsyncPropertyProvider<Input, string, string>(source, null!)).ParamName);
        using var caller = new CancellationTokenSource();
        caller.Cancel();
        for (int form = 0; form < 2; form++)
        {
            var failure = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                CreateAsyncProvider(form, source, converter).GetPropertyAsync<Message>(null!, caller.Token));
            Assert.Equal("context", failure.ParamName);
        }
        Assert.Equal(0, source.Calls);
        Assert.Equal(0, converter.Calls);
    }

    [Theory]
    [MemberData(nameof(SingleStageOutcomes))]
    [RequirementCoverage("REQ-VSB-INITIALIZER-PROVIDER-LIFETIME", "derived-runtime-skips-write-but-owns-original-provider")]
    public async Task PropertyAssignment_PreservesRuntimeTypeOwnershipAfterHeldResolutionAsync(int outcome, bool cancelCaller)
    {
        using var caller = new CancellationTokenSource();
        using var providerCancellation = new CancellationTokenSource();
        providerCancellation.Cancel();
        var original = NewSource<Guid?>();
        var provider = new DelegateProvider<Guid?>(() => original.Task);
        var before = Guid.NewGuid();
        var message = new DerivedMessage { Value = before };
        InitializeContext<Message, Input> context = new BaseInitializeContext(TestContext.Current.CancellationToken)
            .CreateMessageContext<Message>(message).CreateInputContext(new Input());
        var initializer = new ProviderPropertyInitializer<Message, Input, Guid?>(provider,
            typeof(Message).GetProperty(nameof(Message.Value))!);
        var failure = new InvalidOperationException("derived message provider failed");
        Task root = initializer.ApplyAsync(context, caller.Token);
        try
        {
            if (cancelCaller)
                caller.Cancel();
            await AssertPendingAsync(root);
            Assert.Equal(1, provider.Calls);
            Assert.Same(context, provider.Context);
            Assert.Equal(caller.Token, provider.Token);
            Complete(original, outcome, Guid.NewGuid(), failure, providerCancellation.Token);
            await AssertOutcomeAsync(root, outcome, failure, providerCancellation.Token);
            Assert.Equal(before, message.Value);
        }
        finally
        {
            original.TrySetResult(before);
            await ObserveAsync(original.Task);
            await ObserveAsync(root);
        }
    }

    static TheoryData<int, int, bool> CreateOutcomes(int forms, int outcomes)
    {
        var data = new TheoryData<int, int, bool>();
        for (int form = 0; form < forms; form++)
            for (int outcome = 0; outcome < outcomes; outcome++)
            {
                data.Add(form, outcome, false);
                data.Add(form, outcome, true);
            }
        return data;
    }

    static TheoryData<int, bool> CreateForms(int forms)
    {
        var data = new TheoryData<int, bool>();
        for (int form = 0; form < forms; form++)
        {
            data.Add(form, false);
            data.Add(form, true);
        }
        return data;
    }

    static TaskCompletionSource<T?> NewSource<T>() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    static InitializeContext<Message, Input> CreateContext(Message message) =>
        new BaseInitializeContext(TestContext.Current.CancellationToken).CreateMessageContext(message).CreateInputContext(new Input());

    static MessageSendContext<Message> CreateSend(Message message, Guid before)
    {
        var send = new MessageSendContext<Message>(message, TestContext.Current.CancellationToken) { CorrelationId = before };
        send.Headers.Set("value", before);
        return send;
    }

    static Task StartAssignmentAsync(int form, DelegateProvider<Guid?> provider, InitializeContext<Message, Input> context,
        MessageSendContext<Message> send, CancellationToken cancellationToken) => form switch
        {
            0 => new ProviderPropertyInitializer<Message, Input, Guid?>(provider,
                typeof(Message).GetProperty(nameof(Message.Value))!).ApplyAsync(context, cancellationToken),
            1 => new ProviderHeaderInitializer<Message, Input, Guid?>(provider,
                typeof(SendContext).GetProperty(nameof(SendContext.CorrelationId))!).ApplyAsync(context, send, cancellationToken),
            _ => new SetHeaderInitializer<Message, Input, Guid?>("value", provider).ApplyAsync(context, send, cancellationToken)
        };

    static Guid? ReadAssignment(int form, Message message, MessageSendContext<Message> send)
    {
        if (form == 0)
            return message.Value;
        if (form == 1)
            return send.CorrelationId;
        return send.Headers.TryGetHeader("value", out object? value) && value != null ? Assert.IsType<Guid>(value) : null;
    }

    static IPropertyProvider<Input, string> CreateAsyncProvider(int form, DelegateProvider<Task<string?>> source,
        DelegateConverter converter) => form == 0
            ? new AsyncPropertyProvider<Input, string>(source)
            : new AsyncPropertyProvider<Input, string, string>(source, converter);

    static void Complete<T>(TaskCompletionSource<T?> source, int outcome, T? value, Exception failure, CancellationToken providerToken)
    {
        if (outcome == 0)
            source.SetResult(value);
        else if (outcome == 1)
            source.SetException(failure);
        else
            source.SetCanceled(providerToken);
    }

    static async Task AssertPendingAsync(Task task)
    {
        await Assert.ThrowsAsync<TimeoutException>(() => task.WaitAsync(TimeSpan.FromMilliseconds(25), TestContext.Current.CancellationToken));
        Assert.False(task.IsCompleted);
    }

    static Task BoundedAsync(Task task) => task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

    static Task<T> BoundedAsync<T>(Task<T> task) => task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

    static async Task AssertOutcomeAsync(Task task, int outcome, InvalidOperationException failure, CancellationToken providerToken)
    {
        if (outcome == 0)
        {
            await BoundedAsync(task);
            Assert.True(task.IsCompletedSuccessfully);
        }
        else if (outcome == 1)
        {
            Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => BoundedAsync(task)));
            Assert.True(task.IsFaulted);
        }
        else
        {
            var canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => BoundedAsync(task));
            Assert.Equal(providerToken, canceled.CancellationToken);
            Assert.True(task.IsCanceled);
        }
    }

    static async Task ObserveAsync(Task task)
    {
        try
        {
            await BoundedAsync(task);
        }
        catch (Exception) when (task.IsCompleted)
        {
        }
        Assert.True(task.IsCompleted);
    }

    sealed class DelegateProvider<TProperty>(Func<Task<TProperty?>> getValue) : IPropertyProvider<Input, TProperty>
    {
        public int Calls { get; private set; }
        public object? Context { get; private set; }
        public CancellationToken Token { get; private set; }

        public Task<TProperty?> GetPropertyAsync<T>(InitializeContext<T, Input> context, CancellationToken cancellationToken = default)
            where T : class
        {
            Calls++;
            Context = context;
            Token = cancellationToken;
            return getValue();
        }
    }

    sealed class DelegateConverter(Func<Task<string?>> convert) : IPropertyConverter<string, string>
    {
        public int Calls { get; private set; }
        public object? Context { get; private set; }
        public string? Input { get; private set; }
        public CancellationToken Token { get; private set; }
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<string?> ConvertAsync<T>(InitializeContext<T> context, string? input, CancellationToken cancellationToken = default)
            where T : class
        {
            Calls++;
            Context = context;
            Input = input;
            Token = cancellationToken;
            Started.TrySetResult();
            return convert();
        }
    }

    class NoInputContext : DispatchProxy
    {
        public int Reads { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name != "get_HasInput")
                throw new InvalidOperationException("Only input availability may be inspected.");
            Reads++;
            return false;
        }
    }

    sealed class Input;
    class Message
    {
        public Guid? Value { get; set; }
    }
    sealed class DerivedMessage : Message;
}

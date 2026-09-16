using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using ViciOne.ServiceBus.Internals.Dispatching;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Internals.Dispatching;

public sealed class EndpointDispatcherTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RUNTIME-DISPATCH", "internal-static-runtime-dispatchers")]
    public void RuntimeDispatchers_AreInternalStaticImplementationTypes()
    {
        AssertInternalStatic(typeof(SendEndpointDispatcher));
        AssertInternalStatic(typeof(PublishEndpointDispatcher));
        AssertInternalStatic(typeof(ResponseEndpointDispatcher));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-RUNTIME-DISPATCH", "collectible-contract-caches-do-not-pin")]
    public void RuntimeDispatcherCaches_DoNotRetainCollectibleContractTypes(int dispatcher)
    {
        CollectibleReferences references = PopulateRuntimeDispatcherCache(dispatcher);

        CollectUntilReleased(references);

        Assert.False(references.Type.IsAlive);
        Assert.False(references.Assembly.IsAlive);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RUNTIME-DISPATCH", "send-overloads-forward-exact-contract-arguments-and-task")]
    public async Task SendDispatcher_ForwardsEveryOverloadAndReturnedTaskExactlyAsync()
    {
        ISendEndpoint endpoint = CreateProxy<IAdvancedSendEndpoint>(out RecordingProxy recorder);
        var message = new DerivedMessage();
        object values = new { Value = "initialized" };
        IPipe<SendContext> pipe = CreateProxy<IPipe<SendContext>>(out _);
        using var cancellationSource = new CancellationTokenSource();

        await AssertDispatchAsync(
            recorder,
            nameof(ISendEndpoint.SendAsync),
            typeof(TestMessage),
            [message, cancellationSource.Token],
            () => SendEndpointDispatcher.SendAsync(endpoint, message, typeof(TestMessage), cancellationSource.Token));
        await AssertDispatchAsync(
            recorder,
            nameof(ISendEndpoint.SendAsync),
            typeof(TestMessage),
            [message, pipe, cancellationSource.Token],
            () => SendEndpointDispatcher.SendAsync(endpoint, message, typeof(TestMessage), pipe, cancellationSource.Token));
        await AssertDispatchAsync(
            recorder,
            nameof(ISendEndpoint.SendAsync),
            typeof(TestMessage),
            [values, cancellationSource.Token],
            () => SendEndpointDispatcher.SendInitializerAsync(endpoint, typeof(TestMessage), values, cancellationSource.Token));
        await AssertDispatchAsync(
            recorder,
            nameof(ISendEndpoint.SendAsync),
            typeof(TestMessage),
            [values, pipe, cancellationSource.Token],
            () => SendEndpointDispatcher.SendInitializerAsync(endpoint, typeof(TestMessage), values, pipe, cancellationSource.Token));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RUNTIME-DISPATCH", "publish-overloads-forward-exact-contract-arguments-and-task")]
    public async Task PublishDispatcher_ForwardsEveryOverloadAndReturnedTaskExactlyAsync()
    {
        IPublishEndpoint endpoint = CreateProxy<IAdvancedPublishEndpoint>(out RecordingProxy recorder);
        var message = new DerivedMessage();
        object values = new { Value = "initialized" };
        IPipe<PublishContext> pipe = CreateProxy<IPipe<PublishContext>>(out _);
        using var cancellationSource = new CancellationTokenSource();

        await AssertDispatchAsync(
            recorder,
            nameof(IPublishEndpoint.PublishAsync),
            typeof(TestMessage),
            [message, cancellationSource.Token],
            () => PublishEndpointDispatcher.PublishAsync(endpoint, message, typeof(TestMessage), cancellationSource.Token));
        await AssertDispatchAsync(
            recorder,
            nameof(IPublishEndpoint.PublishAsync),
            typeof(TestMessage),
            [message, pipe, cancellationSource.Token],
            () => PublishEndpointDispatcher.PublishAsync(endpoint, message, typeof(TestMessage), pipe, cancellationSource.Token));
        await AssertDispatchAsync(
            recorder,
            nameof(IPublishEndpoint.PublishAsync),
            typeof(TestMessage),
            [values, cancellationSource.Token],
            () => PublishEndpointDispatcher.PublishInitializerAsync(endpoint, typeof(TestMessage), values, cancellationSource.Token));
        await AssertDispatchAsync(
            recorder,
            nameof(IPublishEndpoint.PublishAsync),
            typeof(TestMessage),
            [values, pipe, cancellationSource.Token],
            () => PublishEndpointDispatcher.PublishInitializerAsync(endpoint, typeof(TestMessage), values, pipe, cancellationSource.Token));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RUNTIME-DISPATCH", "response-overloads-forward-exact-contract-arguments-and-task")]
    public async Task ResponseDispatcher_ForwardsEveryOverloadAndReturnedTaskExactlyAsync()
    {
        ConsumeContext context = CreateProxy<ConsumeContext>(out RecordingProxy recorder);
        var message = new DerivedMessage();
        IPipe<SendContext> pipe = CreateProxy<IPipe<SendContext>>(out _);

        await AssertDispatchAsync(
            recorder,
            nameof(ConsumeContext.RespondAsync),
            typeof(TestMessage),
            [message],
            () => ResponseEndpointDispatcher.RespondAsync(context, message, typeof(TestMessage)));
        await AssertDispatchAsync(
            recorder,
            nameof(ConsumeContext.RespondAsync),
            typeof(TestMessage),
            [message, pipe],
            () => ResponseEndpointDispatcher.RespondAsync(context, message, typeof(TestMessage), pipe));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RUNTIME-DISPATCH", "every-required-parameter-fails-before-dispatch")]
    public void RuntimeDispatchers_RejectEveryMissingInputBeforeInvocation()
    {
        ISendEndpoint sendEndpoint = CreateProxy<IAdvancedSendEndpoint>(out RecordingProxy sendRecorder);
        IPublishEndpoint publishEndpoint = CreateProxy<IAdvancedPublishEndpoint>(out RecordingProxy publishRecorder);
        ConsumeContext consumeContext = CreateProxy<ConsumeContext>(out RecordingProxy responseRecorder);
        var message = new TestMessage();
        object values = new { Value = "initialized" };
        CancellationToken token = TestContext.Current.CancellationToken;

        AssertParameter("endpoint", () => SendEndpointDispatcher.SendAsync(null!, message, typeof(TestMessage), token));
        AssertParameter("message", () => SendEndpointDispatcher.SendAsync(sendEndpoint, null!, typeof(TestMessage), token));
        AssertParameter("messageType", () => SendEndpointDispatcher.SendAsync(sendEndpoint, message, null!, token));
        AssertParameter("pipe", () => SendEndpointDispatcher.SendAsync(sendEndpoint, message, typeof(TestMessage), null!, token));
        AssertParameter("endpoint", () => SendEndpointDispatcher.SendInitializerAsync(null!, typeof(TestMessage), values, token));
        AssertParameter("values", () => SendEndpointDispatcher.SendInitializerAsync(sendEndpoint, typeof(TestMessage), null!, token));
        AssertParameter("messageType", () => SendEndpointDispatcher.SendInitializerAsync(sendEndpoint, null!, values, token));
        AssertParameter("pipe", () => SendEndpointDispatcher.SendInitializerAsync(sendEndpoint, typeof(TestMessage), values, null!, token));

        AssertParameter("endpoint", () => PublishEndpointDispatcher.PublishAsync(null!, message, typeof(TestMessage), token));
        AssertParameter("message", () => PublishEndpointDispatcher.PublishAsync(publishEndpoint, null!, typeof(TestMessage), token));
        AssertParameter("messageType", () => PublishEndpointDispatcher.PublishAsync(publishEndpoint, message, null!, token));
        AssertParameter("pipe", () => PublishEndpointDispatcher.PublishAsync(publishEndpoint, message, typeof(TestMessage), null!, token));
        AssertParameter("endpoint", () => PublishEndpointDispatcher.PublishInitializerAsync(null!, typeof(TestMessage), values, token));
        AssertParameter("values", () => PublishEndpointDispatcher.PublishInitializerAsync(publishEndpoint, typeof(TestMessage), null!, token));
        AssertParameter("messageType", () => PublishEndpointDispatcher.PublishInitializerAsync(publishEndpoint, null!, values, token));
        AssertParameter("pipe", () => PublishEndpointDispatcher.PublishInitializerAsync(publishEndpoint, typeof(TestMessage), values, null!, token));

        AssertParameter("consumeContext", () => ResponseEndpointDispatcher.RespondAsync(null!, message, typeof(TestMessage)));
        AssertParameter("message", () => ResponseEndpointDispatcher.RespondAsync(consumeContext, null!, typeof(TestMessage)));
        AssertParameter("messageType", () => ResponseEndpointDispatcher.RespondAsync(consumeContext, message, null!));
        AssertParameter("pipe", () => ResponseEndpointDispatcher.RespondAsync(consumeContext, message, typeof(TestMessage), null!));

        Assert.Empty(sendRecorder.Invocations);
        Assert.Empty(publishRecorder.Invocations);
        Assert.Empty(responseRecorder.Invocations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RUNTIME-DISPATCH", "invalid-contract-shapes-fail-before-dispatch")]
    public void RuntimeDispatchers_RejectEveryUnsupportedContractShapeBeforeInvocation()
    {
        ISendEndpoint sendEndpoint = CreateProxy<IAdvancedSendEndpoint>(out RecordingProxy sendRecorder);
        IPublishEndpoint publishEndpoint = CreateProxy<IAdvancedPublishEndpoint>(out RecordingProxy publishRecorder);
        ConsumeContext consumeContext = CreateProxy<ConsumeContext>(out RecordingProxy responseRecorder);
        var message = new TestMessage();
        CancellationToken token = TestContext.Current.CancellationToken;
        Type[] invalidTypes =
        [
            typeof(int),
            typeof(List<>),
            typeof(TestMessage).MakeByRefType(),
            typeof(int).MakePointerType(),
        ];

        foreach (Type invalidType in invalidTypes)
        {
            AssertContractType(() => SendEndpointDispatcher.SendAsync(sendEndpoint, message, invalidType, token));
            AssertContractType(() => PublishEndpointDispatcher.PublishAsync(publishEndpoint, message, invalidType, token));
            AssertContractType(() => ResponseEndpointDispatcher.RespondAsync(consumeContext, message, invalidType));
        }

        Assert.Empty(sendRecorder.Invocations);
        Assert.Empty(publishRecorder.Invocations);
        Assert.Empty(responseRecorder.Invocations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RUNTIME-DISPATCH", "incompatible-messages-fail-before-dispatch")]
    public void RuntimeDispatchers_RejectIncompatibleMessagesForBothPipePartitions()
    {
        ISendEndpoint sendEndpoint = CreateProxy<IAdvancedSendEndpoint>(out RecordingProxy sendRecorder);
        IPublishEndpoint publishEndpoint = CreateProxy<IAdvancedPublishEndpoint>(out RecordingProxy publishRecorder);
        ConsumeContext consumeContext = CreateProxy<ConsumeContext>(out RecordingProxy responseRecorder);
        IPipe<SendContext> sendPipe = CreateProxy<IPipe<SendContext>>(out _);
        IPipe<PublishContext> publishPipe = CreateProxy<IPipe<PublishContext>>(out _);
        var incompatible = new OtherMessage();
        CancellationToken token = TestContext.Current.CancellationToken;

        AssertMessage(() => SendEndpointDispatcher.SendAsync(sendEndpoint, incompatible, typeof(TestMessage), token));
        AssertMessage(() => SendEndpointDispatcher.SendAsync(sendEndpoint, incompatible, typeof(TestMessage), sendPipe, token));
        AssertMessage(() => PublishEndpointDispatcher.PublishAsync(publishEndpoint, incompatible, typeof(TestMessage), token));
        AssertMessage(() => PublishEndpointDispatcher.PublishAsync(publishEndpoint, incompatible, typeof(TestMessage), publishPipe, token));
        AssertMessage(() => ResponseEndpointDispatcher.RespondAsync(consumeContext, incompatible, typeof(TestMessage)));
        AssertMessage(() => ResponseEndpointDispatcher.RespondAsync(consumeContext, incompatible, typeof(TestMessage), sendPipe));

        Assert.Empty(sendRecorder.Invocations);
        Assert.Empty(publishRecorder.Invocations);
        Assert.Empty(responseRecorder.Invocations);
    }

    private static async Task AssertDispatchAsync(
        RecordingProxy recorder,
        string methodName,
        Type contractType,
        object?[] expectedArguments,
        Func<Task> dispatch)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        recorder.ReturnTask = completion.Task;

        Task returned = dispatch();

        Assert.Same(completion.Task, returned);
        Invocation invocation = Assert.Single(recorder.Invocations);
        Assert.Equal(methodName, invocation.Method.Name);
        Assert.Equal(contractType, Assert.Single(invocation.Method.GetGenericArguments()));
        Assert.Equal(expectedArguments.Length, invocation.Arguments.Length);
        for (var index = 0; index < expectedArguments.Length; index++)
        {
            if (expectedArguments[index] is CancellationToken expectedToken)
                Assert.Equal(expectedToken, Assert.IsType<CancellationToken>(invocation.Arguments[index]));
            else
                Assert.Same(expectedArguments[index], invocation.Arguments[index]);
        }

        completion.SetResult();
        await returned;
        recorder.Invocations.Clear();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static CollectibleReferences PopulateRuntimeDispatcherCache(int dispatcher)
    {
        var name = new AssemblyName($"ViciOne.RuntimeDispatcher.{dispatcher}.{Guid.NewGuid():N}");
        AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.RunAndCollect);
        ModuleBuilder module = assembly.DefineDynamicModule(name.Name!);
        TypeBuilder builder = module.DefineType("RuntimeContract", TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed);
        builder.DefineDefaultConstructor(MethodAttributes.Public);
        Type runtimeType = builder.CreateType()!;
        object message = Activator.CreateInstance(runtimeType)!;

        RecordingProxy recorder;
        Task returned;
        switch (dispatcher)
        {
            case 0:
                ISendEndpoint sendEndpoint = CreateProxy<IAdvancedSendEndpoint>(out recorder);
                returned = SendEndpointDispatcher.SendAsync(sendEndpoint, message, runtimeType, CancellationToken.None);
                break;
            case 1:
                IPublishEndpoint publishEndpoint = CreateProxy<IAdvancedPublishEndpoint>(out recorder);
                returned = PublishEndpointDispatcher.PublishAsync(publishEndpoint, message, runtimeType, CancellationToken.None);
                break;
            case 2:
                ConsumeContext consumeContext = CreateProxy<ConsumeContext>(out recorder);
                returned = ResponseEndpointDispatcher.RespondAsync(consumeContext, message, runtimeType);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(dispatcher));
        }

        Assert.Same(recorder.ReturnTask, returned);
        Assert.Single(recorder.Invocations);
        recorder.Invocations.Clear();
        return new CollectibleReferences(new WeakReference(runtimeType), new WeakReference(assembly));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CollectUntilReleased(CollectibleReferences references)
    {
        for (var attempt = 0; attempt < 20 && (references.Type.IsAlive || references.Assembly.IsAlive); attempt++)
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        }
    }

    private static void AssertParameter(string parameterName, Func<Task> operation) =>
        Assert.Equal(parameterName, Assert.Throws<ArgumentNullException>(() =>
        {
            _ = operation();
        }).ParamName);

    private static void AssertContractType(Func<Task> operation) =>
        Assert.Equal("messageType", Assert.Throws<ArgumentException>(() =>
        {
            _ = operation();
        }).ParamName);

    private static void AssertMessage(Func<Task> operation) =>
        Assert.Equal("message", Assert.Throws<ArgumentException>(() =>
        {
            _ = operation();
        }).ParamName);

    private static TContract CreateProxy<TContract>(out RecordingProxy recorder)
        where TContract : class
    {
        TContract contract = DispatchProxy.Create<TContract, RecordingProxy>();
        recorder = (RecordingProxy)(object)contract;
        return contract;
    }

    private static void AssertInternalStatic(Type type)
    {
        Assert.False(type.IsPublic);
        Assert.True(type.IsAbstract);
        Assert.True(type.IsSealed);
        Assert.Empty(type.GetConstructors());
    }

    private sealed record Invocation(MethodInfo Method, object?[] Arguments);

    private sealed record CollectibleReferences(WeakReference Type, WeakReference Assembly);

    private class RecordingProxy : DispatchProxy
    {
        public List<Invocation> Invocations { get; } = [];

        public Task ReturnTask { get; set; } = Task.CompletedTask;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            Invocations.Add(new Invocation(targetMethod, args ?? []));

            if (targetMethod.ReturnType == typeof(Task))
                return ReturnTask;

            throw new NotSupportedException(targetMethod.Name);
        }
    }

    private record TestMessage;

    private sealed record DerivedMessage : TestMessage;

    private sealed record OtherMessage;
}

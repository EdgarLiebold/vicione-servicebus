using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.Contexts;
using ViciOne.ServiceBus.Initializers.Conventions;
using ViciOne.ServiceBus.Initializers.PropertyProviders;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers.PropertyProviders;

public sealed class PropertyProviderOwnershipTests
{
    public static TheoryData<int, int, bool> TwoStageOutcomes => CreateOutcomes(forms: 2);
    public static TheoryData<int, int, bool> NullableOutcomes => CreateOutcomes(forms: 2);

    [Theory]
    [MemberData(nameof(TwoStageOutcomes))]
    [RequirementCoverage("REQ-VSB-INITIALIZER-PROVIDER-LIFETIME", "converter-provider-owns-each-accepted-stage")]
    public async Task PropertyConverterProvider_OwnsEachAcceptedStageAndOriginalOutcomeAsync(
        int stage, int outcome, bool cancelCaller)
    {
        using var caller = new CancellationTokenSource();
        using var producerCancellation = new CancellationTokenSource();
        producerCancellation.Cancel();
        var source = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var conversion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sourceProvider = new RecordingProvider<string?>(() => stage == 0
            ? source.Task
            : Task.FromResult<string?>("source"));
        var converter = new RecordingConverter<int, string?>(value => stage == 1
            ? conversion.Task
            : Task.FromResult(value?.Length ?? 0));
        var provider = new PropertyConverterPropertyProvider<TestInput, int, string?>(converter, sourceProvider);
        InitializeContext<TestMessage, TestInput> context = CreateContext();
        var failure = new InvalidOperationException("accepted property conversion failed");
        Task<int> root = provider.GetPropertyAsync(context, caller.Token);

        try
        {
            Assert.Same(context, sourceProvider.Context);
            Assert.Equal(caller.Token, sourceProvider.Token);
            Assert.Equal(1, sourceProvider.Calls);
            Assert.Equal(stage == 1 ? 1 : 0, converter.Calls);
            if (cancelCaller)
                caller.Cancel();

            await AssertPendingAsync(root);
            if (stage == 0)
                Complete(source, outcome, "source", failure, producerCancellation.Token);
            else
                Complete(conversion, outcome, 6, failure, producerCancellation.Token);

            await AssertOutcomeAsync(root, outcome, failure, producerCancellation.Token);
            if (outcome == 0)
                Assert.Equal(6, await root);

            bool converterStarted = stage == 1 || outcome == 0;
            Assert.Equal(converterStarted ? 1 : 0, converter.Calls);
            if (converterStarted)
            {
                Assert.Same(context, converter.Context);
                Assert.Equal("source", converter.Input);
                Assert.Equal(caller.Token, converter.Token);
            }
        }
        finally
        {
            source.TrySetResult("cleanup");
            conversion.TrySetResult(0);
            await ObserveAsync(source.Task);
            await ObserveAsync(conversion.Task);
            await ObserveAsync(root);
        }
    }

    [Theory]
    [MemberData(nameof(TwoStageOutcomes))]
    [RequirementCoverage("REQ-VSB-INITIALIZER-PROVIDER-LIFETIME", "variable-provider-owns-each-accepted-stage")]
    public async Task VariableProvider_OwnsEachAcceptedStageAndOriginalOutcomeAsync(
        int stage, int outcome, bool cancelCaller)
    {
        using var caller = new CancellationTokenSource();
        using var producerCancellation = new CancellationTokenSource();
        producerCancellation.Cancel();
        var variableValue = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var variable = new RecordingVariable(() => stage == 1 ? variableValue.Task : Task.FromResult(27));
        var variableSource = new TaskCompletionSource<RecordingVariable?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sourceProvider = new RecordingProvider<RecordingVariable>(() => stage == 0
            ? variableSource.Task
            : Task.FromResult<RecordingVariable?>(variable));
        var provider = new VariablePropertyProvider<TestInput, RecordingVariable, int>(sourceProvider);
        InitializeContext<TestMessage, TestInput> context = CreateContext();
        var failure = new InvalidOperationException("accepted initializer variable failed");
        Task<int> root = provider.GetPropertyAsync(context, caller.Token);

        try
        {
            Assert.Same(context, sourceProvider.Context);
            Assert.Equal(caller.Token, sourceProvider.Token);
            Assert.Equal(1, sourceProvider.Calls);
            Assert.Equal(stage == 1 ? 1 : 0, variable.Calls);
            if (cancelCaller)
                caller.Cancel();

            await AssertPendingAsync(root);
            if (stage == 0)
                Complete(variableSource, outcome, variable, failure, producerCancellation.Token);
            else
                Complete(variableValue, outcome, 27, failure, producerCancellation.Token);

            await AssertOutcomeAsync(root, outcome, failure, producerCancellation.Token);
            if (outcome == 0)
                Assert.Equal(27, await root);

            bool variableStarted = stage == 1 || outcome == 0;
            Assert.Equal(variableStarted ? 1 : 0, variable.Calls);
            if (variableStarted)
            {
                Assert.Same(context, variable.Context);
                Assert.Equal(caller.Token, variable.Token);
            }
        }
        finally
        {
            variableSource.TrySetResult(variable);
            variableValue.TrySetResult(0);
            await ObserveAsync(variableSource.Task);
            await ObserveAsync(variableValue.Task);
            await ObserveAsync(root);
        }
    }

    [Theory]
    [MemberData(nameof(TwoStageOutcomes))]
    [RequirementCoverage("REQ-VSB-INITIALIZER-PROVIDER-LIFETIME", "object-provider-owns-each-accepted-stage")]
    public async Task ObjectProvider_OwnsEachAcceptedStageAndOriginalOutcomeAsync(
        int stage, int outcome, bool cancelCaller)
    {
        using var caller = new CancellationTokenSource();
        using var producerCancellation = new CancellationTokenSource();
        producerCancellation.Cancel();
        var source = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var conversion = new TaskCompletionSource<Payload?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sourceProvider = new RecordingProvider<object>(() => stage == 0
            ? source.Task
            : Task.FromResult<object?>("source"));
        var factory = new RecordingRuntimeFactory(value => stage == 1
            ? conversion.Task
            : Task.FromResult<Payload?>(new Payload(value)));
        var provider = new ObjectPropertyProvider<TestInput, Payload>(factory, sourceProvider);
        InitializeContext<TestMessage, TestInput> context = CreateContext();
        var expected = new Payload("source");
        var failure = new InvalidOperationException("accepted runtime conversion failed");
        Task<Payload?> root = provider.GetPropertyAsync(context, caller.Token);

        try
        {
            Assert.Same(context, sourceProvider.Context);
            Assert.Equal(caller.Token, sourceProvider.Token);
            Assert.Equal(1, sourceProvider.Calls);
            Assert.Equal(stage == 1 ? 1 : 0, factory.Requests);
            if (cancelCaller)
                caller.Cancel();

            await AssertPendingAsync(root);
            if (stage == 0)
                Complete(source, outcome, "source", failure, producerCancellation.Token);
            else
                Complete(conversion, outcome, expected, failure, producerCancellation.Token);

            await AssertOutcomeAsync(root, outcome, failure, producerCancellation.Token);
            if (outcome == 0)
                Assert.Equal(expected, await root);

            bool converterStarted = stage == 1 || outcome == 0;
            Assert.Equal(converterStarted ? 1 : 0, factory.Requests);
            if (converterStarted)
            {
                Assert.Same(context, factory.Context);
                Assert.Equal("source", factory.Input);
                Assert.Equal(caller.Token, factory.Token);
            }
        }
        finally
        {
            source.TrySetResult("cleanup");
            conversion.TrySetResult(expected);
            await ObserveAsync(source.Task);
            await ObserveAsync(conversion.Task);
            await ObserveAsync(root);
        }
    }

    [Theory]
    [MemberData(nameof(NullableOutcomes))]
    [RequirementCoverage("REQ-VSB-INITIALIZER-PROVIDER-LIFETIME", "nullable-providers-own-accepted-outcomes")]
    public async Task NullableProviders_OwnAcceptedProviderAndOriginalOutcomeAsync(
        int form, int outcome, bool cancelCaller)
    {
        using var caller = new CancellationTokenSource();
        using var producerCancellation = new CancellationTokenSource();
        producerCancellation.Cancel();
        var nullableSource = new TaskCompletionSource<int?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var valueSource = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var nullableProvider = new RecordingProvider<int?>(() => nullableSource.Task);
        var valueProvider = new RecordingProvider<int>(() => valueSource.Task);
        InitializeContext<TestMessage, TestInput> context = CreateContext();
        var failure = new InvalidOperationException("accepted nullable provider failed");
        Task<int>? fromNullable = null;
        Task<int?>? toNullable = null;
        Task root;
        if (form == 0)
        {
            fromNullable = new FromNullablePropertyProvider<TestInput, int>(nullableProvider)
                .GetPropertyAsync(context, caller.Token);
            root = fromNullable;
        }
        else
        {
            toNullable = new ToNullablePropertyProvider<TestInput, int>(valueProvider)
                .GetPropertyAsync(context, caller.Token);
            root = toNullable;
        }

        try
        {
            RecordingProviderBase recording = form == 0 ? nullableProvider : valueProvider;
            Assert.Same(context, recording.Context);
            Assert.Equal(caller.Token, recording.Token);
            Assert.Equal(1, recording.Calls);
            if (cancelCaller)
                caller.Cancel();

            await AssertPendingAsync(root);
            if (form == 0)
                Complete(nullableSource, outcome, 31, failure, producerCancellation.Token);
            else
                Complete(valueSource, outcome, 32, failure, producerCancellation.Token);

            await AssertOutcomeAsync(root, outcome, failure, producerCancellation.Token);
            if (outcome == 0)
            {
                if (form == 0)
                    Assert.Equal(31, await fromNullable!);
                else
                    Assert.Equal(32, await toNullable!);
            }
        }
        finally
        {
            nullableSource.TrySetResult(0);
            valueSource.TrySetResult(0);
            await ObserveAsync(nullableSource.Task);
            await ObserveAsync(valueSource.Task);
            await ObserveAsync(root);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-RUNTIME-IDENTITY", "dictionary-object-preserves-assignable-runtime-instances")]
    public async Task DictionaryObjectValues_PreserveAssignableRuntimeInstancesAsync()
    {
        int[] array = [1, 2, 3];
        var collection = new CustomCollection { 4, 5, 6 };
        var payload = new Payload("payload");
        IDictionary<string, object> values = new Dictionary<string, object>
        {
            [nameof(IRuntimeIdentityMessage.Array)] = array,
            [nameof(IRuntimeIdentityMessage.Collection)] = collection,
            [nameof(IRuntimeIdentityMessage.Payload)] = payload,
        };

        InitializeContext<IRuntimeIdentityMessage> context = await MessageInitializerCache<IRuntimeIdentityMessage>
            .InitializeAsync(values, TestContext.Current.CancellationToken);

        Assert.Same(array, context.Message.Array);
        Assert.Same(collection, context.Message.Collection);
        Assert.Same(payload, context.Message.Payload);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-CONVENTIONS", "dictionary-object-unsupported-struct-clean-rejection")]
    public void DictionaryObjectFallback_RejectsUnsupportedStructWithoutActivationFailure()
    {
        var convention = new DictionaryInitializerConvention<StructMessage, Dictionary<string, object>, object>();
        PropertyInfo property = typeof(StructMessage).GetProperty(nameof(StructMessage.Value))!;

        bool found = convention.TryGetPropertyInitializer<CustomStruct>(property, out var initializer);

        Assert.False(found);
        Assert.Null(initializer);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-PROVIDER-RESOURCES", "runtime-converter-cache-does-not-retain-collectible-types")]
    public void ObjectProvider_DoesNotRetainCollectibleRuntimeTypes()
    {
        CacheProbe probe = PopulateRuntimeConverterCache();

        CollectUntilReleased(probe.RuntimeType, probe.Assembly);

        Assert.False(probe.RuntimeType.IsAlive);
        Assert.False(probe.Assembly.IsAlive);
        GC.KeepAlive(probe.Provider);
    }

    static TheoryData<int, int, bool> CreateOutcomes(int forms)
    {
        var data = new TheoryData<int, int, bool>();
        for (int form = 0; form < forms; form++)
            for (int outcome = 0; outcome < 3; outcome++)
            {
                data.Add(form, outcome, false);
                data.Add(form, outcome, true);
            }

        return data;
    }

    static InitializeContext<TestMessage, TestInput> CreateContext() =>
        new BaseInitializeContext(TestContext.Current.CancellationToken)
            .CreateMessageContext(new TestMessage())
            .CreateInputContext(new TestInput());

    static void Complete<T>(TaskCompletionSource<T> source, int outcome, T value,
        InvalidOperationException failure, CancellationToken producerToken)
    {
        if (outcome == 0)
            source.SetResult(value);
        else if (outcome == 1)
            source.SetException(failure);
        else
            source.SetCanceled(producerToken);
    }

    static async Task AssertPendingAsync(Task task)
    {
        await Assert.ThrowsAsync<TimeoutException>(() =>
            task.WaitAsync(TimeSpan.FromMilliseconds(25), TestContext.Current.CancellationToken));
        Assert.False(task.IsCompleted);
    }

    static async Task AssertOutcomeAsync(Task task, int outcome,
        InvalidOperationException failure, CancellationToken producerToken)
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
            OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => BoundedAsync(task));
            Assert.Equal(producerToken, canceled.CancellationToken);
            Assert.True(task.IsCanceled);
        }
    }

    static Task BoundedAsync(Task task) =>
        task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

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

    [MethodImpl(MethodImplOptions.NoInlining)]
    static CacheProbe PopulateRuntimeConverterCache()
    {
        var assemblyName = new AssemblyName($"ViciOne.Collectible.{Guid.NewGuid():N}");
        AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
        ModuleBuilder module = assembly.DefineDynamicModule(assemblyName.Name!);
        TypeBuilder builder = module.DefineType("RuntimeValue", TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed);
        builder.DefineDefaultConstructor(MethodAttributes.Public);
        Type runtimeType = builder.CreateType()!;
        object runtimeValue = Activator.CreateInstance(runtimeType)!;
        var source = new OneShotObjectProvider(runtimeValue);
        var factory = new UnsupportedRuntimeFactory();
        var provider = new ObjectPropertyProvider<TestInput, Payload>(factory, source);

        Payload? result = provider.GetPropertyAsync(CreateContext(), TestContext.Current.CancellationToken)
            .GetAwaiter().GetResult();

        Assert.Null(result);
        Assert.Equal(1, factory.Requests);
        Assert.Null(source.Value);
        return new CacheProbe(provider, new WeakReference(runtimeType), new WeakReference(assembly));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CollectUntilReleased(params WeakReference[] references)
    {
        for (int attempt = 0; attempt < 20 && references.Any(reference => reference.IsAlive); attempt++)
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        }
    }

    abstract class RecordingProviderBase
    {
        public int Calls { get; protected set; }
        public object? Context { get; protected set; }
        public CancellationToken Token { get; protected set; }
    }

    sealed class RecordingProvider<TProperty>(Func<Task<TProperty?>> getValue) :
        RecordingProviderBase, IPropertyProvider<TestInput, TProperty>
    {
        public Task<TProperty?> GetPropertyAsync<T>(InitializeContext<T, TestInput> context,
            CancellationToken cancellationToken = default)
            where T : class
        {
            Calls++;
            Context = context;
            Token = cancellationToken;
            return getValue();
        }
    }

    sealed class RecordingConverter<TResult, TInput>(Func<TInput?, Task<TResult?>> convert) :
        IPropertyConverter<TResult, TInput>
    {
        public int Calls { get; private set; }
        public object? Context { get; private set; }
        public TInput? Input { get; private set; }
        public CancellationToken Token { get; private set; }

        public Task<TResult?> ConvertAsync<T>(InitializeContext<T> context, TInput? input,
            CancellationToken cancellationToken = default)
            where T : class
        {
            Calls++;
            Context = context;
            Input = input;
            Token = cancellationToken;
            return convert(input);
        }
    }

    sealed class RecordingVariable(Func<Task<int>> getValue) : IInitializerVariable<int>
    {
        public int Calls { get; private set; }
        public object? Context { get; private set; }
        public CancellationToken Token { get; private set; }

        public Task<int> GetValueAsync<T>(InitializeContext<T> context,
            CancellationToken cancellationToken = default)
            where T : class
        {
            Calls++;
            Context = context;
            Token = cancellationToken;
            return getValue();
        }
    }

    sealed class RecordingRuntimeFactory(Func<string, Task<Payload?>> convert) : IPropertyProviderFactory<TestInput>
    {
        public int Requests { get; private set; }
        public object? Context { get; private set; }
        public object? Input { get; private set; }
        public CancellationToken Token { get; private set; }

        public bool TryGetPropertyProvider<TResult>(PropertyInfo propertyInfo,
            [NotNullWhen(true)] out IPropertyProvider<TestInput, TResult>? provider)
        {
            provider = null;
            return false;
        }

        public bool TryGetPropertyConverter<TResult, TProperty>(
            [NotNullWhen(true)] out IPropertyConverter<TResult, TProperty>? converter)
        {
            Requests++;
            if (typeof(TResult) == typeof(Payload) && typeof(TProperty) == typeof(string))
            {
                converter = (IPropertyConverter<TResult, TProperty>)(object)new RuntimeConverter(this, convert);
                return true;
            }

            converter = null;
            return false;
        }

        sealed class RuntimeConverter(RecordingRuntimeFactory owner, Func<string, Task<Payload?>> convert) :
            IPropertyConverter<Payload, string>
        {
            public Task<Payload?> ConvertAsync<T>(InitializeContext<T> context, string? input,
                CancellationToken cancellationToken = default)
                where T : class
            {
                owner.Context = context;
                owner.Input = input;
                owner.Token = cancellationToken;
                return convert(input!);
            }
        }
    }

    sealed class UnsupportedRuntimeFactory : IPropertyProviderFactory<TestInput>
    {
        public int Requests { get; private set; }

        public bool TryGetPropertyProvider<TResult>(PropertyInfo propertyInfo,
            [NotNullWhen(true)] out IPropertyProvider<TestInput, TResult>? provider)
        {
            provider = null;
            return false;
        }

        public bool TryGetPropertyConverter<TResult, TProperty>(
            [NotNullWhen(true)] out IPropertyConverter<TResult, TProperty>? converter)
        {
            Requests++;
            converter = null;
            return false;
        }
    }

    sealed class OneShotObjectProvider(object value) : IPropertyProvider<TestInput, object>
    {
        object? _value = value;

        public object? Value => _value;

        public Task<object?> GetPropertyAsync<T>(InitializeContext<T, TestInput> context,
            CancellationToken cancellationToken = default)
            where T : class => Task.FromResult(Interlocked.Exchange(ref _value, null));
    }

    public interface IRuntimeIdentityMessage
    {
        int[] Array { get; }
        CustomCollection Collection { get; }
        Payload Payload { get; }
    }

    public sealed class CustomCollection : List<int>;

    public sealed record Payload(string Value);

    readonly record struct CustomStruct(int Value);

    sealed class StructMessage
    {
        public CustomStruct Value { get; set; }
    }

    sealed record CacheProbe(object Provider, WeakReference RuntimeType, WeakReference Assembly);

    sealed class TestInput;
    sealed class TestMessage;
}

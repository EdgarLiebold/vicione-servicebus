using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.Contexts;
using ViciOne.ServiceBus.Initializers.PropertyProviders;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers.PropertyProviders;

public sealed class PropertyProviderStateMatrixTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-PROVIDERS", "leaf-value-absence-and-cancellation-matrix")]
    public async Task LeafProviders_CoverValueAbsenceConstructionAndPreCancellationStatesAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        InitializeContext<TestMessage, TestInput> context = CreateContext(new TestInput("value"));
        InitializeContext<TestMessage, TestInput> noInput = CreateNoInputContext<TestMessage, TestInput>();
        var constant = new ConstantPropertyProvider<TestInput, string?>("constant");
        var input = new InputPropertyProvider<TestInput, string?>(
            typeof(TestInput).GetProperty(nameof(TestInput.Value))!);
        var dictionary = new InputDictionaryPropertyProvider<Dictionary<string, string?>, string?>("Value");

        Assert.Equal("constant", await constant.GetPropertyAsync(context, token));
        Assert.Equal("value", await input.GetPropertyAsync(context, token));
        Assert.Null(await input.GetPropertyAsync(noInput, token));
        Assert.Equal("present", await dictionary.GetPropertyAsync(
            CreateContext(new Dictionary<string, string?> { ["Value"] = "present" }), token));
        Assert.Null(await dictionary.GetPropertyAsync(CreateContext(new Dictionary<string, string?>()), token));
        Assert.Null(await dictionary.GetPropertyAsync(CreateNoInputContext<TestMessage, Dictionary<string, string?>>(), token));

        Assert.Equal("propertyInfo", Assert.Throws<ArgumentNullException>(() =>
            new InputPropertyProvider<TestInput, string?>(null)).ParamName);
        Assert.Equal("key", Assert.Throws<ArgumentException>(() =>
            new InputDictionaryPropertyProvider<Dictionary<string, string?>, string?>(" ")).ParamName);
        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            constant.GetPropertyAsync<TestMessage>(null!, token))).ParamName);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await AssertCanceledAsync(() => constant.GetPropertyAsync(context, cancellation.Token), cancellation.Token);
        await AssertCanceledAsync(() => input.GetPropertyAsync(CreateThrowingContext<TestMessage, TestInput>(), cancellation.Token), cancellation.Token);
        await AssertCanceledAsync(() => dictionary.GetPropertyAsync(
            CreateThrowingContext<TestMessage, Dictionary<string, string?>>(), cancellation.Token), cancellation.Token);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-PROVIDERS", "nullable-task-and-async-state-matrix")]
    public async Task NullableTaskAndAsyncAdapters_PreserveEveryDependencyStateAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        InitializeContext<TestMessage, TestInput> context = CreateContext(new TestInput("value"));
        InitializeContext<TestMessage, TestInput> noInput = CreateNoInputContext<TestMessage, TestInput>();

        var nullableSource = new DelegateProvider<int?>(() => Task.FromResult<int?>(42));
        var fromNullable = new FromNullablePropertyProvider<TestInput, int>(nullableSource);
        Assert.Equal(42, await fromNullable.GetPropertyAsync(context, token));
        Assert.Equal(0, await new FromNullablePropertyProvider<TestInput, int>(
            new DelegateProvider<int?>(() => Task.FromResult<int?>(null))).GetPropertyAsync(context, token));
        Assert.Equal(0, await fromNullable.GetPropertyAsync(noInput, token));

        var valueSource = new DelegateProvider<int>(() => Task.FromResult(43));
        var toNullable = new ToNullablePropertyProvider<TestInput, int>(valueSource);
        Assert.Equal(43, await toNullable.GetPropertyAsync(context, token));
        Assert.Null(await toNullable.GetPropertyAsync(noInput, token));
        Assert.Equal(1, nullableSource.CallCount);
        Assert.Equal(1, valueSource.CallCount);

        var taskSource = new DelegateProvider<int>(() => Task.FromResult(44));
        var taskAdapter = new TaskPropertyProvider<TestInput, int>(taskSource);
        Task<int>? taskValue = await taskAdapter.GetPropertyAsync(context, token);
        Assert.NotNull(taskValue);
        Assert.Equal(44, await taskValue);
        Assert.Equal(1, taskSource.CallCount);
        Task<int>? absentTask = await taskAdapter.GetPropertyAsync(noInput, token);
        Assert.NotNull(absentTask);
        Assert.Equal(0, await absentTask);
        Assert.Equal(1, taskSource.CallCount);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new TaskPropertyProvider<TestInput, int>(new NullTaskProvider<int>()).GetPropertyAsync(context, token));

        IPropertyProvider<TestInput, string?> directAsync = new AsyncPropertyProvider<TestInput, string?>(
            new DelegateProvider<Task<string?>>(() => Task.FromResult<Task<string?>?>(Task.FromResult<string?>("ready"))));
        Assert.Equal("ready", await directAsync.GetPropertyAsync(context, token));
        IPropertyProvider<TestInput, string?> absentAsync = new AsyncPropertyProvider<TestInput, string?>(
            new DelegateProvider<Task<string?>>(() => Task.FromResult<Task<string?>?>(null)));
        Assert.Null(await absentAsync.GetPropertyAsync(context, token));
        Assert.Null(await directAsync.GetPropertyAsync(noInput, token));

        var convertedAsyncSource = new DelegateProvider<Task<int>>(() => Task.FromResult<Task<int>?>(Task.FromResult(45)));
        var convertedAsyncConverter = new DelegateConverter<long, int>(value => Task.FromResult(value * 10L));
        IPropertyProvider<TestInput, long> convertedAsync = new AsyncPropertyProvider<TestInput, long, int>(
            convertedAsyncSource,
            convertedAsyncConverter);
        Assert.Equal(450L, await convertedAsync.GetPropertyAsync(context, token));
        Assert.Equal(0L, await convertedAsync.GetPropertyAsync(noInput, token));
        Assert.Equal(1, convertedAsyncSource.CallCount);
        Assert.Equal(token, convertedAsyncConverter.Token);
        IPropertyProvider<TestInput, long> absentConvertedTask = new AsyncPropertyProvider<TestInput, long, int>(
            new DelegateProvider<Task<int>>(() => Task.FromResult<Task<int>?>(null)),
            convertedAsyncConverter);
        Assert.Equal(0L, await absentConvertedTask.GetPropertyAsync(context, token));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new FromNullablePropertyProvider<TestInput, int>(new NullTaskProvider<int?>()).GetPropertyAsync(context, token));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ToNullablePropertyProvider<TestInput, int>(new NullTaskProvider<int>()).GetPropertyAsync(context, token));
        IPropertyProvider<TestInput, string?> nullOuterAsync = new AsyncPropertyProvider<TestInput, string?>(
            new NullTaskProvider<Task<string?>>());
        await Assert.ThrowsAsync<InvalidOperationException>(() => nullOuterAsync.GetPropertyAsync(context, token));
        IPropertyProvider<TestInput, long> nullConversionAsync = new AsyncPropertyProvider<TestInput, long, int>(
            new DelegateProvider<Task<int>>(() => Task.FromResult<Task<int>?>(Task.FromResult(1))),
            new NullTaskConverter<long, int>());
        await Assert.ThrowsAsync<InvalidOperationException>(() => nullConversionAsync.GetPropertyAsync(context, token));

        Assert.Equal("provider", Assert.Throws<ArgumentNullException>(() =>
            new TaskPropertyProvider<TestInput, int>(null!)).ParamName);
        Assert.Equal("provider", Assert.Throws<ArgumentNullException>(() =>
            new AsyncPropertyProvider<TestInput, int>(null!)).ParamName);
        Assert.Equal("provider", Assert.Throws<ArgumentNullException>(() =>
            new AsyncPropertyProvider<TestInput, long, int>(
                null!, new DelegateConverter<long, int>(value => Task.FromResult(value * 10L)))).ParamName);
        Assert.Equal("converter", Assert.Throws<ArgumentNullException>(() =>
            new AsyncPropertyProvider<TestInput, long, int>(convertedAsyncSource, null!)).ParamName);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await AssertCanceledAsync(() => taskAdapter.GetPropertyAsync(
            CreateThrowingContext<TestMessage, TestInput>(), cancellation.Token), cancellation.Token);
        Assert.Equal(1, taskSource.CallCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-PROVIDERS", "variable-and-converter-dependency-matrix")]
    public async Task VariableAndConverterProviders_PreserveNullFaultValueAndContextBoundariesAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        InitializeContext<TestMessage, TestInput> context = CreateContext(new TestInput("value"));
        var variable = new RecordingVariable(Task.FromResult(51));
        var variableProvider = new VariablePropertyProvider<TestInput, RecordingVariable, int>(
            new DelegateProvider<RecordingVariable>(() => Task.FromResult<RecordingVariable?>(variable)));

        Assert.Equal(51, await variableProvider.GetPropertyAsync(context, token));
        Assert.Same(context, variable.Context);
        Assert.Equal(token, variable.Token);
        Assert.Equal(0, await new VariablePropertyProvider<TestInput, RecordingVariable, int>(
            new DelegateProvider<RecordingVariable>(() => Task.FromResult<RecordingVariable?>(null)))
            .GetPropertyAsync(context, token));
        Assert.Equal(0, await variableProvider.GetPropertyAsync(CreateNoInputContext<TestMessage, TestInput>(), token));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new VariablePropertyProvider<TestInput, RecordingVariable, int>(new NullTaskProvider<RecordingVariable>())
                .GetPropertyAsync(context, token));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new VariablePropertyProvider<TestInput, RecordingVariable, int>(
                    new DelegateProvider<RecordingVariable>(() => Task.FromResult<RecordingVariable?>(new RecordingVariable(null!))))
                .GetPropertyAsync(context, token));

        var inputProvider = new DelegateProvider<string?>(() => Task.FromResult<string?>("source"));
        var converter = new DelegateConverter<int, string?>(value => Task.FromResult(value?.Length ?? 0));
        var converted = new PropertyConverterPropertyProvider<TestInput, int, string?>(converter, inputProvider);
        Assert.Equal(6, await converted.GetPropertyAsync(context, token));
        Assert.Same(context, converter.Context);
        Assert.Equal(token, converter.Token);
        Assert.Equal(0, await converted.GetPropertyAsync(CreateNoInputContext<TestMessage, TestInput>(), token));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new PropertyConverterPropertyProvider<TestInput, int, string?>(converter, new NullTaskProvider<string?>())
                .GetPropertyAsync(context, token));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new PropertyConverterPropertyProvider<TestInput, int, string?>(
                    new NullTaskConverter<int, string?>(), inputProvider)
                .GetPropertyAsync(context, token));
        Assert.Equal("converter", Assert.Throws<ArgumentNullException>(() =>
            new PropertyConverterPropertyProvider<TestInput, int, string?>(null, inputProvider)).ParamName);
        Assert.Equal("inputProvider", Assert.Throws<ArgumentNullException>(() =>
            new PropertyConverterPropertyProvider<TestInput, int, string?>(converter, null)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-PROVIDERS", "runtime-object-cache-null-and-unsupported-matrix")]
    public async Task RuntimeObjectProvider_CachesConvertersAndDistinguishesNullUnsupportedAndInvalidDependenciesAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        InitializeContext<TestMessage, TestInput> context = CreateContext(new TestInput("value"));
        var factory = new RuntimeConverterFactory();
        var source = new DelegateProvider<object>(() => Task.FromResult<object?>("payload"));
        var provider = new ObjectPropertyProvider<TestInput, Payload>(factory, source);

        Payload? first = await provider.GetPropertyAsync(context, token);
        Payload? second = await provider.GetPropertyAsync(context, token);

        Assert.Equal(new Payload("payload"), first);
        Assert.Equal(new Payload("payload"), second);
        Assert.Equal(1, factory.RequestCount);
        Assert.Equal(2, source.CallCount);
        Assert.Null(await new ObjectPropertyProvider<TestInput, Payload>(factory,
            new DelegateProvider<object>(() => Task.FromResult<object?>(null))).GetPropertyAsync(context, token));
        Assert.Null(await new ObjectPropertyProvider<TestInput, Payload>(factory,
            new DelegateProvider<object>(() => Task.FromResult<object?>(42))).GetPropertyAsync(context, token));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ObjectPropertyProvider<TestInput, Payload>(factory, new NullTaskProvider<object>())
                .GetPropertyAsync(context, token));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ObjectPropertyProvider<TestInput, Payload>(new NullRuntimeConverterFactory(), source)
                .GetPropertyAsync(context, token));
    }

    static async Task AssertCanceledAsync(Func<Task> operation, CancellationToken expectedToken)
    {
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(operation);
        Assert.Equal(expectedToken, exception.CancellationToken);
    }

    static InitializeContext<TestMessage, TInput> CreateContext<TInput>(TInput input)
        where TInput : class =>
        new BaseInitializeContext(TestContext.Current.CancellationToken)
            .CreateMessageContext(new TestMessage())
            .CreateInputContext(input);

    static InitializeContext<TMessage, TInput> CreateNoInputContext<TMessage, TInput>()
        where TMessage : class
        where TInput : class =>
        DispatchProxy.Create<InitializeContext<TMessage, TInput>, NoInputContextProxy>();

    static InitializeContext<TMessage, TInput> CreateThrowingContext<TMessage, TInput>()
        where TMessage : class
        where TInput : class =>
        DispatchProxy.Create<InitializeContext<TMessage, TInput>, ThrowingContextProxy>();

    private sealed class DelegateProvider<TProperty>(Func<Task<TProperty?>> getValue) : IPropertyProvider<TestInput, TProperty>
    {
        public int CallCount { get; private set; }

        public Task<TProperty?> GetPropertyAsync<TMessage>(InitializeContext<TMessage, TestInput> context,
            CancellationToken cancellationToken = default)
            where TMessage : class
        {
            CallCount++;
            return getValue();
        }
    }

    private sealed class NullTaskProvider<TProperty> : IPropertyProvider<TestInput, TProperty>
    {
        public Task<TProperty?> GetPropertyAsync<TMessage>(InitializeContext<TMessage, TestInput> context,
            CancellationToken cancellationToken = default)
            where TMessage : class => null!;
    }

    private sealed class DelegateConverter<TResult, TInput>(Func<TInput?, Task<TResult?>> convert) : IPropertyConverter<TResult, TInput>
    {
        public object? Context { get; private set; }

        public CancellationToken Token { get; private set; }

        public Task<TResult?> ConvertAsync<TMessage>(InitializeContext<TMessage> context, TInput? input,
            CancellationToken cancellationToken = default)
            where TMessage : class
        {
            Context = context;
            Token = cancellationToken;
            return convert(input);
        }
    }

    private sealed class NullTaskConverter<TResult, TInput> : IPropertyConverter<TResult, TInput>
    {
        public Task<TResult?> ConvertAsync<TMessage>(InitializeContext<TMessage> context, TInput? input,
            CancellationToken cancellationToken = default)
            where TMessage : class => null!;
    }

    private sealed class RecordingVariable(Task<int> valueTask) : IInitializerVariable<int>
    {
        public object? Context { get; private set; }

        public CancellationToken Token { get; private set; }

        public Task<int> GetValueAsync<TMessage>(InitializeContext<TMessage> context,
            CancellationToken cancellationToken = default)
            where TMessage : class
        {
            Context = context;
            Token = cancellationToken;
            return valueTask;
        }
    }

    private sealed class RuntimeConverterFactory : IPropertyProviderFactory<TestInput>
    {
        public int RequestCount { get; private set; }

        public bool TryGetPropertyProvider<TResult>(PropertyInfo propertyInfo,
            [NotNullWhen(true)] out IPropertyProvider<TestInput, TResult>? provider)
        {
            provider = null;
            return false;
        }

        public bool TryGetPropertyConverter<TResult, TProperty>(
            [NotNullWhen(true)] out IPropertyConverter<TResult, TProperty>? converter)
        {
            RequestCount++;
            if (typeof(TResult) == typeof(Payload) && typeof(TProperty) == typeof(string))
            {
                converter = (IPropertyConverter<TResult, TProperty>)(object)new DelegateConverter<Payload, string>(
                    value => Task.FromResult<Payload?>(new Payload(value!)));
                return true;
            }

            converter = null;
            return false;
        }
    }

    private sealed class NullRuntimeConverterFactory : IPropertyProviderFactory<TestInput>
    {
        public bool TryGetPropertyProvider<TResult>(PropertyInfo propertyInfo,
            [NotNullWhen(true)] out IPropertyProvider<TestInput, TResult>? provider)
        {
            provider = null;
            return false;
        }

        public bool TryGetPropertyConverter<TResult, TProperty>(
            [NotNullWhen(true)] out IPropertyConverter<TResult, TProperty>? converter)
        {
            if (typeof(TResult) == typeof(Payload) && typeof(TProperty) == typeof(string))
            {
                converter = (IPropertyConverter<TResult, TProperty>)(object)new NullTaskConverter<Payload, string>();
                return true;
            }

            converter = null;
            return false;
        }
    }

    private class NoInputContextProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name == "get_HasInput"
                ? false
                : throw new InvalidOperationException($"{targetMethod?.Name} must not be called without input.");
    }

    private class ThrowingContextProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"{targetMethod?.Name} must not be called after cancellation.");
    }

    private sealed record TestInput(string? Value);

    private sealed record Payload(string Value);

    private sealed class TestMessage;
}

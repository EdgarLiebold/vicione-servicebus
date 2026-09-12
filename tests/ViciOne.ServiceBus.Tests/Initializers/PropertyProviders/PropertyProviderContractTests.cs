using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.Contexts;
using ViciOne.ServiceBus.Initializers.PropertyProviders;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers.PropertyProviders;

public sealed class PropertyProviderContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-PROVIDERS", "runtime-object-conversion-token")]
    public async Task RuntimeObjectConversion_ForwardsTheCallerTokenToTheSelectedConverterAsync()
    {
        using var cancellation = new CancellationTokenSource();
        var factory = new RecordingFactory();
        var provider = new ObjectPropertyProvider<TestInput, string>(
            factory,
            new ConstantPropertyProvider<TestInput, object>("value"));
        var baseContext = new BaseInitializeContext(TestContext.Current.CancellationToken);
        InitializeContext<TestMessage> messageContext = baseContext.CreateMessageContext(new TestMessage());
        InitializeContext<TestMessage, TestInput> inputContext = messageContext.CreateInputContext(new TestInput());

        string? result = await provider.GetPropertyAsync(inputContext, cancellation.Token);

        Assert.Equal("value", result);
        Assert.Equal(cancellation.Token, factory.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-PROVIDERS", "composed-provider-caller-cancellation")]
    public async Task ComposedProviders_ObserveCallerCancellationWhileDependenciesRemainPendingAsync()
    {
        using var cancellation = new CancellationTokenSource();
        InitializeContext<TestMessage, TestInput> context = CreateContext();
        var pendingValue = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pendingInput = new NeverCompletingProvider<string>();
        var pendingVariableValue = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var variable = new PendingVariable(pendingVariableValue.Task);

        IPropertyProvider<TestInput, string> asyncProvider = new AsyncPropertyProvider<TestInput, string>(
            new ConstantPropertyProvider<TestInput, Task<string?>>(pendingValue.Task));
        Task<string?> asyncResult = asyncProvider.GetPropertyAsync(context, cancellation.Token);
        Task<string?> convertedResult = new PropertyConverterPropertyProvider<TestInput, string, string>(
                new PassthroughStringConverter(),
                pendingInput)
            .GetPropertyAsync(context, cancellation.Token);
        Task<int> variableResult = new VariablePropertyProvider<TestInput, PendingVariable, int>(
                new ConstantPropertyProvider<TestInput, PendingVariable>(variable))
            .GetPropertyAsync(context, cancellation.Token);

        cancellation.Cancel();

        await AssertCallerCancellationAsync(asyncResult, cancellation.Token);
        await AssertCallerCancellationAsync(convertedResult, cancellation.Token);
        await AssertCallerCancellationAsync(variableResult, cancellation.Token);
        Assert.False(pendingValue.Task.IsCompleted);
        Assert.False(pendingInput.Task.IsCompleted);
        Assert.False(pendingVariableValue.Task.IsCompleted);
    }

    private static InitializeContext<TestMessage, TestInput> CreateContext()
    {
        var baseContext = new BaseInitializeContext(TestContext.Current.CancellationToken);
        return baseContext.CreateMessageContext(new TestMessage()).CreateInputContext(new TestInput());
    }

    private static async Task AssertCallerCancellationAsync(Task task, CancellationToken cancellationToken)
    {
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            task.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken));
        Assert.Equal(cancellationToken, exception.CancellationToken);
    }

    private sealed class RecordingFactory : IPropertyProviderFactory<TestInput>
    {
        public CancellationToken CancellationToken { get; private set; }

        public bool TryGetPropertyProvider<TResult>(PropertyInfo propertyInfo,
            [NotNullWhen(true)] out IPropertyProvider<TestInput, TResult>? provider)
        {
            provider = null;
            return false;
        }

        public bool TryGetPropertyConverter<TResult, TProperty>(
            [NotNullWhen(true)] out IPropertyConverter<TResult, TProperty>? converter)
        {
            if (typeof(TResult) == typeof(string) && typeof(TProperty) == typeof(string))
            {
                converter = (IPropertyConverter<TResult, TProperty>)(object)new RecordingStringConverter(this);
                return true;
            }

            converter = null;
            return false;
        }

        private sealed class RecordingStringConverter(RecordingFactory owner) : IPropertyConverter<string, string>
        {
            public Task<string?> ConvertAsync<TMessage>(InitializeContext<TMessage> context, string? input,
                CancellationToken cancellationToken = default)
                where TMessage : class
            {
                owner.CancellationToken = cancellationToken;
                return Task.FromResult(input);
            }
        }
    }

    private sealed class NeverCompletingProvider<TProperty> : IPropertyProvider<TestInput, TProperty>
    {
        private readonly TaskCompletionSource<TProperty?> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<TProperty?> Task => _completion.Task;

        public Task<TProperty?> GetPropertyAsync<TMessage>(InitializeContext<TMessage, TestInput> context,
            CancellationToken cancellationToken = default)
            where TMessage : class => _completion.Task;
    }

    private sealed class PassthroughStringConverter : IPropertyConverter<string, string>
    {
        public Task<string?> ConvertAsync<TMessage>(InitializeContext<TMessage> context, string? input,
            CancellationToken cancellationToken = default)
            where TMessage : class => Task.FromResult(input);
    }

    private sealed class PendingVariable(Task<int> valueTask) : IInitializerVariable<int>
    {
        public Task<int> GetValueAsync<TMessage>(InitializeContext<TMessage> context,
            CancellationToken cancellationToken = default)
            where TMessage : class => valueTask;
    }

    private sealed class TestInput;

    private sealed class TestMessage;
}

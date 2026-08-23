using System.Reflection;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.Contexts;
using ViciOne.ServiceBus.Initializers.PropertyProviders;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers.PropertyProviders;

internal static class PropertyProviderTestContext
{
    public static PropertyProviderReader<TInput> For<TInput>(TInput input)
        where TInput : class => new(input, TestContext.Current.CancellationToken);
}

internal sealed class PropertyProviderReader<TInput>
    where TInput : class
{
    private readonly CancellationToken _cancellationToken;
    private readonly PropertyProviderFactory<TInput> _factory = new();
    private readonly TInput _input;

    public PropertyProviderReader(TInput input, CancellationToken cancellationToken)
    {
        _input = input;
        _cancellationToken = cancellationToken;
    }

    public async Task<TResult> ReadAsync<TResult>(string propertyName)
    {
        PropertyInfo? property = typeof(TInput).GetProperty(propertyName);

        Assert.NotNull(property);
        bool found = _factory.TryGetPropertyProvider(property, out IPropertyProvider<TInput, TResult> provider);
        Assert.True(found);
        Assert.NotNull(provider);

        var baseContext = new BaseInitializeContext(_cancellationToken);
        InitializeContext<TestMessage> messageContext = baseContext.CreateMessageContext(new TestMessage());
        InitializeContext<TestMessage, TInput> inputContext = messageContext.CreateInputContext(_input);

        return await provider.GetProperty(inputContext);
    }

    private sealed class TestMessage
    {
    }
}

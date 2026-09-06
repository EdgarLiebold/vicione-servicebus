using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class ConsumeContextActivatorExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-CONTEXT-ACTIVATION", "no-payload-preserves-explicit-arguments")]
    public void NoPayload_CreateInstancePreservesExplicitConstructorArguments()
    {
        ConsumeContext context = CreateContext();

        ArgumentOnlyService service = context.CreateInstance<ArgumentOnlyService>("expected");

        Assert.Equal("expected", service.Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-CONTEXT-ACTIVATION", "provider-resolves-and-constructs")]
    public void ProviderPayload_ResolvesExistingServiceAndCombinesServicesWithExplicitArguments()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddSingleton<Dependency>()
            .AddSingleton<RegisteredService>()
            .BuildServiceProvider(validateScopes: true);
        ConsumeContext context = CreateContext((typeof(IServiceProvider), provider));

        RegisteredService resolved = context.GetServiceOrCreateInstance<RegisteredService>();
        ConstructedService created = context.CreateInstance<ConstructedService>("expected");

        Assert.Same(provider.GetRequiredService<RegisteredService>(), resolved);
        Assert.Same(provider.GetRequiredService<Dependency>(), created.Dependency);
        Assert.Equal("expected", created.Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-CONTEXT-ACTIVATION", "scope-precedes-provider")]
    public void ScopePayload_TakesPrecedenceOverAProviderPayload()
    {
        var scopedDependency = new Dependency("scope");
        var providerDependency = new Dependency("provider");
        using ServiceProvider scopeOwner = new ServiceCollection()
            .AddSingleton(scopedDependency)
            .AddSingleton<RegisteredService>()
            .BuildServiceProvider(validateScopes: true);
        using IServiceScope scope = scopeOwner.CreateScope();
        using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(providerDependency)
            .AddSingleton<RegisteredService>()
            .BuildServiceProvider(validateScopes: true);
        ConsumeContext context = CreateContext(
            (typeof(IServiceScope), scope),
            (typeof(IServiceProvider), provider));

        RegisteredService resolved = context.GetServiceOrCreateInstance<RegisteredService>();
        ConstructedService created = context.CreateInstance<ConstructedService>("expected");

        Assert.Same(scopedDependency, resolved.Dependency);
        Assert.Same(scopedDependency, created.Dependency);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-CONTEXT-ACTIVATION", "no-payload-creates-parameterless-service")]
    public void NoPayload_GetServiceOrCreateInstanceCreatesAParameterlessService()
    {
        ConsumeContext context = CreateContext();

        ParameterlessService service = context.GetServiceOrCreateInstance<ParameterlessService>();

        Assert.NotNull(service);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-CONTEXT-ACTIVATION", "null-boundaries")]
    public void PublicActivationMethods_RejectNullRequiredInputs()
    {
        ConsumeContext context = CreateContext();

        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            ConsumeContextActivatorExtensions.GetServiceOrCreateInstance<object>(null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            ConsumeContextActivatorExtensions.CreateInstance<object>(null!)).ParamName);
        Assert.Equal("arguments", Assert.Throws<ArgumentNullException>(() =>
            context.CreateInstance<object>((object[])null!)).ParamName);
    }

    private static ConsumeContext CreateContext(params (Type Type, object Value)[] payloads)
    {
        ConsumeContext context = DispatchProxy.Create<ConsumeContext, PayloadContextProxy>();
        ((PayloadContextProxy)(object)context).SetPayloads(payloads);
        return context;
    }

    private sealed class Dependency(string source = "default")
    {
        public string Source { get; } = source;
    }

    private sealed class RegisteredService(Dependency dependency)
    {
        public Dependency Dependency { get; } = dependency;
    }

    private sealed class ConstructedService(Dependency dependency, string value)
    {
        public Dependency Dependency { get; } = dependency;
        public string Value { get; } = value;
    }

    private sealed class ArgumentOnlyService(string value)
    {
        public string Value { get; } = value;
    }

    private sealed class ParameterlessService;

    private class PayloadContextProxy : DispatchProxy
    {
        private IReadOnlyDictionary<Type, object> _payloads = new Dictionary<Type, object>();

        public void SetPayloads(IEnumerable<(Type Type, object Value)> payloads) =>
            _payloads = payloads.ToDictionary(entry => entry.Type, entry => entry.Value);

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == nameof(PipeContext.TryGetPayload) && targetMethod.IsGenericMethod)
            {
                Type payloadType = targetMethod.GetGenericArguments()[0];
                bool found = _payloads.TryGetValue(payloadType, out object? payload);
                args![0] = payload;
                return found;
            }

            throw new InvalidOperationException($"The activation test invoked unexpected member {targetMethod.Name}.");
        }
    }
}

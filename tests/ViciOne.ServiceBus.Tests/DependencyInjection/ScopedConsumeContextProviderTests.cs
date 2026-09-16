using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class ScopedConsumeContextProviderTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-SCOPED-CONSUME-CONTEXT", "nullable-atomic-context-snapshot-contract")]
    public void Contract_DeclaresNullableContextAndReturnsOnlyAvailableSnapshots()
    {
        MethodInfo getContext = typeof(IScopedConsumeContextProvider).GetMethod(nameof(IScopedConsumeContextProvider.GetContext))!;
        MethodInfo implementationGetContext = typeof(ScopedConsumeContextProvider)
            .GetMethod(nameof(ScopedConsumeContextProvider.GetContext))!;
        var nullabilityContext = new NullabilityInfoContext();
        var provider = new ScopedConsumeContextProvider();
        ConsumeContext context = Proxy<ConsumeContext>();

        Assert.Equal(NullabilityState.Nullable, nullabilityContext.Create(getContext.ReturnParameter).ReadState);
        Assert.Equal(NullabilityState.Nullable, nullabilityContext.Create(implementationGetContext.ReturnParameter).ReadState);
        Assert.False(provider.TryGetContext(out ConsumeContext? missing));
        Assert.Null(missing);

        using (provider.PushContext(UnavailableConsumeContext.Instance))
        {
            Assert.False(provider.TryGetContext(out ConsumeContext? unavailable));
            Assert.Null(unavailable);
        }

        using (provider.PushContext(context))
        {
            Assert.True(provider.TryGetContext(out ConsumeContext? available));
            Assert.Same(context, available);
        }

        IScopedConsumeContextProvider defaultImplementation = new RecordingProvider();
        Assert.False(defaultImplementation.TryGetContext(out ConsumeContext? defaultMissing));
        Assert.Null(defaultMissing);
        using (defaultImplementation.PushContext(UnavailableConsumeContext.Instance))
        {
            Assert.False(defaultImplementation.TryGetContext(out ConsumeContext? defaultUnavailable));
            Assert.Null(defaultUnavailable);
        }
        using (defaultImplementation.PushContext(context))
        {
            Assert.True(defaultImplementation.TryGetContext(out ConsumeContext? defaultAvailable));
            Assert.Same(context, defaultAvailable);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-SCOPED-CONSUME-CONTEXT", "constructor-and-push-boundaries-before-side-effects")]
    public void ConstructorsAndPushMethods_RejectMissingDependenciesBeforeSideEffects()
    {
        var global = new RecordingProvider();
        var typed = new TypedScopedConsumeContextProvider(global);
        var scope = new StubScope(new NullServiceProvider());
        var setterCalls = 0;
        Func<IServiceProvider, IScopedConsumeContextProvider> setterProvider = _ =>
        {
            setterCalls++;
            return global;
        };
        var setter = new SetScopedConsumeContext(setterProvider);
        var typedSetter = new SetScopedConsumeContext<IBus>(setterProvider);

        AssertParameter("context", () => new ScopedConsumeContextProvider().PushContext(null!));
        AssertParameter("global", () => new TypedScopedConsumeContextProvider(null!));
        AssertParameter("context", () => typed.PushContext(null!));
        Assert.Equal(0, global.PushCount);
        AssertParameter("setterProvider", () => new SetScopedConsumeContext(null!));
        AssertParameter("setterProvider", () => new SetScopedConsumeContext<IBus>(null!));
        AssertParameter("scope", () => setter.PushContext(null!, Proxy<ConsumeContext>()));
        AssertParameter("context", () => setter.PushContext(scope, null!));
        AssertParameter("scope", () => typedSetter.PushContext(null!, Proxy<ConsumeContext>()));
        AssertParameter("context", () => typedSetter.PushContext(scope, null!));
        Assert.Equal(0, setterCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-SCOPED-CONSUME-CONTEXT", "nested-context-publication-and-restoration")]
    public void ScopedProvider_PublishesAndRestoresNestedContextsAndHidesUnavailableContext()
    {
        var provider = new ScopedConsumeContextProvider();
        ConsumeContext first = Proxy<ConsumeContext>();
        ConsumeContext second = Proxy<ConsumeContext>();

        Assert.False(provider.HasContext);
        Assert.Null(provider.GetContext());

        using (provider.PushContext(UnavailableConsumeContext.Instance))
        {
            Assert.False(provider.HasContext);
            Assert.Same(UnavailableConsumeContext.Instance, provider.GetContext());
        }

        using (provider.PushContext(first))
        {
            Assert.True(provider.HasContext);
            Assert.Same(first, provider.GetContext());

            using (provider.PushContext(second))
            {
                Assert.True(provider.HasContext);
                Assert.Same(second, provider.GetContext());
            }

            Assert.True(provider.HasContext);
            Assert.Same(first, provider.GetContext());
        }

        Assert.False(provider.HasContext);
        Assert.Null(provider.GetContext());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-SCOPED-CONSUME-CONTEXT", "typed-local-before-global-idempotent-disposal")]
    public void TypedProvider_DisposesLocalBeforeGlobalExactlyOnce()
    {
        TypedScopedConsumeContextProvider? typed = null;
        var localWasPresentWhenGlobalDisposed = false;
        var global = new RecordingProvider(() => localWasPresentWhenGlobalDisposed = typed!.HasContext);
        typed = new TypedScopedConsumeContextProvider(global);
        ConsumeContext context = Proxy<ConsumeContext>();

        IDisposable pushed = typed.PushContext(context);

        Assert.True(typed.HasContext);
        Assert.True(global.HasContext);
        Assert.Same(context, typed.GetContext());
        Assert.Same(context, global.GetContext());

        pushed.Dispose();
        pushed.Dispose();

        Assert.False(localWasPresentWhenGlobalDisposed);
        Assert.False(typed.HasContext);
        Assert.False(global.HasContext);
        Assert.Equal(1, global.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-SCOPED-CONSUME-CONTEXT", "setters-resolve-from-exact-scope-and-forward-context")]
    public void Setters_ResolveFromExactScopeAndForwardContext()
    {
        ConsumeContext context = Proxy<ConsumeContext>();
        var provider = new RecordingProvider();
        var services = new NullServiceProvider();
        var scope = new StubScope(services);
        IServiceProvider? resolvedServices = null;
        Func<IServiceProvider, IScopedConsumeContextProvider> providerFactory = value =>
        {
            resolvedServices = value;
            return provider;
        };

        using (new SetScopedConsumeContext(providerFactory).PushContext(scope, context))
        {
            Assert.Same(services, resolvedServices);
            Assert.Same(context, provider.GetContext());
        }

        using (new SetScopedConsumeContext<IBus>(providerFactory).PushContext(scope, context))
        {
            Assert.Same(services, resolvedServices);
            Assert.Same(context, provider.GetContext());
        }

        Assert.Equal(2, provider.PushCount);
        Assert.Equal(2, provider.DisposeCount);
        Assert.False(provider.HasContext);
    }

    private static T Proxy<T>() where T : class => DispatchProxy.Create<T, PassiveProxy>();

    private static void AssertParameter(string expected, Action action) =>
        Assert.Equal(expected, Assert.Throws<ArgumentNullException>(action).ParamName);

    private sealed class RecordingProvider(Action? onDispose = null) : IScopedConsumeContextProvider
    {
        ConsumeContext? _context;

        public bool HasContext => _context != null;

        public int PushCount { get; private set; }

        public int DisposeCount { get; private set; }

        public ConsumeContext? GetContext() => _context;

        public IDisposable PushContext(ConsumeContext context)
        {
            PushCount++;
            _context = context;

            return new CallbackDisposable(() =>
            {
                DisposeCount++;
                onDispose?.Invoke();
                _context = null;
            });
        }
    }

    private sealed class CallbackDisposable(Action callback) : IDisposable
    {
        int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                callback();
        }
    }

    private sealed class StubScope(IServiceProvider serviceProvider) : IServiceScope
    {
        public IServiceProvider ServiceProvider { get; } = serviceProvider;

        public void Dispose()
        {
        }
    }

    private sealed class NullServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    private class PassiveProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.ReturnType.IsValueType == true
                ? Activator.CreateInstance(targetMethod.ReturnType)
                : null;
    }
}

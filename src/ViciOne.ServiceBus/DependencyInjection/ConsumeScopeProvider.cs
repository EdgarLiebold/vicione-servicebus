using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.DependencyInjection;

public class ConsumeScopeProvider :
    BaseConsumeScopeProvider,
    IConsumeScopeProvider
{
    public ConsumeScopeProvider(IRegistrationContext context)
        : base(context)
    {
    }

    public ConsumeScopeProvider(IServiceProvider serviceProvider, ISetScopedConsumeContext setScopedConsumeContext)
        : base(serviceProvider, setScopedConsumeContext)
    {
    }

    public void Probe(ProbeContext context)
    {
        context.Add("provider", "dependencyInjection");
    }

    public ValueTask<IConsumeScopeContext> GetScopeAsync(ConsumeContext context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.ValueTask.FromCanceled<global::ViciOne.ServiceBus.DependencyInjection.IConsumeScopeContext>(cancellationToken); return GetScopeContextAsync(context, ExistingScopeContextFactory, CreatedScopeContextFactory, PipeContextFactory);
    }

    public ValueTask<IConsumeScopeContext<T>> GetScopeAsync<T>(ConsumeContext<T> context, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.ValueTask.FromCanceled<global::ViciOne.ServiceBus.DependencyInjection.IConsumeScopeContext<T>>(cancellationToken); return GetScopeContextAsync(context, ExistingScopeContextFactory, CreatedScopeContextFactory, PipeContextFactory);
    }

    public ValueTask<IConsumerConsumeScopeContext<TConsumer, T>> GetScopeAsync<TConsumer, T>(ConsumeContext<T> context, CancellationToken cancellationToken = default)
        where TConsumer : class
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.ValueTask.FromCanceled<global::ViciOne.ServiceBus.DependencyInjection.IConsumerConsumeScopeContext<TConsumer, T>>(cancellationToken); return GetScopeContextAsync(context, ExistingScopeContextFactory<TConsumer, T>, CreatedScopeContextFactory<TConsumer, T>, PipeContextFactory);
    }

    static ConsumeContext PipeContextFactory(ConsumeContext consumeContext, IServiceScope serviceScope, IServiceProvider serviceProvider)
    {
        return new ConsumeContextScope(consumeContext, serviceScope, serviceScope.ServiceProvider, serviceProvider);
    }

    static IConsumeScopeContext ExistingScopeContextFactory(ConsumeContext consumeContext, IServiceScope serviceScope, IDisposable disposable)
    {
        return new ExistingConsumeScopeContext(consumeContext, disposable);
    }

    static IConsumeScopeContext CreatedScopeContextFactory(ConsumeContext consumeContext, IServiceScope serviceScope, IDisposable disposable)
    {
        return new CreatedConsumeScopeContext(serviceScope, consumeContext, disposable);
    }

    static ConsumeContext<T> PipeContextFactory<T>(ConsumeContext<T> consumeContext, IServiceScope serviceScope, IServiceProvider serviceProvider)
        where T : class
    {
        return new ConsumeContextScope<T>(consumeContext, serviceScope, serviceScope.ServiceProvider, serviceProvider);
    }

    IConsumeScopeContext<T> ExistingScopeContextFactory<T>(ConsumeContext<T> consumeContext, IServiceScope serviceScope, IDisposable disposable)
        where T : class
    {
        return new ExistingConsumeScopeContext<T>(consumeContext, serviceScope, disposable, SetScopedConsumeContext);
    }

    IConsumeScopeContext<T> CreatedScopeContextFactory<T>(ConsumeContext<T> consumeContext, IServiceScope serviceScope, IDisposable disposable)
        where T : class
    {
        return new CreatedConsumeScopeContext<T>(serviceScope, consumeContext, disposable, SetScopedConsumeContext);
    }

    static IConsumerConsumeScopeContext<TConsumer, T> ExistingScopeContextFactory<TConsumer, T>(ConsumeContext<T> consumeContext,
        IServiceScope serviceScope, IDisposable disposable)
        where T : class
        where TConsumer : class
    {
        var consumer = serviceScope.ServiceProvider.GetService<TConsumer>();
        if (consumer == null)
            throw new ConsumerException($"Unable to resolve consumer type '{TypeCache<TConsumer>.ShortName}'.");

        var consumerContext = new ConsumerConsumeContextScope<TConsumer, T>(consumeContext, consumer);

        return new ExistingConsumerConsumeScopeContext<TConsumer, T>(consumerContext, disposable);
    }

    static IConsumerConsumeScopeContext<TConsumer, T> CreatedScopeContextFactory<TConsumer, T>(ConsumeContext<T> consumeContext, IServiceScope serviceScope,
        IDisposable disposable)
        where T : class
        where TConsumer : class
    {
        var consumer = serviceScope.ServiceProvider.GetService<TConsumer>();
        if (consumer == null)
            throw new ConsumerException($"Unable to resolve consumer type '{TypeCache<TConsumer>.ShortName}'.");

        var consumerContext = new ConsumerConsumeContextScope<TConsumer, T>(consumeContext, consumer);

        return new CreatedConsumerConsumeScopeContext<TConsumer, T>(serviceScope, consumerContext, disposable);
    }
}

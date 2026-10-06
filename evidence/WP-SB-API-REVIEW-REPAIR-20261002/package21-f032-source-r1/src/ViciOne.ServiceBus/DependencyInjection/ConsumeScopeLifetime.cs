using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Releases an ambient-context restoration handle and an optional owned dependency-injection scope exactly once.</summary>
internal sealed class ConsumeScopeLifetime :
    IAsyncDisposable
{
    readonly IDisposable _restoreContext;
    readonly IServiceScope? _scope;
    readonly ScopedServiceLifetime _services;

    public ConsumeScopeLifetime(IDisposable restoreContext, IServiceScope? scope = null)
    {
        _restoreContext = restoreContext ?? throw new ArgumentNullException(nameof(restoreContext));
        _scope = scope;
        _services = new ScopedServiceLifetime(ReleaseScopeAsync);
    }

    public T GetService<T>(Func<IServiceProvider> getProvider)
        where T : class
        => _services.GetService<T>(getProvider);

    public ValueTask DisposeAsync() => _services.DisposeAsync();

    async ValueTask ReleaseScopeAsync()
    {
        Exception? restoreFailure = null;
        try
        {
            _restoreContext.Dispose();
        }
        catch (Exception exception)
        {
            restoreFailure = exception;
        }

        Exception? scopeFailure = null;
        try
        {
            if (_scope is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            else
                _scope?.Dispose();
        }
        catch (Exception exception)
        {
            scopeFailure = exception;
        }

        if (restoreFailure is not null && scopeFailure is not null)
        {
            throw new AggregateException(
                "The consume context could not be restored and its dependency-injection scope could not be released.",
                restoreFailure,
                scopeFailure);
        }

        if (restoreFailure is not null)
            ExceptionDispatchInfo.Capture(restoreFailure).Throw();

        if (scopeFailure is not null)
            ExceptionDispatchInfo.Capture(scopeFailure).Throw();
    }
}

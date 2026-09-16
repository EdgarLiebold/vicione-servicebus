using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Releases one owned dependency-injection scope at most once.</summary>
internal sealed class ServiceScopeLifetime :
    IAsyncDisposable
{
    readonly IServiceScope _scope;
    int _disposed;

    public ServiceScopeLifetime(IServiceScope scope)
    {
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
    }

    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return default;

        if (_scope is IAsyncDisposable asyncDisposable)
            return asyncDisposable.DisposeAsync();

        _scope.Dispose();
        return default;
    }
}

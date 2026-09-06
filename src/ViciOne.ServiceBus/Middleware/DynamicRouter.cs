using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// A dynamic router is a pipe on which additional pipes can be connected and context is
/// routed through the pipe based upon the output requirements of the connected pipes. It is built
/// around the dynamic filter, which is the central point of the router.
/// </summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public class DynamicRouter<TContext> :
    IDynamicRouter<TContext>
    where TContext : class, PipeContext
{
    readonly IDynamicFilter<TContext> _filter;
    readonly IPipe<TContext> _pipe;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="converterFactory">The converter factory.</param>
    public DynamicRouter(IPipeContextConverterFactory<TContext> converterFactory)
    {
        ArgumentNullException.ThrowIfNull(converterFactory);

        _filter = new DynamicFilter<TContext>(converterFactory);
        _pipe = Pipe.New<TContext>(x => x.UseFilter(_filter));
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        var scope = context.CreateScope("dynamicRouter");

        _pipe.Probe(scope);
    }

    Task IPipe<TContext>.SendAsync(TContext context)
    {
        return _pipe.SendAsync(context);
    }

    /// <summary>Connects pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectPipe<T>(IPipe<T> pipe)
        where T : class, PipeContext
    {
        return _filter.ConnectPipe(pipe);
    }

    ConnectHandle IFilterObserverConnector.ConnectObserver<T>(IFilterObserver<T> observer)
    {
        return _filter.ConnectObserver(observer);
    }

    ConnectHandle IFilterObserverConnector.ConnectObserver(IFilterObserver observer)
    {
        return _filter.ConnectObserver(observer);
    }
}


/// <summary>Routes dynamic operations.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
/// <typeparam name="TKey">The key used for lookup.</typeparam>
public class DynamicRouter<TContext, TKey> :
    IDynamicRouter<TContext, TKey>
    where TContext : class, PipeContext
    where TKey : notnull
{
    readonly IDynamicFilter<TContext, TKey> _filter;
    readonly IPipe<TContext> _pipe;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="converterFactory">The converter factory.</param>
    /// <param name="keyAccessor">The key accessor.</param>
    public DynamicRouter(IPipeContextConverterFactory<TContext> converterFactory, KeyAccessor<TContext, TKey> keyAccessor)
    {
        ArgumentNullException.ThrowIfNull(converterFactory);
        ArgumentNullException.ThrowIfNull(keyAccessor);

        _filter = new DynamicFilter<TContext, TKey>(converterFactory, keyAccessor);
        _pipe = Pipe.New<TContext>(x => x.UseFilter(_filter));
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        var scope = context.CreateScope("dynamicRouter");

        _pipe.Probe(scope);
    }

    Task IPipe<TContext>.SendAsync(TContext context)
    {
        return _pipe.SendAsync(context);
    }

    /// <summary>Connects pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectPipe<T>(IPipe<T> pipe)
        where T : class, PipeContext
    {
        return _filter.ConnectPipe(pipe);
    }

    ConnectHandle IFilterObserverConnector.ConnectObserver<T>(IFilterObserver<T> observer)
    {
        return _filter.ConnectObserver(observer);
    }

    ConnectHandle IFilterObserverConnector.ConnectObserver(IFilterObserver observer)
    {
        return _filter.ConnectObserver(observer);
    }

    /// <summary>Connects pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectPipe<T>(TKey key, IPipe<T> pipe)
        where T : class, PipeContext
    {
        return _filter.ConnectPipe(key, pipe);
    }
}

using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus;

public static class PipeExtensions
{
    /// <summary>
    /// Returns true if the pipe is not empty
    /// </summary>
    /// <param name="pipe"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static bool IsNotEmpty<T>(this IPipe<T>? pipe)
        where T : class, PipeContext
    {
        return pipe switch
        {
            null => false,
            PipeConfigurator<T>.EmptyPipe _ => false,
            _ => true
        };
    }

    /// <summary>
    /// Returns true if the pipe is empty
    /// </summary>
    /// <param name="pipe"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static bool IsEmpty<T>(this IPipe<T>? pipe)
        where T : class, PipeContext
    {
        return pipe switch
        {
            null => true,
            PipeConfigurator<T>.EmptyPipe _ => true,
            _ => false
        };
    }

    /// <summary>
    /// Get a payload from the pipe context
    /// </summary>
    /// <typeparam name="TPayload">The payload type</typeparam>
    /// <param name="context">The pipe context</param>
    /// <returns>The payload, or throws a PayloadNotFoundException if the payload is not present</returns>
    public static TPayload GetPayload<TPayload>(this PipeContext context)
        where TPayload : class
    {
        if (!context.TryGetPayload(out TPayload? payload))
            throw new PayloadNotFoundException($"The payload was not found: {TypeCache<TPayload>.ShortName}");

        return payload!;
    }

    /// <summary>
    /// Get a payload from the pipe context
    /// </summary>
    /// <typeparam name="TPayload">The payload type</typeparam>
    /// <param name="context">The pipe context</param>
    /// <param name="defaultPayload"></param>
    /// <returns>The payload, or the default Value</returns>
    public static TPayload GetPayload<TPayload>(this PipeContext context, TPayload defaultPayload)
        where TPayload : class
    {
        return context.TryGetPayload(out TPayload? payload) ? payload! : defaultPayload;
    }

    /// <summary>
    /// Using a filter-supplied context type, block so that the one time code is only executed once regardless of how many
    /// threads are pushing through the pipe at the same time.
    /// </summary>
    /// <typeparam name="T">The payload type, should be an interface</typeparam>
    /// <param name="context">The pipe context</param>
    /// <param name="setupMethod">The setup method, called once regardless of the thread count</param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static async Task<OneTimeContext<T>> OneTimeSetupAsync<T>(this PipeContext context, OneTimeSetupCallback setupMethod, CancellationToken cancellationToken = default)
        where T : class
    {
        cancellationToken.ThrowIfCancellationRequested(); if (context == null)
            throw new ArgumentNullException(nameof(context));
        if (setupMethod == null)
            throw new ArgumentNullException(nameof(setupMethod));

        OneTimeContextPayload<T> oneTimeContext = context.GetOrAddPayload(() => new OneTimeContextPayload<T>());

        await oneTimeContext.RunOneTimeAsync(() => new OneTimeSetupMethod(setupMethod)).ConfigureAwait(false);

        return oneTimeContext;
    }
}

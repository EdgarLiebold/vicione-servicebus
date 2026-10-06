using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Receives notifications about filter events.</summary>
public interface IFilterObserver
{
    /// <summary>Called before a context is sent through the observed output pipeline.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The pipeline context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PreSendAsync<T>(T context)
        where T : class, PipeContext;

    /// <summary>Called after the observed output pipeline completes successfully. Failures are reported to SendFaultAsync.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PostSendAsync<T>(T context)
        where T : class, PipeContext;

    /// <summary>Called when the observed output pipeline or its observers fail.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SendFaultAsync<T>(T context, Exception exception)
        where T : class, PipeContext;
}


/// <summary>Receives notifications about filter events.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public interface IFilterObserver<in TContext>
    where TContext : class, PipeContext
{
    /// <summary>Called before a context is sent through the observed output pipeline.</summary>
    /// <param name="context">The pipeline context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PreSendAsync(TContext context);

    /// <summary>Called after the observed output pipeline completes successfully. Failures are reported to SendFaultAsync.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PostSendAsync(TContext context);

    /// <summary>Called when the observed output pipeline or its observers fail.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SendFaultAsync(TContext context, Exception exception);
}

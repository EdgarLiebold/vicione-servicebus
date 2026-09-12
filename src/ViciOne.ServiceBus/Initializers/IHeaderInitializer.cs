using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers;

/// <summary>Populates outgoing headers from an initialized message and its input object.</summary>
/// <typeparam name="TMessage">The initialized message contract.</typeparam>
/// <typeparam name="TInput">The input-object type.</typeparam>
public interface IHeaderInitializer<in TMessage, in TInput>
    where TMessage : class
    where TInput : class
{
    /// <summary>Populates headers on an outgoing send context.</summary>
    /// <param name="context">The initialized message and input object.</param>
    /// <param name="sendContext">The outgoing context whose headers are populated.</param>
    /// <param name="cancellationToken">The token that cancels header initialization.</param>
    /// <returns>A task that completes after the headers have been populated.</returns>
    Task ApplyAsync(InitializeContext<TMessage, TInput> context, SendContext sendContext, CancellationToken cancellationToken = default);
}


/// <summary>Populates outgoing headers from an initialized message.</summary>
/// <typeparam name="TMessage">The initialized message contract.</typeparam>
public interface IHeaderInitializer<in TMessage>
    where TMessage : class
{
    /// <summary>Populates headers on an outgoing send context.</summary>
    /// <param name="context">The initialized message.</param>
    /// <param name="sendContext">The outgoing context whose headers are populated.</param>
    /// <param name="cancellationToken">The token that cancels header initialization.</param>
    /// <returns>A task that completes after the headers have been populated.</returns>
    Task ApplyAsync(InitializeContext<TMessage> context, SendContext sendContext, CancellationToken cancellationToken = default);
}

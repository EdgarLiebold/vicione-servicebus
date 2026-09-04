using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers;

/// <summary>
/// Initialize a message header
/// </summary>
/// <typeparam name="TMessage"></typeparam>
/// <typeparam name="TInput"></typeparam>
public interface IHeaderInitializer<in TMessage, in TInput>
    where TMessage : class
    where TInput : class
{
    Task ApplyAsync(InitializeContext<TMessage, TInput> context, SendContext sendContext, CancellationToken cancellationToken = default);
}


/// <summary>
/// Initialize a message header
/// </summary>
/// <typeparam name="TMessage"></typeparam>
public interface IHeaderInitializer<in TMessage>
    where TMessage : class
{
    Task ApplyAsync(InitializeContext<TMessage> context, SendContext sendContext, CancellationToken cancellationToken = default);
}

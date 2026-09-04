using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Initializers.Factories;

namespace ViciOne.ServiceBus.Initializers;

/// <summary>
/// Provides a message initializer cache implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class MessageInitializerCache<TMessage> :
    IMessageInitializerCache<TMessage>
    where TMessage : class
{
    readonly IDictionary<Type, Lazy<IMessageInitializer<TMessage>>> _initializers;

    MessageInitializerCache()
    {
        _initializers = new Dictionary<Type, Lazy<IMessageInitializer<TMessage>>>();
    }

    IMessageInitializer<TMessage> IMessageInitializerCache<TMessage>.GetInitializer(Type inputType)
    {
        Lazy<IMessageInitializer<TMessage>> result;
        lock (_initializers)
        {
            if (_initializers.TryGetValue(inputType, out Lazy<IMessageInitializer<TMessage>>? initializer))
                return initializer.Value;

            result = new Lazy<IMessageInitializer<TMessage>>(() => CreateMessageInitializer(inputType));

            _initializers[inputType] = result;
        }

        return result.Value;
    }

    static IMessageInitializer<TMessage> CreateMessageInitializer(Type inputType)
    {
        var factoryType = typeof(MessageInitializerFactory<,>).MakeGenericType(typeof(TMessage), inputType);

        var factory = (IMessageInitializerFactory<TMessage>)(Activator.CreateInstance(factoryType,
            new object[] { MessageInitializer.Conventions.ToArray() }) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

        return factory.CreateMessageInitializer();
    }

    /// <summary>
    /// Returns the initializer for the message/input type combination
    /// </summary>
    /// <returns></returns>
    public static IMessageInitializer<TMessage> GetInitializer(Type inputType)
    {
        return Cached.InitializerCache.GetInitializer(inputType);
    }

    /// <summary>
    /// Performs the initialize operation.
    /// </summary>
    /// <param name="values">The values value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static Task<InitializeContext<TMessage>> InitializeAsync(object values, CancellationToken cancellationToken = default)
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        return Cached.InitializerCache.GetInitializer(values.GetType()).InitializeAsync(values, cancellationToken);
    }

    /// <summary>
    /// Performs the initialize message operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="values">The values value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>> InitializeMessageAsync(PipeContext context, object values, CancellationToken cancellationToken = default)
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        IMessageInitializer<TMessage> initializer = Cached.InitializerCache.GetInitializer(values.GetType());

        return initializer.InitializeMessageAsync(context, values, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the initialize message operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="values">The values value.</param>
    /// <param name="moreValues">The more values value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>> InitializeMessageAsync(PipeContext context, object values, object?[] moreValues,
        IPipe<SendContext<TMessage>>? pipe = null, CancellationToken cancellationToken = default)
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        IMessageInitializer<TMessage> initializer = Cached.InitializerCache.GetInitializer(values.GetType());

        return initializer.InitializeMessageAsync(context, values, moreValues, pipe, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the initialize message operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="values">The values value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>> InitializeMessageAsync(PipeContext context, object values, IPipe<SendContext<TMessage>> pipe, CancellationToken cancellationToken = default)
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        IMessageInitializer<TMessage> initializer = Cached.InitializerCache.GetInitializer(values.GetType());

        return initializer.InitializeMessageAsync(context, values, pipe.IsNotEmpty() ? pipe : Pipe.Empty<SendContext<TMessage>>(), cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the initialize message operation.
    /// </summary>
    /// <param name="values">The values value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>> InitializeMessageAsync(object values, CancellationToken cancellationToken = default)
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        IMessageInitializer<TMessage> initializer = Cached.InitializerCache.GetInitializer(values.GetType());

        return initializer.InitializeMessageAsync(values, Pipe.Empty<SendContext<TMessage>>(), cancellationToken);
    }

    /// <summary>
    /// Performs the initialize message operation.
    /// </summary>
    /// <param name="values">The values value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>> InitializeMessageAsync(object values, IPipe<SendContext<TMessage>> pipe,
        CancellationToken cancellationToken = default)
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        IMessageInitializer<TMessage> initializer = Cached.InitializerCache.GetInitializer(values.GetType());

        return initializer.InitializeMessageAsync(values, pipe.IsNotEmpty() ? pipe : Pipe.Empty<SendContext<TMessage>>(), cancellationToken);
    }

    /// <summary>
    /// Performs the initialize operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="values">The values value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static Task<InitializeContext<TMessage>> InitializeAsync(InitializeContext<TMessage> context, object values, CancellationToken cancellationToken = default)
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        return Cached.InitializerCache.GetInitializer(values.GetType()).InitializeAsync(context, values, cancellationToken: cancellationToken);
    }


    static class Cached
    {
        internal static readonly IMessageInitializerCache<TMessage> InitializerCache = new MessageInitializerCache<TMessage>();
    }
}

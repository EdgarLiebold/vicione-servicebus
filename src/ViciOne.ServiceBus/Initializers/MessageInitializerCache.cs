using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Initializers.Factories;

namespace ViciOne.ServiceBus.Initializers;

/// <summary>Provides cached message initialization for a message contract.</summary>
/// <typeparam name="TMessage">The message contract produced by initialization.</typeparam>
public static class MessageInitializerCache<TMessage>
    where TMessage : class
{
    static readonly IDictionary<Type, Lazy<IMessageInitializer<TMessage>>> _initializers =
        new Dictionary<Type, Lazy<IMessageInitializer<TMessage>>>();

    static IMessageInitializer<TMessage> GetOrAddInitializer(Type inputType)
    {
        ArgumentNullException.ThrowIfNull(inputType);

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

        var factory = (IMessageInitializerFactory<TMessage>)(Activator.CreateInstance(
            factoryType,
            new object[] { MessageInitializer.Conventions.ToArray() })
            ?? throw new InvalidOperationException($"The initializer factory for '{inputType}' could not be activated."));

        return factory.CreateMessageInitializer();
    }

    /// <summary>Returns the cached initializer for the message/input type combination.</summary>
    /// <param name="inputType">The input-object type accepted by the initializer.</param>
    /// <returns>The cached initializer for <typeparamref name="TMessage"/> and <paramref name="inputType"/>.</returns>
    public static IMessageInitializer<TMessage> GetInitializer(Type inputType)
    {
        return GetOrAddInitializer(inputType);
    }

    /// <summary>Creates and populates a message from a runtime input object.</summary>
    /// <param name="input">The object whose public properties populate the message.</param>
    /// <param name="cancellationToken">The token that cancels message initialization.</param>
    /// <returns>A task containing the populated message context.</returns>
    public static Task<InitializeContext<TMessage>> InitializeAsync(object input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        return GetOrAddInitializer(input.GetType()).InitializeAsync(input, cancellationToken);
    }

    /// <summary>Creates a message that inherits an existing pipeline context.</summary>
    /// <param name="context">The pipeline context inherited by initialization and sending.</param>
    /// <param name="input">The object whose public properties populate the message.</param>
    /// <param name="cancellationToken">The token that cancels message initialization.</param>
    /// <returns>A task containing the populated message and its send pipe.</returns>
    public static Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>> InitializeMessageAsync(
        PipeContext context,
        object input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(input);

        IMessageInitializer<TMessage> initializer = GetOrAddInitializer(input.GetType());

        return initializer.InitializeMessageAsync(context, input, cancellationToken: cancellationToken);
    }

    /// <summary>Creates a message from ordered additional inputs followed by a primary input.</summary>
    /// <param name="context">The pipeline context inherited by initialization and sending.</param>
    /// <param name="input">The primary input object, applied after <paramref name="moreInputs"/>.</param>
    /// <param name="moreInputs">Additional input objects applied in array order; null entries are ignored.</param>
    /// <param name="pipe">Additional send-pipeline stages for the initialized message.</param>
    /// <param name="cancellationToken">The token that cancels message initialization.</param>
    /// <returns>A task containing the populated message and its send pipe.</returns>
    public static Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>> InitializeMessageAsync(PipeContext context, object input, object?[] moreInputs,
        IPipe<SendContext<TMessage>>? pipe = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(moreInputs);

        IMessageInitializer<TMessage> initializer = GetOrAddInitializer(input.GetType());

        return initializer.InitializeMessageAsync(context, input, moreInputs, pipe, cancellationToken: cancellationToken);
    }

    /// <summary>Creates a message and combines its header initializer with an explicit send pipe.</summary>
    /// <param name="context">The pipeline context inherited by initialization and sending.</param>
    /// <param name="input">The object whose public properties populate the message.</param>
    /// <param name="pipe">The additional send-pipeline stages.</param>
    /// <param name="cancellationToken">The token that cancels message initialization.</param>
    /// <returns>A task containing the populated message and its send pipe.</returns>
    public static Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>> InitializeMessageAsync(
        PipeContext context,
        object input,
        IPipe<SendContext<TMessage>> pipe,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(pipe);

        IMessageInitializer<TMessage> initializer = GetOrAddInitializer(input.GetType());

        return initializer.InitializeMessageAsync(
            context,
            input,
            pipe.IsNotEmpty() ? pipe : Pipe.Empty<SendContext<TMessage>>(),
            cancellationToken: cancellationToken);
    }

    /// <summary>Creates a message and its convention-derived header pipe.</summary>
    /// <param name="input">The object whose public properties populate the message.</param>
    /// <param name="cancellationToken">The token that cancels message initialization.</param>
    /// <returns>A task containing the populated message and its send pipe.</returns>
    public static Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>> InitializeMessageAsync(
        object input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        IMessageInitializer<TMessage> initializer = GetOrAddInitializer(input.GetType());

        return initializer.InitializeMessageAsync(input, Pipe.Empty<SendContext<TMessage>>(), cancellationToken);
    }

    /// <summary>Creates a message and combines its header initializer with an explicit send pipe.</summary>
    /// <param name="input">The object whose public properties populate the message.</param>
    /// <param name="pipe">The additional send-pipeline stages.</param>
    /// <param name="cancellationToken">The token that cancels message initialization.</param>
    /// <returns>A task containing the populated message and its send pipe.</returns>
    public static Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>> InitializeMessageAsync(object input, IPipe<SendContext<TMessage>> pipe,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(pipe);

        IMessageInitializer<TMessage> initializer = GetOrAddInitializer(input.GetType());

        return initializer.InitializeMessageAsync(input, pipe.IsNotEmpty() ? pipe : Pipe.Empty<SendContext<TMessage>>(), cancellationToken);
    }

    /// <summary>Populates an existing message context from a runtime input object.</summary>
    /// <param name="context">The existing message context to populate.</param>
    /// <param name="input">The object whose public properties populate the message.</param>
    /// <param name="cancellationToken">The token that cancels message initialization.</param>
    /// <returns>A task containing the populated message context.</returns>
    public static Task<InitializeContext<TMessage>> InitializeAsync(InitializeContext<TMessage> context, object input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(input);

        return GetOrAddInitializer(input.GetType()).InitializeAsync(context, input, cancellationToken: cancellationToken);
    }
}

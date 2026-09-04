using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers.Factories;

/// <summary>
/// Provides a message initializer builder implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
/// <typeparam name="TInput">The t input type.</typeparam>
public class MessageInitializerBuilder<TMessage, TInput> :
    IMessageInitializerBuilder<TMessage, TInput>
    where TMessage : class
    where TInput : class
{
    readonly List<IHeaderInitializer<TMessage, TInput>> _headerInitializers;
    readonly IDictionary<string, IPropertyInitializer<TMessage, TInput>> _initializers;
    readonly HashSet<string> _inputPropertyUsed;
    readonly IMessageFactory<TMessage>? _messageFactory;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="messageFactory">The message factory value.</param>
    public MessageInitializerBuilder(IMessageFactory<TMessage>? messageFactory)
    {
        if (!MessageTypeCache<TMessage>.IsValidMessageType)
            throw new ArgumentException(MessageTypeCache<TMessage>.InvalidMessageTypeReason, nameof(TMessage));

        _messageFactory = messageFactory;

        _initializers = new Dictionary<string, IPropertyInitializer<TMessage, TInput>>(StringComparer.OrdinalIgnoreCase);
        _headerInitializers = new List<IHeaderInitializer<TMessage, TInput>>();
        _inputPropertyUsed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <param name="propertyName">The property name value.</param>
    /// <param name="initializer">The initializer value.</param>
    public void Add(string propertyName, IPropertyInitializer<TMessage> initializer)
    {
        _initializers[propertyName] = new PropertyAdapter(initializer);
    }

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <param name="propertyName">The property name value.</param>
    /// <param name="initializer">The initializer value.</param>
    public void Add(string propertyName, IPropertyInitializer<TMessage, TInput> initializer)
    {
        _initializers[propertyName] = initializer;
    }

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <param name="initializer">The initializer value.</param>
    public void Add(IHeaderInitializer<TMessage> initializer)
    {
        _headerInitializers.Add(new HeaderAdapter(initializer));
    }

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <param name="initializer">The initializer value.</param>
    public void Add(IHeaderInitializer<TMessage, TInput> initializer)
    {
        _headerInitializers.Add(initializer);
    }

    /// <summary>
    /// Determines whether input property used.
    /// </summary>
    /// <param name="propertyName">The property name value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool IsInputPropertyUsed(string propertyName)
    {
        return _inputPropertyUsed.Contains(propertyName);
    }

    /// <summary>
    /// Sets input property used.
    /// </summary>
    /// <param name="propertyName">The property name value.</param>
    public void SetInputPropertyUsed(string propertyName)
    {
        _inputPropertyUsed.Add(propertyName);
    }

    /// <summary>
    /// Performs the build operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IMessageInitializer<TMessage> Build()
    {
        IMessageFactory<TMessage> messageFactory = _messageFactory ?? MessageFactoryCache<TMessage>.Factory;

        return new MessageInitializer<TMessage, TInput>(messageFactory, _initializers.Values, _headerInitializers);
    }


    class PropertyAdapter :
        IPropertyInitializer<TMessage, TInput>
    {
        readonly IPropertyInitializer<TMessage> _initializer;

        public PropertyAdapter(IPropertyInitializer<TMessage> initializer)
        {
            _initializer = initializer;
        }

        public Task ApplyAsync(InitializeContext<TMessage, TInput> context, CancellationToken cancellationToken = default)
        {
            return _initializer.ApplyAsync(context, cancellationToken: cancellationToken);
        }
    }


    class HeaderAdapter :
        IHeaderInitializer<TMessage, TInput>
    {
        readonly IHeaderInitializer<TMessage> _initializer;

        public HeaderAdapter(IHeaderInitializer<TMessage> initializer)
        {
            _initializer = initializer;
        }

        public Task ApplyAsync(InitializeContext<TMessage, TInput> context, SendContext sendContext, CancellationToken cancellationToken = default)
        {
            return _initializer.ApplyAsync(context, sendContext, cancellationToken: cancellationToken);
        }
    }
}

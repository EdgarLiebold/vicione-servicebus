using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers.Factories;

/// <summary>Builds message initializer components.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="TInput">The input type.</typeparam>
public class MessageInitializerBuilder<TMessage, TInput> :
    IMessageInitializerBuilder<TMessage, TInput>
    where TMessage : class
    where TInput : class
{
    readonly List<IHeaderInitializer<TMessage, TInput>> _headerInitializers;
    readonly IDictionary<string, IPropertyInitializer<TMessage, TInput>> _initializers;
    readonly HashSet<string> _inputPropertyUsed;
    readonly IMessageFactory<TMessage>? _messageFactory;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="messageFactory">The message factory.</param>
    public MessageInitializerBuilder(IMessageFactory<TMessage>? messageFactory)
    {
        if (!MessageTypeCache<TMessage>.IsValidMessageType)
            throw new ArgumentException(MessageTypeCache<TMessage>.InvalidMessageTypeReason, nameof(TMessage));

        _messageFactory = messageFactory;

        _initializers = new Dictionary<string, IPropertyInitializer<TMessage, TInput>>(StringComparer.OrdinalIgnoreCase);
        _headerInitializers = new List<IHeaderInitializer<TMessage, TInput>>();
        _inputPropertyUsed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="propertyName">The property name.</param>
    /// <param name="initializer">The initializer.</param>
    public void Add(string propertyName, IPropertyInitializer<TMessage> initializer)
    {
        _initializers[propertyName] = new PropertyAdapter(initializer);
    }

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="propertyName">The property name.</param>
    /// <param name="initializer">The initializer.</param>
    public void Add(string propertyName, IPropertyInitializer<TMessage, TInput> initializer)
    {
        _initializers[propertyName] = initializer;
    }

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="initializer">The initializer.</param>
    public void Add(IHeaderInitializer<TMessage> initializer)
    {
        _headerInitializers.Add(new HeaderAdapter(initializer));
    }

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="initializer">The initializer.</param>
    public void Add(IHeaderInitializer<TMessage, TInput> initializer)
    {
        _headerInitializers.Add(initializer);
    }

    /// <summary>Determines whether input property used.</summary>
    /// <param name="propertyName">The property name.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool IsInputPropertyUsed(string propertyName)
    {
        return _inputPropertyUsed.Contains(propertyName);
    }

    /// <summary>Sets input property used.</summary>
    /// <param name="propertyName">The property name.</param>
    public void SetInputPropertyUsed(string propertyName)
    {
        _inputPropertyUsed.Add(propertyName);
    }

    /// <summary>Builds the configured component.</summary>
    /// <returns>The configured component.</returns>
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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers.Factories;

/// <summary>Collects resolved mappings and creates an immutable message initializer.</summary>
internal sealed class MessageInitializerBuilder<TMessage, TInput> :
    IMessageInitializerBuilder<TMessage, TInput>
    where TMessage : class
    where TInput : class
{
    readonly List<IHeaderInitializer<TMessage, TInput>> _headerInitializers;
    readonly IDictionary<string, IPropertyInitializer<TMessage, TInput>> _initializers;
    readonly HashSet<string> _inputPropertyUsed;
    readonly IMessageFactory<TMessage>? _messageFactory;

    /// <summary>Creates an empty mapping plan with an optional explicit message factory.</summary>
    /// <param name="messageFactory">The factory used to create messages, or <see langword="null" /> to use the cached default.</param>
    public MessageInitializerBuilder(IMessageFactory<TMessage>? messageFactory)
    {
        if (!MessageTypeCache<TMessage>.IsValidMessageType)
            throw new ArgumentException(MessageTypeCache<TMessage>.InvalidMessageTypeReason, nameof(TMessage));

        _messageFactory = messageFactory;

        _initializers = new Dictionary<string, IPropertyInitializer<TMessage, TInput>>(StringComparer.OrdinalIgnoreCase);
        _headerInitializers = new List<IHeaderInitializer<TMessage, TInput>>();
        _inputPropertyUsed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Adds or replaces an input-independent property mapping by name.</summary>
    /// <param name="propertyName">The case-insensitive message-property name.</param>
    /// <param name="initializer">The mapping to adapt to input-bearing initialization.</param>
    public void Add(string propertyName, IPropertyInitializer<TMessage> initializer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        ArgumentNullException.ThrowIfNull(initializer);
        _initializers[propertyName] = new PropertyAdapter(initializer);
    }

    /// <summary>Adds or replaces an input-dependent property mapping by name.</summary>
    /// <param name="propertyName">The case-insensitive message-property name.</param>
    /// <param name="initializer">The mapping applied during initialization.</param>
    public void Add(string propertyName, IPropertyInitializer<TMessage, TInput> initializer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        ArgumentNullException.ThrowIfNull(initializer);
        _initializers[propertyName] = initializer;
    }

    /// <summary>Adds an input-independent outgoing-header mapping.</summary>
    /// <param name="initializer">The mapping to adapt to input-bearing initialization.</param>
    public void Add(IHeaderInitializer<TMessage> initializer)
    {
        ArgumentNullException.ThrowIfNull(initializer);
        _headerInitializers.Add(new HeaderAdapter(initializer));
    }

    /// <summary>Adds an input-dependent outgoing-header mapping.</summary>
    /// <param name="initializer">The mapping applied by the initialized send pipe.</param>
    public void Add(IHeaderInitializer<TMessage, TInput> initializer)
    {
        ArgumentNullException.ThrowIfNull(initializer);
        _headerInitializers.Add(initializer);
    }

    /// <summary>Determines whether a convention has claimed an input property.</summary>
    /// <param name="propertyName">The case-insensitive input-property name.</param>
    /// <returns><see langword="true" /> when the property is already claimed; otherwise, <see langword="false" />.</returns>
    public bool IsInputPropertyUsed(string propertyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        return _inputPropertyUsed.Contains(propertyName);
    }

    /// <summary>Marks an input property as claimed by a convention.</summary>
    /// <param name="propertyName">The case-insensitive input-property name.</param>
    public void SetInputPropertyUsed(string propertyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        _inputPropertyUsed.Add(propertyName);
    }

    /// <summary>Creates an immutable initializer from the current property and header mappings.</summary>
    /// <returns>The completed initializer plan.</returns>
    public IMessageInitializer<TMessage> Build()
    {
        IMessageFactory<TMessage> messageFactory = _messageFactory ?? MessageFactoryCache<TMessage>.Factory;

        return new MessageInitializer<TMessage, TInput>(messageFactory, _initializers.Values, _headerInitializers);
    }


    sealed class PropertyAdapter :
        IPropertyInitializer<TMessage, TInput>
    {
        readonly IPropertyInitializer<TMessage> _initializer;

        public PropertyAdapter(IPropertyInitializer<TMessage> initializer)
        {
            _initializer = initializer ?? throw new ArgumentNullException(nameof(initializer));
        }

        public Task ApplyAsync(InitializeContext<TMessage, TInput> context, CancellationToken cancellationToken = default)
        {
            return _initializer.ApplyAsync(context, cancellationToken: cancellationToken);
        }
    }


    sealed class HeaderAdapter :
        IHeaderInitializer<TMessage, TInput>
    {
        readonly IHeaderInitializer<TMessage> _initializer;

        public HeaderAdapter(IHeaderInitializer<TMessage> initializer)
        {
            _initializer = initializer ?? throw new ArgumentNullException(nameof(initializer));
        }

        public Task ApplyAsync(InitializeContext<TMessage, TInput> context, SendContext sendContext, CancellationToken cancellationToken = default)
        {
            return _initializer.ApplyAsync(context, sendContext, cancellationToken: cancellationToken);
        }
    }
}

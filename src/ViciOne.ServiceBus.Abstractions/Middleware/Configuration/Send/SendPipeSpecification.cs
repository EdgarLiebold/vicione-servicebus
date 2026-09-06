using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for send pipe.</summary>
public class SendPipeSpecification :
    ISendPipeConfigurator,
    ISendPipeSpecification
{
    readonly object _lock = new object();
    readonly Dictionary<Type, IMessageSendPipeSpecification> _messageSpecifications;
    readonly SendPipeSpecificationObservable _observers;
    readonly List<IPipeSpecification<SendContext>> _specifications;

    /// <summary>Initializes a new instance.</summary>
    public SendPipeSpecification()
    {
        _specifications = new List<IPipeSpecification<SendContext>>();
        _messageSpecifications = new Dictionary<Type, IMessageSendPipeSpecification>();
        _observers = new SendPipeSpecificationObservable();
    }

    /// <summary>Adds pipe specification to the configuration.</summary>
    /// <param name="specification">The specification.</param>
    public void AddPipeSpecification(IPipeSpecification<SendContext> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        lock (_lock)
        {
            _specifications.Add(specification);

            foreach (IMessageSendPipeSpecification messageSpecification in _messageSpecifications.Values)
                messageSpecification.AddPipeSpecification(specification);
        }
    }

    void ISendPipeConfigurator.AddPipeSpecification<T>(IPipeSpecification<SendContext<T>> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        IMessageSendPipeSpecification<T> messageSpecification = GetMessageSpecification<T>();

        messageSpecification.AddPipeSpecification(specification);
    }

    /// <summary>Connects send pipe specification observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectSendPipeSpecificationObserver(ISendPipeSpecificationObserver observer)
    {
        return _observers.Connect(observer);
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        lock (_lock)
        {
            return _specifications.SelectMany(x => x.Validate())
                .Concat(_messageSpecifications.Values.SelectMany(x => x.Validate()))
                .ToArray();
        }
    }

    /// <summary>Gets message specification.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The message specification.</returns>
    public IMessageSendPipeSpecification<T> GetMessageSpecification<T>()
        where T : class
    {
        lock (_lock)
        {
            if (!_messageSpecifications.TryGetValue(typeof(T), out IMessageSendPipeSpecification? specification))
            {
                var created = new MessageSendPipeSpecification<T>();
                specification = created;
                _messageSpecifications.Add(typeof(T), specification);

                try
                {
                    InitializeMessageSpecification(created);
                }
                catch
                {
                    _messageSpecifications.Remove(typeof(T));
                    throw;
                }
            }

            return specification.GetMessageSpecification<T>();
        }
    }

    void InitializeMessageSpecification<T>(MessageSendPipeSpecification<T> specification)
        where T : class
    {
        foreach (IPipeSpecification<SendContext> pipeSpecification in _specifications)
            specification.AddPipeSpecification(pipeSpecification);

        _observers.MessageSpecificationCreated(specification);

        var connector = new ImplementedMessageTypeConnector<T>(this, specification);

        ImplementedMessageTypeCache<T>.EnumerateImplementedTypes(connector);
    }


    class ImplementedMessageTypeConnector<TMessage> :
        IImplementedMessageType
        where TMessage : class
    {
        readonly MessageSendPipeSpecification<TMessage> _messageSpecification;
        readonly ISendPipeSpecification _specification;

        public ImplementedMessageTypeConnector(ISendPipeSpecification specification, MessageSendPipeSpecification<TMessage> messageSpecification)
        {
            _specification = specification;
            _messageSpecification = messageSpecification;
        }

        public void ImplementsMessageType<T>(bool direct)
            where T : class
        {
            // Do not create implemented types for message types that have been excluded
            if (typeof(T).HasAttribute<ExcludeFromImplementedTypesAttribute>())
                return;

            IMessageSendPipeSpecification<T> implementedTypeSpecification = _specification.GetMessageSpecification<T>();

            _messageSpecification.AddImplementedMessageSpecification(implementedTypeSpecification);
        }
    }
}

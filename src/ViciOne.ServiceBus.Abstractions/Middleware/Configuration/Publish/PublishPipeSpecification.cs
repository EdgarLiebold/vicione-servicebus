using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Configuration;

public class PublishPipeSpecification :
    IPublishPipeConfigurator,
    IPublishPipeSpecification
{
    readonly object _lock = new object();
    readonly Dictionary<Type, IMessagePublishPipeSpecification> _messageSpecifications;
    readonly PublishPipeSpecificationObservable _observers;
    readonly List<IPipeSpecification<PublishContext>> _specifications;

    public PublishPipeSpecification()
    {
        _specifications = new List<IPipeSpecification<PublishContext>>();
        _messageSpecifications = new Dictionary<Type, IMessagePublishPipeSpecification>();
        _observers = new PublishPipeSpecificationObservable();
    }

    public void AddPipeSpecification(IPipeSpecification<PublishContext> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        lock (_lock)
        {
            _specifications.Add(specification);

            foreach (IMessagePublishPipeSpecification messageSpecification in _messageSpecifications.Values)
                messageSpecification.AddPipeSpecification(specification);
        }
    }

    public void AddPipeSpecification<T>(IPipeSpecification<PublishContext<T>> specification)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(specification);

        IMessagePublishPipeSpecification<T> messageSpecification = GetMessageSpecification<T>();

        messageSpecification.AddPipeSpecification(specification);
    }

    void IPublishPipeConfigurator.AddPipeSpecification(IPipeSpecification<SendContext> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var splitSpecification = new PipeConfigurator<PublishContext>.SplitFilterPipeSpecification<SendContext>(specification, MergeContext, FilterContext);

        AddPipeSpecification(splitSpecification);
    }

    void IPublishPipeConfigurator.AddPipeSpecification<T>(IPipeSpecification<SendContext<T>> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var splitSpecification =
            new PipeConfigurator<PublishContext<T>>.SplitFilterPipeSpecification<SendContext<T>>(specification, MergeContext, FilterContext);

        AddPipeSpecification(splitSpecification);
    }

    public ConnectHandle ConnectPublishPipeSpecificationObserver(IPublishPipeSpecificationObserver observer)
    {
        return _observers.Connect(observer);
    }

    public IEnumerable<ValidationResult> Validate()
    {
        lock (_lock)
        {
            return _specifications.SelectMany(x => x.Validate())
                .Concat(_messageSpecifications.Values.SelectMany(x => x.Validate()))
                .ToArray();
        }
    }

    public IMessagePublishPipeSpecification<T> GetMessageSpecification<T>()
        where T : class
    {
        lock (_lock)
        {
            if (!_messageSpecifications.TryGetValue(typeof(T), out IMessagePublishPipeSpecification? specification))
            {
                var created = new MessagePublishPipeSpecification<T>();
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

    static SendContext<T> FilterContext<T>(PublishContext<T> context)
        where T : class
    {
        return context;
    }

    static PublishContext<T> MergeContext<T>(PublishContext<T> input, SendContext context)
        where T : class
    {
        return context.GetPayload<PublishContext<T>>();
    }

    static SendContext FilterContext(PublishContext context)
    {
        return context;
    }

    static PublishContext MergeContext(PublishContext input, SendContext context)
    {
        return context.GetPayload<PublishContext>();
    }

    void InitializeMessageSpecification<T>(MessagePublishPipeSpecification<T> specification)
        where T : class
    {
        foreach (IPipeSpecification<PublishContext> pipeSpecification in _specifications)
            specification.AddPipeSpecification(pipeSpecification);

        _observers.MessageSpecificationCreated(specification);

        var connector = new ImplementedMessageTypeConnector<T>(this, specification);

        ImplementedMessageTypeCache<T>.EnumerateImplementedTypes(connector);
    }


    class ImplementedMessageTypeConnector<TMessage> :
        IImplementedMessageType
        where TMessage : class
    {
        readonly MessagePublishPipeSpecification<TMessage> _messageSpecification;
        readonly IPublishPipeSpecification _specification;

        public ImplementedMessageTypeConnector(IPublishPipeSpecification specification, MessagePublishPipeSpecification<TMessage> messageSpecification)
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

            IMessagePublishPipeSpecification<T> implementedTypeSpecification = _specification.GetMessageSpecification<T>();

            _messageSpecification.AddImplementedMessageSpecification(implementedTypeSpecification);
        }
    }
}

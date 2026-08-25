namespace ViciOne.ServiceBus.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Internals;
    using Metadata;


    public class SendPipeSpecification :
        ISendPipeConfigurator,
        ISendPipeSpecification
    {
        readonly object _lock = new object();
        readonly Dictionary<Type, IMessageSendPipeSpecification> _messageSpecifications;
        readonly SendPipeSpecificationObservable _observers;
        readonly List<IPipeSpecification<SendContext>> _specifications;

        public SendPipeSpecification()
        {
            _specifications = new List<IPipeSpecification<SendContext>>();
            _messageSpecifications = new Dictionary<Type, IMessageSendPipeSpecification>();
            _observers = new SendPipeSpecificationObservable();
        }

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

        public ConnectHandle ConnectSendPipeSpecificationObserver(ISendPipeSpecificationObserver observer)
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
}

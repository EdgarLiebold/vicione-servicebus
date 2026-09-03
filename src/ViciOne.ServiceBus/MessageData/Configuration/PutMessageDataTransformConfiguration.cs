namespace ViciOne.ServiceBus.MessageData.Configuration
{
    using System;
    using System.Reflection;
    using Initializers.PropertyProviders;
    using PropertyProviders;


    public class PutMessageDataTransformConfiguration<TInput, TValue> :
        IMessageDataTransformConfiguration<TInput>
        where TInput : class
    {
        readonly PropertyInfo _property;
        readonly IMessageDataRepository _repository;
        readonly MessageDataPolicy _policy;

        public PutMessageDataTransformConfiguration(IMessageDataRepository repository, MessageDataPolicy policy, PropertyInfo property)
        {
            if (repository == null)
                throw new ArgumentNullException(nameof(repository));

            _property = property;
            _repository = repository;
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        }

        public void Apply(ITransformConfigurator<TInput> configurator)
        {
            if (!MessageTypeCache<TInput>.IsValidMessageType)
                return;

            var inputPropertyProvider = new InputPropertyProvider<TInput, MessageData<TValue>>(_property);

            var provider = new PutMessageDataPropertyProvider<TInput, TValue>(inputPropertyProvider, _repository, _policy);

            configurator.Set(_property, provider);
        }
    }
}

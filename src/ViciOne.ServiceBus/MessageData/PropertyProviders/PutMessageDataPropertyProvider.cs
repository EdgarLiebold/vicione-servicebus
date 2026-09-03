namespace ViciOne.ServiceBus.MessageData.PropertyProviders
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Initializers;
    using Metadata;
    using Util;
    using Values;


    public class PutMessageDataPropertyProvider<TInput, TValue> :
        IPropertyProvider<TInput, MessageData<TValue>>
        where TInput : class
    {
        readonly IPropertyProvider<TInput, MessageData<TValue>> _inputProvider;
        readonly IMessageDataRepository _repository;
        readonly MessageDataPolicy _policy;

        public PutMessageDataPropertyProvider(
            IPropertyProvider<TInput, MessageData<TValue>> inputProvider,
            IMessageDataRepository repository,
            MessageDataPolicy policy)
        {
            _inputProvider = inputProvider ?? throw new ArgumentNullException(nameof(inputProvider));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        }

        public Task<MessageData<TValue>> GetProperty<T>(InitializeContext<T, TInput> context)
            where T : class
        {
            if (!context.HasInput)
                return TaskResults.Default<MessageData<TValue>>();

            Task<MessageData<TValue>> inputTask = _inputProvider.GetProperty(context);
            if (inputTask.IsCompleted)
            {
                MessageData<TValue> messageData = inputTask.Result;
                if (messageData is PutMessageData<TValue> putMessageData && putMessageData.HasValue)
                    return Put(context, putMessageData.Value);

                if (messageData is IInlineMessageData && messageData.HasValue && messageData.Address == null)
                    return Put(context, messageData.Value);

                return Task.FromResult(messageData);
            }

            async Task<MessageData<TValue>> GetPropertyAsync()
            {
                MessageData<TValue> messageData = await inputTask.ConfigureAwait(false);

                if (messageData is PutMessageData<TValue> putMessageData && putMessageData.HasValue)
                    return await Put(context, putMessageData.Value).ConfigureAwait(false);

                if (messageData is IInlineMessageData && messageData.HasValue && messageData.Address == null)
                    return await Put(context, messageData.Value).ConfigureAwait(false);

                return messageData;
            }

            return GetPropertyAsync();
        }

        async Task<MessageData<TValue>> Put(PipeContext context, Task<TValue> valueTask)
        {
            var repository = _repository;
            TimeSpan? timeToLive = default;
            if (context.TryGetPayload(out SendContext sendContext) && sendContext.TimeToLive.HasValue)
                timeToLive = sendContext.TimeToLive;

            if (timeToLive.HasValue && _policy.ExtraTimeToLive.HasValue)
                timeToLive += _policy.ExtraTimeToLive;

            if (!timeToLive.HasValue && _policy.TimeToLive.HasValue)
                timeToLive = _policy.TimeToLive.Value;

            var value = await valueTask.ConfigureAwait(false);
            if (value is string stringValue)
            {
                MessageData<string> messageData = await repository.PutString(stringValue, timeToLive, _policy, context.CancellationToken).ConfigureAwait(false);
                return (MessageData<TValue>)messageData;
            }

            if (value is byte[] bytesValue)
            {
                MessageData<byte[]> messageData = await repository.PutBytes(bytesValue, timeToLive, _policy, context.CancellationToken).ConfigureAwait(false);
                return (MessageData<TValue>)messageData;
            }

            if (value is Stream streamValue)
            {
                MessageData<Stream> messageData = await repository.PutStream(streamValue, timeToLive, context.CancellationToken).ConfigureAwait(false);
                return (MessageData<TValue>)messageData;
            }

            if (value is { } && TypeMetadataCache.IsValidMessageDataType(value.GetType()))
            {
                var messageData = await repository.PutObject(value, value.GetType(), timeToLive, _policy, context.CancellationToken).ConfigureAwait(false);

                if (messageData is IInlineMessageData inlineMessageData)
                    return new InlineMessageData<TValue>(messageData.Address, value, inlineMessageData);

                return new StoredMessageData<TValue>(messageData.Address, value);
            }

            throw new MessageDataException("Unsupported message data type: " + TypeCache<TValue>.ShortName);
        }
    }
}

using System;
using System.IO;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.MessageData.Values;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.MessageData.PropertyProviders;

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

    public Task<MessageData<TValue>?> GetPropertyAsync<T>(InitializeContext<T, TInput> context, CancellationToken cancellationToken = default)
        where T : class
    {
        if (!context.HasInput)
            return TaskResults.DefaultAsync<MessageData<TValue>>(cancellationToken: cancellationToken);

        Task<MessageData<TValue>?> inputTask = _inputProvider.GetPropertyAsync(context, cancellationToken: cancellationToken);
        if (inputTask.IsCompleted)
        {
            MessageData<TValue>? messageData = inputTask.Result;
            if (messageData == null)
                return TaskResults.DefaultAsync<MessageData<TValue>>(cancellationToken: cancellationToken);

            if (messageData is PutMessageData<TValue> putMessageData && putMessageData.HasValue)
                return PutAsync(context, putMessageData.Value);

            if (messageData is IInlineMessageData && messageData.HasValue && messageData.Address == null)
                return PutAsync(context, messageData.Value);

            ObserveStoredReference(context, messageData);
            return Task.FromResult<MessageData<TValue>?>(messageData);
        }

        async Task<MessageData<TValue>?> GetPropertyAsync()
        {
            MessageData<TValue>? messageData = await inputTask.ConfigureAwait(false);
            if (messageData == null)
                return null;

            if (messageData is PutMessageData<TValue> putMessageData && putMessageData.HasValue)
                return await PutAsync(context, putMessageData.Value).ConfigureAwait(false);

            if (messageData is IInlineMessageData && messageData.HasValue && messageData.Address == null)
                return await PutAsync(context, messageData.Value).ConfigureAwait(false);

            ObserveStoredReference(context, messageData);
            return messageData;
        }

        return GetPropertyAsync();
    }

    async Task<MessageData<TValue>?> PutAsync(PipeContext context, Task<TValue?> valueTask)
    {
        var repository = _repository;
        TimeSpan? timeToLive = default;
        if (context.TryGetPayload(out SendContext? sendContext) && sendContext.TimeToLive.HasValue)
            timeToLive = sendContext.TimeToLive;

        if (timeToLive.HasValue && _policy.ExtraTimeToLive.HasValue)
            timeToLive += _policy.ExtraTimeToLive;

        if (!timeToLive.HasValue && _policy.TimeToLive.HasValue)
            timeToLive = _policy.TimeToLive.Value;

        var value = await valueTask.ConfigureAwait(false);
        if (value is string stringValue)
        {
            MessageData<string> messageData = await repository.PutStringAsync(stringValue, timeToLive, _policy, context.CancellationToken).ConfigureAwait(false);
            ObserveStoredReference(context, messageData);
            return (MessageData<TValue>)messageData;
        }

        if (value is byte[] bytesValue)
        {
            MessageData<byte[]> messageData = await repository.PutBytesAsync(bytesValue, timeToLive, _policy, context.CancellationToken).ConfigureAwait(false);
            ObserveStoredReference(context, messageData);
            return (MessageData<TValue>)messageData;
        }

        if (value is Stream streamValue)
        {
            MessageData<Stream> messageData = await repository.PutStreamAsync(streamValue, timeToLive, context.CancellationToken).ConfigureAwait(false);
            ObserveStoredReference(context, messageData);
            return (MessageData<TValue>)messageData;
        }

        if (value is { } && TypeMetadataCache.IsValidMessageDataType(value.GetType()))
        {
            var messageData = await repository.PutObjectAsync(value, value.GetType(), timeToLive, _policy, context.CancellationToken).ConfigureAwait(false);

            if (messageData is IInlineMessageData inlineMessageData)
            {
                var result = new InlineMessageData<TValue>(messageData.Address, value, inlineMessageData);
                ObserveStoredReference(context, result);
                return result;
            }

            var stored = new StoredMessageData<TValue>(messageData.Address, value);
            ObserveStoredReference(context, stored);
            return stored;
        }

        throw new MessageDataException("Unsupported message data type: " + TypeCache<TValue>.ShortName);
    }

    void ObserveStoredReference(PipeContext context, IMessageData messageData)
    {
        if (messageData is not { HasValue: true } || messageData.Address == null)
            return;

        PipeContext evidenceOwner = context.TryGetPayload(out SendContext? sendContext)
            ? sendContext
            : context;
        MessageDataAdmissionEvidence evidence = evidenceOwner.GetOrAddPayload(
            () => new MessageDataAdmissionEvidence(_repository, _policy));
        evidence.Observe(_repository, _policy);
    }
}

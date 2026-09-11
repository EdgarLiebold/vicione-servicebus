using System;
using System.IO;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.MessageData.Admission;
using ViciOne.ServiceBus.MessageData.Internals;
using ViciOne.ServiceBus.MessageData.Serialization;
using ViciOne.ServiceBus.MessageData.Values;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.MessageData.PropertyProviders;

/// <summary>Applies the bus-owned storage policy to outgoing message-data properties.</summary>
/// <typeparam name="TInput">The outgoing message type.</typeparam>
/// <typeparam name="TValue">The message-data value type.</typeparam>
internal sealed class PutMessageDataPropertyProvider<TInput, TValue> :
    IPropertyProvider<TInput, MessageData<TValue>>
    where TInput : class
{
    readonly IPropertyProvider<TInput, MessageData<TValue>> _inputProvider;
    readonly IMessageDataRepository _repository;
    readonly MessageDataPolicy _policy;

    /// <summary>Creates a property provider bound to one repository and policy owner.</summary>
    /// <param name="inputProvider">The provider that reads the outgoing property.</param>
    /// <param name="repository">The repository used for external storage.</param>
    /// <param name="policy">The policy that selects inline or external storage.</param>
    public PutMessageDataPropertyProvider(
        IPropertyProvider<TInput, MessageData<TValue>> inputProvider,
        IMessageDataRepository repository,
        MessageDataPolicy policy)
    {
        _inputProvider = inputProvider ?? throw new ArgumentNullException(nameof(inputProvider));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
    }

    /// <summary>Returns an already stored value or materializes an addressless value according to the configured policy.</summary>
    /// <typeparam name="T">The message type being initialized.</typeparam>
    /// <param name="context">The initialization context containing the outgoing input.</param>
    /// <param name="cancellationToken">The token propagated to input reading and repository storage.</param>
    /// <returns>The transformed message-data value, or <see langword="null" /> when the input property is absent.</returns>
    public async Task<MessageData<TValue>?> GetPropertyAsync<T>(InitializeContext<T, TInput> context, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.HasInput)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return null;
        }

        MessageData<TValue>? messageData = await _inputProvider.GetPropertyAsync(context, cancellationToken).ConfigureAwait(false);
        if (messageData is null)
            return null;
        if (messageData is { HasValue: true, Address: null })
            return await PutAsync(context, messageData.Value, cancellationToken).ConfigureAwait(false);

        ObserveStoredReference(context, messageData);
        return messageData;
    }

    async Task<MessageData<TValue>> PutAsync(PipeContext context, Task<TValue?> valueTask, CancellationToken cancellationToken)
    {
        var repository = _repository;
        TimeSpan? timeToLive = default;
        if (context.TryGetPayload(out SendContext? sendContext) && sendContext.TimeToLive.HasValue)
            timeToLive = sendContext.TimeToLive;

        if (timeToLive.HasValue && _policy.ExtraTimeToLive.HasValue)
        {
            try
            {
                timeToLive = timeToLive.Value.Add(_policy.ExtraTimeToLive.Value);
            }
            catch (OverflowException exception)
            {
                throw new MessageDataException("The outgoing message lifetime and additional repository retention exceed the supported duration.", exception);
            }
        }

        if (!timeToLive.HasValue && _policy.TimeToLive.HasValue)
            timeToLive = _policy.TimeToLive.Value;

        var value = await valueTask.ConfigureAwait(false)
            ?? throw new MessageDataException("The message-data property reported a value but returned null.");
        if (value is string stringValue)
        {
            MessageData<string> messageData = await repository.PutStringAsync(stringValue, timeToLive, _policy, cancellationToken).ConfigureAwait(false);
            ObserveStoredReference(context, messageData);
            return (MessageData<TValue>)messageData;
        }

        if (value is byte[] bytesValue)
        {
            MessageData<byte[]> messageData = await repository.PutBytesAsync(bytesValue, timeToLive, _policy, cancellationToken).ConfigureAwait(false);
            ObserveStoredReference(context, messageData);
            return (MessageData<TValue>)messageData;
        }

        if (value is Stream streamValue)
        {
            MessageData<Stream> messageData = await repository.PutStreamAsync(streamValue, timeToLive, cancellationToken).ConfigureAwait(false);
            ObserveStoredReference(context, messageData);
            return (MessageData<TValue>)messageData;
        }

        if (MessageDataTypeClassifier.IsSupported(value.GetType()))
        {
            var messageData = await repository.PutObjectAsync(value, value.GetType(), timeToLive, _policy, cancellationToken).ConfigureAwait(false);

            if (messageData is IInlineMessageData inlineMessageData)
            {
                var result = new InlineMessageData<TValue>(messageData.Address, value, inlineMessageData);
                ObserveStoredReference(context, result);
                return result;
            }

            var address = messageData.Address
                ?? throw new MessageDataException("The repository-backed object value did not provide an address.");
            var stored = new StoredMessageData<TValue>(address, value);
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

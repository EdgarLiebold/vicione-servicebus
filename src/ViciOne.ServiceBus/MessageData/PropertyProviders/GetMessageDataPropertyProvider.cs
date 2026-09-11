using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.MessageData.Serialization;
using ViciOne.ServiceBus.MessageData.Values;

namespace ViciOne.ServiceBus.MessageData.PropertyProviders;

/// <summary>Resolves inline and repository-backed message-data properties during consume transformation.</summary>
/// <typeparam name="TInput">The incoming message type.</typeparam>
/// <typeparam name="TValue">The message-data value type.</typeparam>
internal sealed class GetMessageDataPropertyProvider<TInput, TValue> :
    IPropertyProvider<TInput, MessageData<TValue>>
    where TInput : class
{
    readonly IPropertyProvider<TInput, MessageData<TValue>> _inputProvider;
    readonly IMessageDataReader<TValue> _reader;
    readonly IMessageDataRepository _repository;

    /// <summary>Creates a property resolver bound to one repository owner.</summary>
    /// <param name="inputProvider">The provider that reads the serialized message-data property.</param>
    /// <param name="repository">The repository that owns external references.</param>
    public GetMessageDataPropertyProvider(
        IPropertyProvider<TInput, MessageData<TValue>> inputProvider,
        IMessageDataRepository repository)
    {
        _inputProvider = inputProvider ?? throw new ArgumentNullException(nameof(inputProvider));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));

        _reader = MessageDataReaderFactory.CreateReader<TValue>();
    }

    /// <summary>Returns inline data unchanged, normalizes empty data, or creates a lazy repository-backed value.</summary>
    /// <typeparam name="T">The message type being initialized.</typeparam>
    /// <param name="context">The initialization context containing the serialized input.</param>
    /// <param name="cancellationToken">The token propagated to input reading and lazy repository loading.</param>
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
        if (messageData is IInlineMessageData)
            return messageData;
        if (!messageData.HasValue || messageData.Address is null)
            return EmptyMessageData<TValue>.Instance;

        return _reader.GetMessageData(_repository, messageData.Address, cancellationToken);
    }
}

using System.IO;
using System.Text;
using System.Threading.Tasks;
using ViciOne.ServiceBus.MessageData.Values;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Initializers.PropertyConverters;

/// <summary>Converts scalar, stream, and binary initializer inputs into message-data values.</summary>
internal sealed class MessageDataPropertyConverter :
    IPropertyConverter<MessageData<byte[]>, MessageData<byte[]>>,
    IPropertyConverter<MessageData<byte[]>, MessageData<string>>,
    IPropertyConverter<MessageData<string>, MessageData<string>>,
    IPropertyConverter<MessageData<Stream>, MessageData<Stream>>,
    IPropertyConverter<MessageData<string>, string>,
    IPropertyConverter<MessageData<byte[]>, string>,
    IPropertyConverter<MessageData<byte[]>, byte[]>,
    IPropertyConverter<MessageData<Stream>, Stream>
{
    /// <summary>Gets the shared stateless converter.</summary>
    internal static MessageDataPropertyConverter Instance { get; } = new MessageDataPropertyConverter();

    MessageDataPropertyConverter()
    {
    }

    /// <summary>Wraps a binary input in a deferred-storage message-data value.</summary>
    /// <typeparam name="T">The message type being initialized.</typeparam>
    /// <param name="context">The active initialization context.</param>
    /// <param name="input">The binary input, or <see langword="null" />.</param>
    /// <param name="cancellationToken">The token that cancels conversion.</param>
    /// <returns>The converted value, or <see langword="null" /> when the input is absent.</returns>
    public Task<MessageData<byte[]>?> ConvertAsync<T>(InitializeContext<T> context, byte[]? input, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<MessageData<byte[]>?>(cancellationToken);

        return input == null
            ? TaskResults.DefaultAsync<MessageData<byte[]>>()
            : Task.FromResult<MessageData<byte[]>?>(new PutMessageData<byte[]>(input));
    }

    /// <summary>Preserves an existing binary message-data value.</summary>
    /// <typeparam name="T">The message type being initialized.</typeparam>
    /// <param name="context">The active initialization context.</param>
    /// <param name="input">The existing value, or <see langword="null" />.</param>
    /// <param name="cancellationToken">The token that cancels conversion.</param>
    /// <returns>The original value.</returns>
    public Task<MessageData<byte[]>?> ConvertAsync<T>(InitializeContext<T> context, MessageData<byte[]>? input, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<MessageData<byte[]>?>(cancellationToken);

        return input == null
            ? TaskResults.DefaultAsync<MessageData<byte[]>>()
            : Task.FromResult<MessageData<byte[]>?>(input);
    }

    async Task<MessageData<byte[]>?> IPropertyConverter<MessageData<byte[]>, MessageData<string>>.ConvertAsync<T>(InitializeContext<T> context,
        MessageData<string>? input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        if (input == null || !input.HasValue)
            return null;

        Task<string?> valueTask = input.Value
            ?? throw new InvalidOperationException("The message data value task cannot be null.");
        var text = await valueTask.WaitAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new MessageDataException("The message data reference reported a value but returned null.");

        var bytes = Encoding.UTF8.GetBytes(text);

        if (bytes.Length < MessageDataPolicy.Default.Threshold)
            return new BytesInlineMessageData(bytes, input.Address);

        return input.Address is { } address
            ? new StoredMessageData<byte[]>(address, bytes)
            : new PutMessageData<byte[]>(bytes);
    }

    Task<MessageData<byte[]>?> IPropertyConverter<MessageData<byte[]>, string>.ConvertAsync<T>(InitializeContext<T> context, string? input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<MessageData<byte[]>?>(cancellationToken);

        if (input == null)
            return TaskResults.DefaultAsync<MessageData<byte[]>>();

        var bytes = Encoding.UTF8.GetBytes(input);

        return Task.FromResult<MessageData<byte[]>?>(bytes.Length < MessageDataPolicy.Default.Threshold
            ? (MessageData<byte[]>)new BytesInlineMessageData(bytes)
            : new PutMessageData<byte[]>(bytes));
    }

    /// <summary>Preserves an existing stream message-data value.</summary>
    /// <typeparam name="T">The message type being initialized.</typeparam>
    /// <param name="context">The active initialization context.</param>
    /// <param name="input">The existing value, or <see langword="null" />.</param>
    /// <param name="cancellationToken">The token that cancels conversion.</param>
    /// <returns>The original value.</returns>
    public Task<MessageData<Stream>?> ConvertAsync<T>(InitializeContext<T> context, MessageData<Stream>? input, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<MessageData<Stream>?>(cancellationToken);

        return input == null
            ? TaskResults.DefaultAsync<MessageData<Stream>>()
            : Task.FromResult<MessageData<Stream>?>(input);
    }

    /// <summary>Wraps a stream in a deferred-storage message-data value.</summary>
    /// <typeparam name="T">The message type being initialized.</typeparam>
    /// <param name="context">The active initialization context.</param>
    /// <param name="input">The stream input, or <see langword="null" />.</param>
    /// <param name="cancellationToken">The token that cancels conversion.</param>
    /// <returns>The converted value, or <see langword="null" /> when the input is absent.</returns>
    public Task<MessageData<Stream>?> ConvertAsync<T>(InitializeContext<T> context, Stream? input, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<MessageData<Stream>?>(cancellationToken);

        return input == null
            ? TaskResults.DefaultAsync<MessageData<Stream>>()
            : Task.FromResult<MessageData<Stream>?>(new PutMessageData<Stream>(input));
    }

    Task<MessageData<string>?> IPropertyConverter<MessageData<string>, MessageData<string>>.ConvertAsync<T>(InitializeContext<T> context,
        MessageData<string>? input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        return cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<MessageData<string>?>(cancellationToken)
            : Task.FromResult<MessageData<string>?>(input);
    }

    Task<MessageData<string>?> IPropertyConverter<MessageData<string>, string>.ConvertAsync<T>(InitializeContext<T> context, string? input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<MessageData<string>?>(cancellationToken);

        return input == null
            ? TaskResults.DefaultAsync<MessageData<string>>()
            : Task.FromResult<MessageData<string>?>(new PutMessageData<string>(input));
    }
}


/// <summary>Converts object initializer inputs into typed message-data values.</summary>
/// <typeparam name="TValue">The object contract type.</typeparam>
internal sealed class MessageDataPropertyConverter<TValue> :
    IPropertyConverter<MessageData<TValue>, MessageData<TValue>>,
    IPropertyConverter<MessageData<TValue>, TValue>
    where TValue : class
{
    /// <summary>Preserves an existing typed message-data value.</summary>
    /// <typeparam name="T">The message type being initialized.</typeparam>
    /// <param name="context">The active initialization context.</param>
    /// <param name="input">The existing value, or <see langword="null" />.</param>
    /// <param name="cancellationToken">The token that cancels conversion.</param>
    /// <returns>The original value.</returns>
    public Task<MessageData<TValue>?> ConvertAsync<T>(InitializeContext<T> context, MessageData<TValue>? input, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<MessageData<TValue>?>(cancellationToken);

        return input == null
            ? TaskResults.DefaultAsync<MessageData<TValue>>()
            : Task.FromResult<MessageData<TValue>?>(input);
    }

    /// <summary>Wraps an object input in a deferred-storage message-data value.</summary>
    /// <typeparam name="T">The message type being initialized.</typeparam>
    /// <param name="context">The active initialization context.</param>
    /// <param name="input">The object input, or <see langword="null" />.</param>
    /// <param name="cancellationToken">The token that cancels conversion.</param>
    /// <returns>The converted value, or <see langword="null" /> when the input is absent.</returns>
    public Task<MessageData<TValue>?> ConvertAsync<T>(InitializeContext<T> context, TValue? input, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<MessageData<TValue>?>(cancellationToken);

        return input == null
            ? TaskResults.DefaultAsync<MessageData<TValue>>()
            : Task.FromResult<MessageData<TValue>?>(new PutMessageData<TValue>(input));
    }
}

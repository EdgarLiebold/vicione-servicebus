using System.IO;
using System.Text;
using System.Threading.Tasks;
using ViciOne.ServiceBus.MessageData.Values;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Initializers.PropertyConverters;

/// <summary>Converts message data property values.</summary>
public class MessageDataPropertyConverter :
    IPropertyConverter<MessageData<byte[]>, MessageData<byte[]>>,
    IPropertyConverter<MessageData<byte[]>, MessageData<string>>,
    IPropertyConverter<MessageData<string>, MessageData<string>>,
    IPropertyConverter<MessageData<Stream>, MessageData<Stream>>,
    IPropertyConverter<MessageData<string>, string>,
    IPropertyConverter<MessageData<byte[]>, string>,
    IPropertyConverter<MessageData<byte[]>, byte[]>,
    IPropertyConverter<MessageData<Stream>, Stream>
{
    /// <summary>Exposes the instance used by the containing type.</summary>
    public static readonly MessageDataPropertyConverter Instance = new MessageDataPropertyConverter();

    MessageDataPropertyConverter()
    {
    }

    /// <summary>Converts the supplied value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="input">The input.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the converted value.</returns>
    public Task<MessageData<byte[]>?> ConvertAsync<T>(InitializeContext<T> context, byte[]? input, CancellationToken cancellationToken = default)
        where T : class
    {
        return input == null
            ? TaskResults.DefaultAsync<MessageData<byte[]>>(cancellationToken: cancellationToken)
            : Task.FromResult<MessageData<byte[]>?>(new PutMessageData<byte[]>(input));
    }

    /// <summary>Converts the supplied value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="input">The input.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the converted value.</returns>
    public Task<MessageData<byte[]>?> ConvertAsync<T>(InitializeContext<T> context, MessageData<byte[]>? input, CancellationToken cancellationToken = default)
        where T : class
    {
        return input == null
            ? TaskResults.DefaultAsync<MessageData<byte[]>>(cancellationToken: cancellationToken)
            : Task.FromResult<MessageData<byte[]>?>(input);
    }

    async Task<MessageData<byte[]>?> IPropertyConverter<MessageData<byte[]>, MessageData<string>>.ConvertAsync<T>(InitializeContext<T> context,
        MessageData<string>? input, CancellationToken cancellationToken)
    {
        if (input == null || !input.HasValue)
            return null;

        var text = await input.Value.ConfigureAwait(false)
            ?? throw new MessageDataException("The message data reference reported a value but returned null.");

        var bytes = Encoding.UTF8.GetBytes(text);

        return bytes.Length < MessageDataPolicy.Default.Threshold
            ? (MessageData<byte[]>)new BytesInlineMessageData(bytes, input.Address)
            : new StoredMessageData<byte[]>(input.Address, bytes);
    }

    Task<MessageData<byte[]>?> IPropertyConverter<MessageData<byte[]>, string>.ConvertAsync<T>(InitializeContext<T> context, string? input, CancellationToken cancellationToken)
    {
        if (input == null)
            return TaskResults.DefaultAsync<MessageData<byte[]>>(cancellationToken: cancellationToken);

        var bytes = Encoding.UTF8.GetBytes(input);

        return Task.FromResult<MessageData<byte[]>?>(bytes.Length < MessageDataPolicy.Default.Threshold
            ? (MessageData<byte[]>)new BytesInlineMessageData(bytes)
            : new PutMessageData<byte[]>(bytes));
    }

    /// <summary>Converts the supplied value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="input">The input.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the converted value.</returns>
    public Task<MessageData<Stream>?> ConvertAsync<T>(InitializeContext<T> context, MessageData<Stream>? input, CancellationToken cancellationToken = default)
        where T : class
    {
        return input == null
            ? TaskResults.DefaultAsync<MessageData<Stream>>(cancellationToken: cancellationToken)
            : Task.FromResult<MessageData<Stream>?>(input);
    }

    /// <summary>Converts the supplied value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="input">The input.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the converted value.</returns>
    public Task<MessageData<Stream>?> ConvertAsync<T>(InitializeContext<T> context, Stream? input, CancellationToken cancellationToken = default)
        where T : class
    {
        return input == null
            ? TaskResults.DefaultAsync<MessageData<Stream>>(cancellationToken: cancellationToken)
            : Task.FromResult<MessageData<Stream>?>(new PutMessageData<Stream>(input));
    }

    Task<MessageData<string>?> IPropertyConverter<MessageData<string>, MessageData<string>>.ConvertAsync<T>(InitializeContext<T> context,
        MessageData<string>? input, CancellationToken cancellationToken)
    {
        return Task.FromResult<MessageData<string>?>(input);
    }

    Task<MessageData<string>?> IPropertyConverter<MessageData<string>, string>.ConvertAsync<T>(InitializeContext<T> context, string? input, CancellationToken cancellationToken)
    {
        return input == null
            ? TaskResults.DefaultAsync<MessageData<string>>(cancellationToken: cancellationToken)
            : Task.FromResult<MessageData<string>?>(new PutMessageData<string>(input));
    }
}


/// <summary>Converts message data property values.</summary>
/// <typeparam name="TValue">The value stored by the member.</typeparam>
public class MessageDataPropertyConverter<TValue> :
    IPropertyConverter<MessageData<TValue>, MessageData<TValue>>,
    IPropertyConverter<MessageData<TValue>, TValue>
    where TValue : class
{
    /// <summary>Converts the supplied value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="input">The input.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the converted value.</returns>
    public Task<MessageData<TValue>?> ConvertAsync<T>(InitializeContext<T> context, MessageData<TValue>? input, CancellationToken cancellationToken = default)
        where T : class
    {
        return input == null
            ? TaskResults.DefaultAsync<MessageData<TValue>>(cancellationToken: cancellationToken)
            : Task.FromResult<MessageData<TValue>?>(input);
    }

    /// <summary>Converts the supplied value.</summary>
    /// <typeparam name="T1">The 1 type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="input">The input.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the converted value.</returns>
    public Task<MessageData<TValue>?> ConvertAsync<T1>(InitializeContext<T1> context, TValue? input, CancellationToken cancellationToken = default)
        where T1 : class
    {
        return input == null
            ? TaskResults.DefaultAsync<MessageData<TValue>>(cancellationToken: cancellationToken)
            : Task.FromResult<MessageData<TValue>?>(new PutMessageData<TValue>(input));
    }
}

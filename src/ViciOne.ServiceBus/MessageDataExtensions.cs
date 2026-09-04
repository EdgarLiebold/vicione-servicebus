using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.MessageData.Values;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Provides extension methods for message data.
/// </summary>
public static class MessageDataExtensions
{
    /// <summary>
    /// Performs the put string operation.
    /// </summary>
    /// <param name="repository">The repository value.</param>
    /// <param name="value">The value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static Task<MessageData<string>> PutStringAsync(this IMessageDataRepository repository, string value,
        CancellationToken cancellationToken = default)
    {
        return PutStringAsync(repository, value, default, MessageDataPolicy.Default, cancellationToken);
    }

    /// <summary>
    /// Performs the put string operation.
    /// </summary>
    /// <param name="repository">The repository value.</param>
    /// <param name="value">The value.</param>
    /// <param name="timeToLive">The time to live value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static Task<MessageData<string>> PutStringAsync(this IMessageDataRepository repository, string value, TimeSpan? timeToLive,
        CancellationToken cancellationToken = default)
    {
        return PutStringAsync(repository, value, timeToLive, MessageDataPolicy.Default, cancellationToken);
    }

    /// <summary>
    /// Performs the put string operation.
    /// </summary>
    /// <param name="repository">The repository value.</param>
    /// <param name="value">The value.</param>
    /// <param name="timeToLive">The time to live value.</param>
    /// <param name="policy">The policy value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static async Task<MessageData<string>> PutStringAsync(this IMessageDataRepository repository, string value, TimeSpan? timeToLive,
        MessageDataPolicy policy, CancellationToken cancellationToken = default)
    {
        if (repository == null)
            throw new ArgumentNullException(nameof(repository));
        if (policy == null)
            throw new ArgumentNullException(nameof(policy));
        if (value == null)
            return EmptyMessageData<string>.Instance;

        var bytesCount = Encoding.UTF8.GetByteCount(value);
        if (bytesCount < policy.Threshold && !policy.AlwaysWriteToRepository)
            return new StringInlineMessageData(value);

        var bytes = Encoding.UTF8.GetBytes(value);

        using var ms = new MemoryStream(bytes, false);

        var address = await repository.PutAsync(ms, timeToLive, cancellationToken).ConfigureAwait(false);

        if (bytesCount < policy.Threshold)
            return new StringInlineMessageData(value, address);

        return new StoredMessageData<string>(address, value);
    }

    /// <summary>
    /// Performs the put bytes operation.
    /// </summary>
    /// <param name="repository">The repository value.</param>
    /// <param name="bytes">The bytes value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static Task<MessageData<byte[]>> PutBytesAsync(this IMessageDataRepository repository, byte[] bytes,
        CancellationToken cancellationToken = default)
    {
        return PutBytesAsync(repository, bytes, default, MessageDataPolicy.Default, cancellationToken);
    }

    /// <summary>
    /// Performs the put bytes operation.
    /// </summary>
    /// <param name="repository">The repository value.</param>
    /// <param name="bytes">The bytes value.</param>
    /// <param name="timeToLive">The time to live value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static Task<MessageData<byte[]>> PutBytesAsync(this IMessageDataRepository repository, byte[] bytes, TimeSpan? timeToLive,
        CancellationToken cancellationToken = default)
    {
        return PutBytesAsync(repository, bytes, timeToLive, MessageDataPolicy.Default, cancellationToken);
    }

    /// <summary>
    /// Performs the put bytes operation.
    /// </summary>
    /// <param name="repository">The repository value.</param>
    /// <param name="bytes">The bytes value.</param>
    /// <param name="timeToLive">The time to live value.</param>
    /// <param name="policy">The policy value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static async Task<MessageData<byte[]>> PutBytesAsync(this IMessageDataRepository repository, byte[] bytes, TimeSpan? timeToLive,
        MessageDataPolicy policy, CancellationToken cancellationToken = default)
    {
        if (repository == null)
            throw new ArgumentNullException(nameof(repository));
        if (policy == null)
            throw new ArgumentNullException(nameof(policy));
        if (bytes == null)
            return EmptyMessageData<byte[]>.Instance;

        if (bytes.Length < policy.Threshold && !policy.AlwaysWriteToRepository)
            return new BytesInlineMessageData(bytes);

        using var ms = new MemoryStream(bytes, false);

        var address = await repository.PutAsync(ms, timeToLive, cancellationToken).ConfigureAwait(false);

        if (bytes.Length < policy.Threshold)
            return new BytesInlineMessageData(bytes, address);

        return new StoredMessageData<byte[]>(address, bytes);
    }

    /// <summary>
    /// Performs the put object operation.
    /// </summary>
    /// <param name="repository">The repository value.</param>
    /// <param name="value">The value.</param>
    /// <param name="objectType">The object type value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static Task<IMessageData> PutObjectAsync(this IMessageDataRepository repository, object value, Type objectType,
        CancellationToken cancellationToken =
            default)
    {
        return PutObjectAsync(repository, value, objectType, default, MessageDataPolicy.Default, cancellationToken);
    }

    /// <summary>
    /// Performs the put object operation.
    /// </summary>
    /// <param name="repository">The repository value.</param>
    /// <param name="value">The value.</param>
    /// <param name="objectType">The object type value.</param>
    /// <param name="timeToLive">The time to live value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static Task<IMessageData> PutObjectAsync(this IMessageDataRepository repository, object value, Type objectType, TimeSpan? timeToLive,
        CancellationToken cancellationToken = default)
    {
        return PutObjectAsync(repository, value, objectType, timeToLive, MessageDataPolicy.Default, cancellationToken);
    }

    /// <summary>
    /// Performs the put object operation.
    /// </summary>
    /// <param name="repository">The repository value.</param>
    /// <param name="value">The value.</param>
    /// <param name="objectType">The object type value.</param>
    /// <param name="timeToLive">The time to live value.</param>
    /// <param name="policy">The policy value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static async Task<IMessageData> PutObjectAsync(this IMessageDataRepository repository, object value, Type objectType, TimeSpan? timeToLive,
        MessageDataPolicy policy, CancellationToken cancellationToken = default)
    {
        if (repository == null)
            throw new ArgumentNullException(nameof(repository));
        if (policy == null)
            throw new ArgumentNullException(nameof(policy));
        if (value == null)
            return EmptyMessageData<byte[]>.Instance;

        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, objectType, ServiceBusMetadataJson.Options);

        if (bytes.Length < policy.Threshold && !policy.AlwaysWriteToRepository)
            return new BytesInlineMessageData(bytes);

        using var ms = new MemoryStream(bytes, false);

        var address = await repository.PutAsync(ms, timeToLive, cancellationToken).ConfigureAwait(false);

        if (bytes.Length < policy.Threshold)
            return new BytesInlineMessageData(bytes, address);

        return new StoredMessageData<byte[]>(address, bytes);
    }

    /// <summary>
    /// Performs the put stream operation.
    /// </summary>
    /// <param name="repository">The repository value.</param>
    /// <param name="stream">The stream value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static Task<MessageData<Stream>> PutStreamAsync(this IMessageDataRepository repository, Stream stream,
        CancellationToken cancellationToken = default)
    {
        return PutStreamAsync(repository, stream, default, cancellationToken);
    }

    /// <summary>
    /// Performs the put stream operation.
    /// </summary>
    /// <param name="repository">The repository value.</param>
    /// <param name="stream">The stream value.</param>
    /// <param name="timeToLive">The time to live value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static async Task<MessageData<Stream>> PutStreamAsync(this IMessageDataRepository repository, Stream stream, TimeSpan? timeToLive,
        CancellationToken cancellationToken = default)
    {
        if (repository == null)
            throw new ArgumentNullException(nameof(repository));
        if (stream == null)
            return EmptyMessageData<Stream>.Instance;

        var address = await repository.PutAsync(stream, timeToLive, cancellationToken).ConfigureAwait(false);

        return new StoredMessageData<Stream>(address, stream);
    }

    /// <summary>
    /// Gets string.
    /// </summary>
    /// <param name="repository">The repository value.</param>
    /// <param name="address">The address value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static async Task<MessageData<string>> GetStringAsync(this IMessageDataRepository repository, Uri address,
        CancellationToken cancellationToken = default)
    {
        if (repository == null)
            throw new ArgumentNullException(nameof(repository));

        using var ms = new MemoryStream();

        using var stream = await repository.GetAsync(address, cancellationToken).ConfigureAwait(false);

        await stream.CopyToAsync(ms, 4096, cancellationToken).ConfigureAwait(false);

        return new StoredMessageData<string>(address, Encoding.UTF8.GetString(ms.ToArray()));
    }

    /// <summary>
    /// Gets bytes.
    /// </summary>
    /// <param name="repository">The repository value.</param>
    /// <param name="address">The address value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static async Task<MessageData<byte[]>> GetBytesAsync(this IMessageDataRepository repository, Uri address,
        CancellationToken cancellationToken = default)
    {
        if (repository == null)
            throw new ArgumentNullException(nameof(repository));

        using var ms = new MemoryStream();

        using var stream = await repository.GetAsync(address, cancellationToken).ConfigureAwait(false);

        await stream.CopyToAsync(ms, 4096, cancellationToken).ConfigureAwait(false);

        return new StoredMessageData<byte[]>(address, ms.ToArray());
    }
}

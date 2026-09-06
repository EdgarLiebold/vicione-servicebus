using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.MessageData.Values;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides extension methods for message data.</summary>
public static class MessageDataExtensions
{
    /// <summary>Writes string.</summary>
    /// <param name="repository">The repository.</param>
    /// <param name="value">The value to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the put string outcome.</returns>
    public static Task<MessageData<string>> PutStringAsync(this IMessageDataRepository repository, string value,
        CancellationToken cancellationToken = default)
    {
        return PutStringAsync(repository, value, default, MessageDataPolicy.Default, cancellationToken);
    }

    /// <summary>Writes string.</summary>
    /// <param name="repository">The repository.</param>
    /// <param name="value">The value to process.</param>
    /// <param name="timeToLive">The time to live.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the put string outcome.</returns>
    public static Task<MessageData<string>> PutStringAsync(this IMessageDataRepository repository, string value, TimeSpan? timeToLive,
        CancellationToken cancellationToken = default)
    {
        return PutStringAsync(repository, value, timeToLive, MessageDataPolicy.Default, cancellationToken);
    }

    /// <summary>Writes string.</summary>
    /// <param name="repository">The repository.</param>
    /// <param name="value">The value to process.</param>
    /// <param name="timeToLive">The time to live.</param>
    /// <param name="policy">The policy.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the put string outcome.</returns>
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

    /// <summary>Writes bytes.</summary>
    /// <param name="repository">The repository.</param>
    /// <param name="bytes">The bytes.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the put bytes outcome.</returns>
    public static Task<MessageData<byte[]>> PutBytesAsync(this IMessageDataRepository repository, byte[] bytes,
        CancellationToken cancellationToken = default)
    {
        return PutBytesAsync(repository, bytes, default, MessageDataPolicy.Default, cancellationToken);
    }

    /// <summary>Writes bytes.</summary>
    /// <param name="repository">The repository.</param>
    /// <param name="bytes">The bytes.</param>
    /// <param name="timeToLive">The time to live.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the put bytes outcome.</returns>
    public static Task<MessageData<byte[]>> PutBytesAsync(this IMessageDataRepository repository, byte[] bytes, TimeSpan? timeToLive,
        CancellationToken cancellationToken = default)
    {
        return PutBytesAsync(repository, bytes, timeToLive, MessageDataPolicy.Default, cancellationToken);
    }

    /// <summary>Writes bytes.</summary>
    /// <param name="repository">The repository.</param>
    /// <param name="bytes">The bytes.</param>
    /// <param name="timeToLive">The time to live.</param>
    /// <param name="policy">The policy.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the put bytes outcome.</returns>
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

    /// <summary>Writes object.</summary>
    /// <param name="repository">The repository.</param>
    /// <param name="value">The value to process.</param>
    /// <param name="objectType">The runtime object type used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the put object outcome.</returns>
    public static Task<IMessageData> PutObjectAsync(this IMessageDataRepository repository, object value, Type objectType,
        CancellationToken cancellationToken =
            default)
    {
        return PutObjectAsync(repository, value, objectType, default, MessageDataPolicy.Default, cancellationToken);
    }

    /// <summary>Writes object.</summary>
    /// <param name="repository">The repository.</param>
    /// <param name="value">The value to process.</param>
    /// <param name="objectType">The runtime object type used by the operation.</param>
    /// <param name="timeToLive">The time to live.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the put object outcome.</returns>
    public static Task<IMessageData> PutObjectAsync(this IMessageDataRepository repository, object value, Type objectType, TimeSpan? timeToLive,
        CancellationToken cancellationToken = default)
    {
        return PutObjectAsync(repository, value, objectType, timeToLive, MessageDataPolicy.Default, cancellationToken);
    }

    /// <summary>Writes object.</summary>
    /// <param name="repository">The repository.</param>
    /// <param name="value">The value to process.</param>
    /// <param name="objectType">The runtime object type used by the operation.</param>
    /// <param name="timeToLive">The time to live.</param>
    /// <param name="policy">The policy.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the put object outcome.</returns>
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

    /// <summary>Writes stream.</summary>
    /// <param name="repository">The repository.</param>
    /// <param name="stream">The stream.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the put stream outcome.</returns>
    public static Task<MessageData<Stream>> PutStreamAsync(this IMessageDataRepository repository, Stream stream,
        CancellationToken cancellationToken = default)
    {
        return PutStreamAsync(repository, stream, default, cancellationToken);
    }

    /// <summary>Writes stream.</summary>
    /// <param name="repository">The repository.</param>
    /// <param name="stream">The stream.</param>
    /// <param name="timeToLive">The time to live.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the put stream outcome.</returns>
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

    /// <summary>Gets string.</summary>
    /// <param name="repository">The repository.</param>
    /// <param name="address">The address.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
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

    /// <summary>Gets bytes.</summary>
    /// <param name="repository">The repository.</param>
    /// <param name="address">The address.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
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

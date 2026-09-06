using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.MessageData.Values;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Stores and retrieves inline or repository-backed message data.</summary>
public static class MessageDataExtensions
{
    /// <summary>Stores a string according to the default message-data policy.</summary>
    /// <param name="repository">The repository used when external storage is required.</param>
    /// <param name="value">The value, or <see langword="null"/> for empty message data.</param>
    /// <param name="cancellationToken">Cancels storage.</param>
    /// <returns>The inline or repository-backed message data.</returns>
    public static Task<MessageData<string>> PutStringAsync(this IMessageDataRepository repository, string? value,
        CancellationToken cancellationToken = default)
    {
        return PutStringAsync(repository, value, default, MessageDataPolicy.Default, cancellationToken);
    }

    /// <summary>Stores a string with an optional repository lifetime.</summary>
    /// <param name="repository">The repository used when external storage is required.</param>
    /// <param name="value">The value, or <see langword="null"/> for empty message data.</param>
    /// <param name="timeToLive">The optional repository retention period.</param>
    /// <param name="cancellationToken">Cancels storage.</param>
    /// <returns>The inline or repository-backed message data.</returns>
    public static Task<MessageData<string>> PutStringAsync(this IMessageDataRepository repository, string? value, TimeSpan? timeToLive,
        CancellationToken cancellationToken = default)
    {
        return PutStringAsync(repository, value, timeToLive, MessageDataPolicy.Default, cancellationToken);
    }

    /// <summary>Stores a string according to an explicit message-data policy.</summary>
    /// <param name="repository">The repository used when external storage is required.</param>
    /// <param name="value">The value, or <see langword="null"/> for empty message data.</param>
    /// <param name="timeToLive">The optional repository retention period.</param>
    /// <param name="policy">Controls inline and repository storage.</param>
    /// <param name="cancellationToken">Cancels storage.</param>
    /// <returns>The inline or repository-backed message data.</returns>
    public static async Task<MessageData<string>> PutStringAsync(this IMessageDataRepository repository, string? value, TimeSpan? timeToLive,
        MessageDataPolicy policy, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(policy);
        cancellationToken.ThrowIfCancellationRequested();

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

    /// <summary>Stores bytes according to the default message-data policy.</summary>
    /// <param name="repository">The repository used when external storage is required.</param>
    /// <param name="bytes">The bytes, or <see langword="null"/> for empty message data.</param>
    /// <param name="cancellationToken">Cancels storage.</param>
    /// <returns>The inline or repository-backed message data.</returns>
    public static Task<MessageData<byte[]>> PutBytesAsync(this IMessageDataRepository repository, byte[]? bytes,
        CancellationToken cancellationToken = default)
    {
        return PutBytesAsync(repository, bytes, default, MessageDataPolicy.Default, cancellationToken);
    }

    /// <summary>Stores bytes with an optional repository lifetime.</summary>
    /// <param name="repository">The repository used when external storage is required.</param>
    /// <param name="bytes">The bytes.</param>
    /// <param name="timeToLive">The optional repository retention period.</param>
    /// <param name="cancellationToken">Cancels storage.</param>
    /// <returns>The inline or repository-backed message data.</returns>
    public static Task<MessageData<byte[]>> PutBytesAsync(this IMessageDataRepository repository, byte[]? bytes, TimeSpan? timeToLive,
        CancellationToken cancellationToken = default)
    {
        return PutBytesAsync(repository, bytes, timeToLive, MessageDataPolicy.Default, cancellationToken);
    }

    /// <summary>Stores bytes according to an explicit message-data policy.</summary>
    /// <param name="repository">The repository used when external storage is required.</param>
    /// <param name="bytes">The bytes.</param>
    /// <param name="timeToLive">The optional repository retention period.</param>
    /// <param name="policy">Controls inline and repository storage.</param>
    /// <param name="cancellationToken">Cancels storage.</param>
    /// <returns>The inline or repository-backed message data.</returns>
    public static async Task<MessageData<byte[]>> PutBytesAsync(this IMessageDataRepository repository, byte[]? bytes, TimeSpan? timeToLive,
        MessageDataPolicy policy, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(policy);
        cancellationToken.ThrowIfCancellationRequested();

        if (bytes == null)
            return EmptyMessageData<byte[]>.Instance;

        byte[] snapshot = bytes.AsSpan().ToArray();

        if (snapshot.Length < policy.Threshold && !policy.AlwaysWriteToRepository)
            return new BytesInlineMessageData(snapshot);

        using var ms = new MemoryStream(snapshot, false);

        var address = await repository.PutAsync(ms, timeToLive, cancellationToken).ConfigureAwait(false);

        if (snapshot.Length < policy.Threshold)
            return new BytesInlineMessageData(snapshot, address);

        return new StoredMessageData<byte[]>(address, snapshot);
    }

    /// <summary>Serializes and stores an object according to the default message-data policy.</summary>
    /// <param name="repository">The repository used when external storage is required.</param>
    /// <param name="value">The value, or <see langword="null"/> for empty message data.</param>
    /// <param name="objectType">The runtime object type used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the put object outcome.</returns>
    public static Task<IMessageData> PutObjectAsync(this IMessageDataRepository repository, object? value, Type objectType,
        CancellationToken cancellationToken =
            default)
    {
        return PutObjectAsync(repository, value, objectType, default, MessageDataPolicy.Default, cancellationToken);
    }

    /// <summary>Serializes and stores an object with an optional repository lifetime.</summary>
    /// <param name="repository">The repository used when external storage is required.</param>
    /// <param name="value">The value, or <see langword="null"/> for empty message data.</param>
    /// <param name="objectType">The runtime object type used by the operation.</param>
    /// <param name="timeToLive">The time to live.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the put object outcome.</returns>
    public static Task<IMessageData> PutObjectAsync(this IMessageDataRepository repository, object? value, Type objectType, TimeSpan? timeToLive,
        CancellationToken cancellationToken = default)
    {
        return PutObjectAsync(repository, value, objectType, timeToLive, MessageDataPolicy.Default, cancellationToken);
    }

    /// <summary>Serializes and stores an object according to an explicit message-data policy.</summary>
    /// <param name="repository">The repository used when external storage is required.</param>
    /// <param name="value">The value, or <see langword="null"/> for empty message data.</param>
    /// <param name="objectType">The runtime object type used by the operation.</param>
    /// <param name="timeToLive">The time to live.</param>
    /// <param name="policy">The policy.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the put object outcome.</returns>
    public static async Task<IMessageData> PutObjectAsync(this IMessageDataRepository repository, object? value, Type objectType, TimeSpan? timeToLive,
        MessageDataPolicy policy, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(objectType);
        ArgumentNullException.ThrowIfNull(policy);
        cancellationToken.ThrowIfCancellationRequested();

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

    /// <summary>Stores a stream in the repository.</summary>
    /// <param name="repository">The target repository.</param>
    /// <param name="stream">The stream, or <see langword="null"/> for empty message data.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the put stream outcome.</returns>
    public static Task<MessageData<Stream>> PutStreamAsync(this IMessageDataRepository repository, Stream? stream,
        CancellationToken cancellationToken = default)
    {
        return PutStreamAsync(repository, stream, default, cancellationToken);
    }

    /// <summary>Stores a stream in the repository with an optional retention period.</summary>
    /// <param name="repository">The target repository.</param>
    /// <param name="stream">The stream, or <see langword="null"/> for empty message data.</param>
    /// <param name="timeToLive">The optional repository retention period.</param>
    /// <param name="cancellationToken">Cancels storage.</param>
    /// <returns>The repository-backed message data.</returns>
    public static async Task<MessageData<Stream>> PutStreamAsync(this IMessageDataRepository repository, Stream? stream, TimeSpan? timeToLive,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repository);
        cancellationToken.ThrowIfCancellationRequested();

        if (stream == null)
            return EmptyMessageData<Stream>.Instance;

        var address = await repository.PutAsync(stream, timeToLive, cancellationToken).ConfigureAwait(false);

        return new StoredMessageData<Stream>(address, stream);
    }

    /// <summary>Retrieves UTF-8 text from a repository address.</summary>
    /// <param name="repository">The source repository.</param>
    /// <param name="address">The stored-data address.</param>
    /// <param name="cancellationToken">Cancels retrieval.</param>
    /// <returns>The retrieved text and its repository address.</returns>
    public static async Task<MessageData<string>> GetStringAsync(this IMessageDataRepository repository, Uri address,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(address);
        cancellationToken.ThrowIfCancellationRequested();

        using var ms = new MemoryStream();

        using var stream = await repository.GetAsync(address, cancellationToken).ConfigureAwait(false);

        await stream.CopyToAsync(ms, 4096, cancellationToken).ConfigureAwait(false);

        return new StoredMessageData<string>(address, Encoding.UTF8.GetString(ms.ToArray()));
    }

    /// <summary>Retrieves bytes from a repository address.</summary>
    /// <param name="repository">The source repository.</param>
    /// <param name="address">The stored-data address.</param>
    /// <param name="cancellationToken">Cancels retrieval.</param>
    /// <returns>The retrieved bytes and their repository address.</returns>
    public static async Task<MessageData<byte[]>> GetBytesAsync(this IMessageDataRepository repository, Uri address,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(address);
        cancellationToken.ThrowIfCancellationRequested();

        using var ms = new MemoryStream();

        using var stream = await repository.GetAsync(address, cancellationToken).ConfigureAwait(false);

        await stream.CopyToAsync(ms, 4096, cancellationToken).ConfigureAwait(false);

        return new StoredMessageData<byte[]>(address, ms.ToArray());
    }
}

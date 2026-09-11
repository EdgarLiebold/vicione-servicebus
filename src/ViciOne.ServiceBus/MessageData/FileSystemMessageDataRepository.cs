using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.MessageData;

/// <summary>Stores message data as files below one configured root directory.</summary>
public sealed class FileSystemMessageDataRepository :
    IMessageDataRepository
{
    const int DefaultBufferSize = 4096;
    const string AddressPrefix = "urn:file:";
    readonly string _rootPath;
    readonly string _rootPathPrefix;
    readonly TimeProvider _timeProvider;

    /// <summary>Creates a repository constrained to one root directory.</summary>
    /// <param name="dataDirectory">The root below which message-data files are stored.</param>
    /// <param name="timeProvider">The time source used to assign and evaluate retention periods.</param>
    public FileSystemMessageDataRepository(DirectoryInfo dataDirectory, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(dataDirectory);

        _rootPath = Path.GetFullPath(dataDirectory.FullName);
        _rootPathPrefix = Path.EndsInDirectorySeparator(_rootPath)
            ? _rootPath
            : _rootPath + Path.DirectorySeparatorChar;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    Task<Stream> IMessageDataRepository.GetAsync(Uri address, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(address);
        cancellationToken.ThrowIfCancellationRequested();

        StoredFile storedFile = ParseStoredFile(address);
        string fullPath = GetContainedFullPath(storedFile.RelativePath, address);

        if (storedFile.ExpiresAt.HasValue && _timeProvider.GetUtcNow() >= storedFile.ExpiresAt.Value)
        {
            File.Delete(fullPath);
            throw new MessageDataNotFoundException(address);
        }

        if (!File.Exists(fullPath))
            throw new MessageDataNotFoundException(address);

        try
        {
            Stream stream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                DefaultBufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            return Task.FromResult(stream);
        }
        catch (FileNotFoundException)
        {
            throw new MessageDataNotFoundException(address);
        }
        catch (DirectoryNotFoundException)
        {
            throw new MessageDataNotFoundException(address);
        }
    }

    async Task<Uri> IMessageDataRepository.PutAsync(Stream stream, TimeSpan? timeToLive, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        StoredFile storedFile = GenerateStoredFile(timeToLive);
        cancellationToken.ThrowIfCancellationRequested();

        string fullPath = GetContainedFullPath(storedFile.RelativePath, null);

        VerifyDirectory(fullPath);

        try
        {
            await using var fileStream = new FileStream(
                fullPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                DefaultBufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            await stream.CopyToAsync(fileStream, DefaultBufferSize, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            File.Delete(fullPath);
            throw;
        }

        string addressPath = storedFile.RelativePath
            .Replace(Path.DirectorySeparatorChar, ':')
            .Replace(Path.AltDirectorySeparatorChar, ':');
        return new Uri(AddressPrefix + addressPath);
    }

    static void VerifyDirectory(string fullPath)
    {
        var directoryName = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrEmpty(directoryName))
            throw new DirectoryNotFoundException("No directory was found for the file path: " + fullPath);

        Directory.CreateDirectory(directoryName);
    }

    StoredFile GenerateStoredFile(TimeSpan? timeToLive)
    {
        var fileId = FormatUtil.Formatter.Format(NewId.Next().ToSequentialGuid().ToByteArray());

        if (!timeToLive.HasValue || timeToLive.Value == TimeSpan.MaxValue)
            return new StoredFile(Path.Combine("none", fileId), null);
        if (timeToLive.Value < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeToLive), "The retention period cannot be negative.");

        DateTimeOffset expiration;
        try
        {
            expiration = _timeProvider.GetUtcNow().Add(timeToLive.Value);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new ArgumentOutOfRangeException(nameof(timeToLive), timeToLive, "The retention period exceeds the supported UTC range.");
        }

        string relativePath = Path.Combine(
            expiration.Year.ToString("0000", CultureInfo.InvariantCulture),
            expiration.Month.ToString("00", CultureInfo.InvariantCulture),
            expiration.Day.ToString("00", CultureInfo.InvariantCulture),
            expiration.Hour.ToString("00", CultureInfo.InvariantCulture),
            expiration.UtcTicks.ToString(CultureInfo.InvariantCulture),
            fileId);
        return new StoredFile(relativePath, expiration);
    }

    static StoredFile ParseStoredFile(Uri address)
    {
        if (!address.IsAbsoluteUri
            || !string.Equals(address.Scheme, "urn", StringComparison.OrdinalIgnoreCase)
            || !address.OriginalString.StartsWith(AddressPrefix, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The address must use the urn:file scheme.", nameof(address));

        string[] elements = address.OriginalString[AddressPrefix.Length..]
            .Split(':', StringSplitOptions.None)
            .Select(DecodeAndValidateElement)
            .ToArray();

        if (elements.Length == 2 && string.Equals(elements[0], "none", StringComparison.Ordinal))
            return new StoredFile(Path.Combine(elements), null);

        if (elements.Length != 6
            || !long.TryParse(elements[4], NumberStyles.None, CultureInfo.InvariantCulture, out long ticks))
            throw new ArgumentException("The file address has an invalid retention path.", nameof(address));

        DateTimeOffset expiration;
        try
        {
            expiration = new DateTimeOffset(ticks, TimeSpan.Zero);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new ArgumentException("The file address has an invalid expiration value.", nameof(address), exception);
        }

        string[] expectedDateParts =
        [
            expiration.Year.ToString("0000", CultureInfo.InvariantCulture),
            expiration.Month.ToString("00", CultureInfo.InvariantCulture),
            expiration.Day.ToString("00", CultureInfo.InvariantCulture),
            expiration.Hour.ToString("00", CultureInfo.InvariantCulture),
        ];
        if (!elements.Take(4).SequenceEqual(expectedDateParts, StringComparer.Ordinal))
            throw new ArgumentException("The file address expiration path does not match its timestamp.", nameof(address));

        return new StoredFile(Path.Combine(elements), expiration);
    }

    string GetContainedFullPath(string relativePath, Uri? address)
    {
        string fullPath = Path.GetFullPath(Path.Combine(_rootPath, relativePath));
        if (!fullPath.StartsWith(_rootPathPrefix, PathComparison))
            throw new ArgumentException("The file address resolves outside the repository root.", address is null ? "relativePath" : nameof(address));

        return fullPath;
    }

    static string DecodeAndValidateElement(string encodedElement)
    {
        string element = Uri.UnescapeDataString(encodedElement);
        if (string.IsNullOrWhiteSpace(element)
            || element is "." or ".."
            || element.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]) >= 0
            || element.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException("The file address contains an invalid path element.", "address");

        return element;
    }

    static StringComparison PathComparison => OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    sealed record StoredFile(string RelativePath, DateTimeOffset? ExpiresAt);
}

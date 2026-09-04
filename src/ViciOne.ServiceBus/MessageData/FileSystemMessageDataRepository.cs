using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

#nullable enable annotations
namespace ViciOne.ServiceBus.MessageData;

public class FileSystemMessageDataRepository :
    IMessageDataRepository
{
    const int DefaultBufferSize = 4096;
    static readonly char[] _separator = { ':' };
    readonly DirectoryInfo _dataDirectory;
    readonly TimeProvider _timeProvider;

    public FileSystemMessageDataRepository(DirectoryInfo dataDirectory, TimeProvider? timeProvider = null)
    {
        _dataDirectory = dataDirectory;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    Task<Stream> IMessageDataRepository.GetAsync(Uri address, CancellationToken cancellationToken)
    {
        var filePath = ParseFilePath(address);

        var fullPath = Path.Combine(_dataDirectory.FullName, filePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("The file was not found", fullPath);

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize, FileOptions.Asynchronous);

        return Task.FromResult<Stream>(stream);
    }

    async Task<Uri> IMessageDataRepository.PutAsync(Stream stream, TimeSpan? timeToLive, CancellationToken cancellationToken)
    {
        var filePath = GenerateFilePath(timeToLive, _timeProvider);

        var fullPath = Path.Combine(_dataDirectory.FullName, filePath);

        VerifyDirectory(fullPath);

        using var fileStream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read, DefaultBufferSize, FileOptions.Asynchronous);

        await stream.CopyToAsync(fileStream, DefaultBufferSize, cancellationToken).ConfigureAwait(false);

        return new Uri($"urn:file:{filePath.Replace(Path.DirectorySeparatorChar, ':')}");
    }

    static void VerifyDirectory(string fullPath)
    {
        var directoryName = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrEmpty(directoryName))
            throw new DirectoryNotFoundException("No directory was found for the file path: " + fullPath);

        Directory.CreateDirectory(directoryName);
    }

    static string GenerateFilePath(TimeSpan? timeToLive, TimeProvider timeProvider)
    {
        var fileId = FormatUtil.Formatter.Format(NewId.Next().ToSequentialGuid().ToByteArray());

        var expiration = timeToLive.HasValue && timeToLive.Value < TimeSpan.MaxValue && timeToLive >= TimeSpan.Zero
            ? (timeProvider.GetUtcNow().UtcDateTime + timeToLive.Value).ToString("yyyy-MM-dd-HH").Replace('-', Path.DirectorySeparatorChar)
            : "none";

        return Path.Combine(expiration, fileId);
    }

    static string ParseFilePath(Uri address)
    {
        if (address.Scheme != "urn")
            throw new ArgumentException("The address must be a urn");

        var parts = address.Segments[0].Split(_separator);
        if (parts[0] != "file")
            throw new ArgumentException("The address must be a urn:file");

        var length = parts.Length - 1;
        var elements = new string[length];
        Array.Copy(parts, 1, elements, 0, length);

        return Path.Combine(elements);
    }
}

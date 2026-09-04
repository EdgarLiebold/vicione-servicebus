using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.AmazonS3.MessageData;
/// <summary>
/// Stores message payloads in one caller-owned Amazon S3 bucket.
/// </summary>
public sealed class AmazonS3MessageDataRepository :
    IMessageDataRepository,
    IBusObserver
{
    internal const string LifecycleRuleId = "s3-messagedata-rule";

    private readonly IAmazonS3 _client;
    private readonly AmazonS3MessageDataRepositoryOptions _options;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="client">The client value.</param>
    /// <param name="options">The options value.</param>
    public AmazonS3MessageDataRepository(
        IAmazonS3 client,
        AmazonS3MessageDataRepositoryOptions options)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);

        _client = client;
        _options = options;
    }

    /// <summary>
    /// Performs the post create operation.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    public void PostCreate(IBus bus) => ArgumentNullException.ThrowIfNull(bus);

    /// <summary>
    /// Creates faulted.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    public void CreateFaulted(Exception exception) => ArgumentNullException.ThrowIfNull(exception);

    /// <summary>
    /// Performs the pre start operation.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    /// <returns>The result of the operation.</returns>
    public Task PreStartAsync(IBus bus)
    {
        ArgumentNullException.ThrowIfNull(bus);
        return EnsureReadyAsync(CancellationToken.None);
    }

    /// <summary>
    /// Performs the post start operation.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    /// <param name="busReady">The bus ready value.</param>
    /// <returns>The result of the operation.</returns>
    public Task PostStartAsync(IBus bus, Task<BusReady> busReady)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(busReady);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Starts faulted.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task StartFaultedAsync(IBus bus, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(exception);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs the pre stop operation.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    /// <returns>The result of the operation.</returns>
    public Task PreStopAsync(IBus bus)
    {
        ArgumentNullException.ThrowIfNull(bus);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs the post stop operation.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    /// <returns>The result of the operation.</returns>
    public Task PostStopAsync(IBus bus)
    {
        ArgumentNullException.ThrowIfNull(bus);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops faulted.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task StopFaultedAsync(IBus bus, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(exception);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs the get operation.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<Stream> GetAsync(Uri address, CancellationToken cancellationToken = default)
    {
        string objectKey = ParseObjectKey(address);
        using var transfer = new TransferUtility(_client);

        return await transfer
            .OpenStreamAsync(_options.BucketName, objectKey, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the put operation.
    /// </summary>
    /// <param name="stream">The stream value.</param>
    /// <param name="timeToLive">The time to live value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<Uri> PutAsync(
        Stream stream,
        TimeSpan? timeToLive = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead)
            throw new ArgumentException("The message-data stream must be readable.", nameof(stream));

        _options.ValidateTimeToLive(timeToLive);

        string objectKey = FormatUtil.Formatter.Format(NewId.Next().ToSequentialGuid().ToByteArray());
        using var transfer = new TransferUtility(_client);
        await transfer
            .UploadAsync(stream, _options.BucketName, objectKey, cancellationToken)
            .ConfigureAwait(false);

        return new Uri($"urn:file:{objectKey}", UriKind.Absolute);
    }

    internal async Task EnsureReadyAsync(CancellationToken cancellationToken)
    {
        bool bucketExists = await BucketExistsAsync(cancellationToken).ConfigureAwait(false);

        if (!bucketExists)
        {
            try
            {
                await _client.PutBucketAsync(
                        new PutBucketRequest
                        {
                            BucketName = _options.BucketName,
                            BucketRegionName = ClientRegion(),
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (AmazonS3Exception exception) when (
                exception.ErrorCode is "BucketAlreadyOwnedByYou")
            {
                // Another instance completed the same idempotent startup transition.
            }
        }

        if (_options.LifecycleExpirationDays is { } expirationDays)
            await ReconcileOwnedLifecycleRuleAsync(expirationDays, cancellationToken).ConfigureAwait(false);
    }

    private async Task ReconcileOwnedLifecycleRuleAsync(
        int expirationDays,
        CancellationToken cancellationToken)
    {
        List<LifecycleRule> rules;
        try
        {
            GetLifecycleConfigurationResponse response = await _client
                .GetLifecycleConfigurationAsync(
                    new GetLifecycleConfigurationRequest { BucketName = _options.BucketName },
                    cancellationToken)
                .ConfigureAwait(false);

            rules = response.Configuration?.Rules?
                .Select(NormalizeRuleForWrite)
                .ToList() ?? [];
        }
        catch (AmazonS3Exception exception) when (
            exception.StatusCode == HttpStatusCode.NotFound ||
            exception.ErrorCode is "NoSuchLifecycleConfiguration")
        {
            rules = [];
        }

        LifecycleRule[] ownedRules = rules
            .Where(rule => string.Equals(rule.Id, LifecycleRuleId, StringComparison.Ordinal))
            .ToArray();

        if (ownedRules.Length == 1 && IsCurrentOwnedRule(ownedRules[0], expirationDays))
            return;

        int ownedRuleIndex = rules.FindIndex(rule =>
            string.Equals(rule.Id, LifecycleRuleId, StringComparison.Ordinal));
        rules.RemoveAll(rule =>
            string.Equals(rule.Id, LifecycleRuleId, StringComparison.Ordinal));
        rules.Insert(
            ownedRuleIndex < 0 ? rules.Count : ownedRuleIndex,
            new LifecycleRule
            {
                Id = LifecycleRuleId,
                Status = LifecycleRuleStatus.Enabled,
                Filter = AllObjectsFilter(),
                Expiration = new LifecycleRuleExpiration { Days = expirationDays },
            });

        await _client.PutLifecycleConfigurationAsync(
                new PutLifecycleConfigurationRequest
                {
                    BucketName = _options.BucketName,
                    Configuration = new LifecycleConfiguration { Rules = rules },
                },
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static bool IsCurrentOwnedRule(LifecycleRule rule, int expirationDays) =>
        rule.Status == LifecycleRuleStatus.Enabled &&
        IsAllObjectsFilter(rule.Filter) &&
        rule.Expiration?.Days == expirationDays &&
        rule.AbortIncompleteMultipartUpload is null &&
        rule.NoncurrentVersionExpiration is null &&
        rule.NoncurrentVersionTransitions is not { Count: > 0 } &&
        rule.Transitions is not { Count: > 0 };

    private static LifecycleFilter AllObjectsFilter() =>
        new()
        {
            LifecycleFilterPredicate = new LifecyclePrefixPredicate { Prefix = string.Empty },
        };

    private static bool IsAllObjectsFilter(LifecycleFilter? filter) =>
        filter is not null &&
        (filter.LifecycleFilterPredicate is null or LifecyclePrefixPredicate { Prefix: "" });

    private async Task<bool> BucketExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _client.GetBucketAclAsync(
                    new GetBucketAclRequest { BucketName = _options.BucketName },
                    cancellationToken)
                .ConfigureAwait(false);
            return true;
        }
        catch (AmazonS3Exception exception) when (
            exception.StatusCode == HttpStatusCode.NotFound ||
            exception.ErrorCode is "NoSuchBucket")
        {
            return false;
        }
        catch (AmazonS3Exception exception) when (
            exception.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.MovedPermanently ||
            exception.ErrorCode is "AccessDenied" or "PermanentRedirect")
        {
            // Existence is established even when this client must use another endpoint or lacks ACL access.
            return true;
        }
    }

    private static LifecycleRule NormalizeRuleForWrite(LifecycleRule source)
    {
        LifecycleFilter? filter = source.Filter;
        if (filter is null)
        {
            // Prefix is the AWS SDK's only representation of lifecycle rules returned in the older wire format.
#pragma warning disable CS0618 // Reading the deprecated SDK property is required to preserve an existing rule while rewriting it with Filter.
            string prefix = source.Prefix ?? string.Empty;
#pragma warning restore CS0618
            filter = new LifecycleFilter
            {
                LifecycleFilterPredicate = new LifecyclePrefixPredicate { Prefix = prefix },
            };
        }

        return new LifecycleRule
        {
            AbortIncompleteMultipartUpload = source.AbortIncompleteMultipartUpload,
            Expiration = source.Expiration,
            Filter = filter,
            Id = source.Id,
            NoncurrentVersionExpiration = source.NoncurrentVersionExpiration,
            NoncurrentVersionTransitions = source.NoncurrentVersionTransitions,
            Status = source.Status,
            Transitions = source.Transitions,
        };
    }

    private string ClientRegion() =>
        _client.Config.AuthenticationRegion ??
        _client.Config.RegionEndpoint?.SystemName ??
        throw new InvalidOperationException(
            "The Amazon S3 client must own an authentication region before the repository can create a bucket.");

    private static string ParseObjectKey(Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);
        if (!address.IsAbsoluteUri || !address.Scheme.Equals("urn", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The message-data address must be an absolute urn:file URI.", nameof(address));

        const string prefix = "urn:file:";
        string original = address.OriginalString;
        if (!original.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The message-data address must use the urn:file namespace.", nameof(address));

        string objectKey = original[prefix.Length..];
        if (objectKey.Length == 0 ||
            objectKey.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_'))
        {
            throw new ArgumentException(
                "The urn:file object key must contain only ASCII letters, digits, '-' or '_'.",
                nameof(address));
        }

        return objectKey;
    }
}

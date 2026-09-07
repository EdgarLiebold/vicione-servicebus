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
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Observers;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.AmazonS3.MessageData;

/// <summary>Stores message payloads in one caller-owned Amazon S3 bucket.</summary>
public sealed class AmazonS3MessageDataRepository :
    IMessageDataRepository,
    IBusObserver
{
    internal const string LifecycleRuleId = "vicione-servicebus-message-data-expiration";

    private readonly IAmazonS3 _client;
    private readonly AmazonS3MessageDataRepositoryOptions _options;

    /// <summary>Creates a message-data repository that uses the supplied Amazon S3 client and bucket settings.</summary>
    /// <param name="client">The client used for all Amazon S3 operations.</param>
    /// <param name="options">The bucket name and optional lifecycle-expiration policy.</param>
    public AmazonS3MessageDataRepository(
        IAmazonS3 client,
        AmazonS3MessageDataRepositoryOptions options)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);

        _client = client;
        _options = options;
    }

    /// <summary>Observes successful bus creation; this repository requires no post-creation work.</summary>
    /// <param name="bus">The bus that was created.</param>
    public void PostCreate(IBus bus) => ArgumentNullException.ThrowIfNull(bus);

    /// <summary>Observes a bus-creation failure; this repository performs no recovery work.</summary>
    /// <param name="exception">The exception that prevented bus creation.</param>
    public void CreateFaulted(Exception exception) => ArgumentNullException.ThrowIfNull(exception);

    /// <summary>Ensures that the configured bucket and repository-owned lifecycle rule are ready before the bus starts.</summary>
    /// <param name="bus">The bus that is about to start.</param>
    /// <returns>A task that completes after the Amazon S3 resources have been reconciled.</returns>
    public Task PreStartAsync(IBus bus)
    {
        ArgumentNullException.ThrowIfNull(bus);
        return EnsureReadyAsync(CancellationToken.None);
    }

    /// <summary>Observes successful bus startup; all repository initialization occurs before startup.</summary>
    /// <param name="bus">The bus that started.</param>
    /// <param name="busReady">The task that reports the bus readiness result.</param>
    /// <returns>An already-completed task.</returns>
    public Task PostStartAsync(IBus bus, Task<BusReady> busReady)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(busReady);
        return Task.CompletedTask;
    }

    /// <summary>Observes a bus-start failure; this repository performs no recovery work.</summary>
    /// <param name="bus">The bus whose start failed.</param>
    /// <param name="exception">The startup exception.</param>
    /// <returns>An already-completed task.</returns>
    public Task StartFaultedAsync(IBus bus, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(exception);
        return Task.CompletedTask;
    }

    /// <summary>Observes that the bus is about to stop; this repository owns no shutdown work.</summary>
    /// <param name="bus">The bus that is about to stop.</param>
    /// <returns>An already-completed task.</returns>
    public Task PreStopAsync(IBus bus)
    {
        ArgumentNullException.ThrowIfNull(bus);
        return Task.CompletedTask;
    }

    /// <summary>Observes successful bus shutdown; this repository owns no post-stop work.</summary>
    /// <param name="bus">The bus that stopped.</param>
    /// <returns>An already-completed task.</returns>
    public Task PostStopAsync(IBus bus)
    {
        ArgumentNullException.ThrowIfNull(bus);
        return Task.CompletedTask;
    }

    /// <summary>Observes a bus-stop failure; this repository performs no recovery work.</summary>
    /// <param name="bus">The bus whose stop failed.</param>
    /// <param name="exception">The shutdown exception.</param>
    /// <returns>An already-completed task.</returns>
    public Task StopFaultedAsync(IBus bus, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(exception);
        return Task.CompletedTask;
    }

    /// <summary>Opens a readable stream for the Amazon S3 object identified by a message-data address.</summary>
    /// <param name="address">An absolute <c>s3</c> URI containing the configured bucket and repository object key.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task whose result is the readable object stream returned by Amazon S3.</returns>
    public async Task<Stream> GetAsync(Uri address, CancellationToken cancellationToken = default)
    {
        string objectKey = ParseObjectKey(address);
        using var transfer = new TransferUtility(_client);

        return await transfer
            .OpenStreamAsync(_options.BucketName, objectKey, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Uploads a message-data stream under a new object key in the configured bucket.</summary>
    /// <param name="stream">The readable payload stream to upload.</param>
    /// <param name="timeToLive">An optional positive whole-day retention period that must match the configured bucket lifecycle expiration.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task whose result is the generated <c>s3</c> URI of the uploaded object.</returns>
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

        return new Uri($"s3://{_options.BucketName}/{objectKey}", UriKind.Absolute);
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

            rules = response.Configuration?.Rules?.ToList() ?? [];
        }
        catch (AmazonS3Exception exception) when (
            exception.StatusCode == HttpStatusCode.NotFound ||
            exception.ErrorCode is "NoSuchLifecycleConfiguration")
        {
            rules = [];
        }

        LifecycleRule[] ownedRules =
        [
            .. rules.Where(rule => string.Equals(rule.Id, LifecycleRuleId, StringComparison.Ordinal)),
        ];

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

    private string ClientRegion() =>
        _client.Config.AuthenticationRegion ??
        _client.Config.RegionEndpoint?.SystemName ??
        throw new InvalidOperationException(
            "The Amazon S3 client must own an authentication region before the repository can create a bucket.");

    private string ParseObjectKey(Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);
        if (!address.IsAbsoluteUri ||
            !address.Scheme.Equals("s3", StringComparison.OrdinalIgnoreCase) ||
            !address.IdnHost.Equals(_options.BucketName, StringComparison.Ordinal) ||
            !address.IsDefaultPort ||
            address.UserInfo.Length != 0 ||
            address.Query.Length != 0 ||
            address.Fragment.Length != 0)
        {
            throw new ArgumentException(
                "The message-data address must be an absolute s3 URI for the configured bucket.",
                nameof(address));
        }

        string objectKey = address.GetComponents(UriComponents.Path, UriFormat.Unescaped);
        if (objectKey.Length == 0 ||
            objectKey.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_'))
        {
            throw new ArgumentException(
                "The S3 object key must contain only ASCII letters, digits, '-' or '_'.",
                nameof(address));
        }

        return objectKey;
    }
}

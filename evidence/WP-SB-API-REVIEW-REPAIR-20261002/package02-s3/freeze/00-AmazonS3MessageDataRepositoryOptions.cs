using System;
using System.Linq;
using System.Net;

namespace ViciOne.ServiceBus.AmazonS3.MessageData;

/// <summary>Immutable Amazon S3 message-data storage contract.</summary>
public sealed class AmazonS3MessageDataRepositoryOptions
{
    private static readonly string[] ReservedSuffixes =
    [
        "-s3alias",
        "--ol-s3",
        ".mrap",
        "--x-s3",
        "--table-s3",
    ];

    /// <summary>Creates validated settings for one general-purpose Amazon S3 bucket used for message data.</summary>
    /// <param name="bucketName">The DNS-compatible general-purpose bucket name.</param>
    /// <param name="lifecycleExpirationDays">The optional positive whole-day expiration applied by the repository-owned lifecycle rule.</param>
    public AmazonS3MessageDataRepositoryOptions(
        string bucketName,
        int? lifecycleExpirationDays = null)
    {
        ValidateBucketName(bucketName);
        if (lifecycleExpirationDays is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lifecycleExpirationDays),
                lifecycleExpirationDays,
                "Lifecycle expiration must be a positive whole number of days.");
        }

        BucketName = bucketName;
        LifecycleExpirationDays = lifecycleExpirationDays;
    }

    /// <summary>Gets the general-purpose Amazon S3 bucket that stores message data.</summary>
    public string BucketName { get; }

    /// <summary>Gets the whole-day expiration for the repository-owned lifecycle rule, or <see langword="null"/> when no rule is managed.</summary>
    public int? LifecycleExpirationDays { get; }

    internal void ValidateTimeToLive(TimeSpan? timeToLive)
    {
        if (timeToLive is null)
            return;

        if (timeToLive <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeToLive),
                timeToLive,
                "Message-data time to live must be positive.");
        }

        if (timeToLive.Value.Ticks % TimeSpan.TicksPerDay != 0)
        {
            throw new NotSupportedException(
                "Amazon S3 message-data retention supports only whole-day values.");
        }

        long days = timeToLive.Value.Ticks / TimeSpan.TicksPerDay;
        if (LifecycleExpirationDays is null || days != LifecycleExpirationDays.Value)
        {
            throw new NotSupportedException(
                "Per-message retention must equal the bucket lifecycle expiration configured for this repository.");
        }
    }

    private static void ValidateBucketName(string bucketName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bucketName);

        bool hasValidLength = bucketName.Length is >= 3 and <= 63;
        bool hasValidEnds = hasValidLength &&
            IsLowerLetterOrDigit(bucketName[0]) &&
            IsLowerLetterOrDigit(bucketName[^1]);
        bool hasValidCharacters = bucketName.All(
            character => IsLowerLetterOrDigit(character) || character is '-' or '.');
        bool hasValidSeparators =
            !bucketName.Contains("..", StringComparison.Ordinal) &&
            !bucketName.Contains(".-", StringComparison.Ordinal) &&
            !bucketName.Contains("-.", StringComparison.Ordinal);
        bool hasAllowedPrefix =
            !bucketName.StartsWith("xn--", StringComparison.Ordinal) &&
            !bucketName.StartsWith("sthree-", StringComparison.Ordinal) &&
            !bucketName.StartsWith("amzn-s3-demo-", StringComparison.Ordinal);
        bool hasAllowedSuffix = ReservedSuffixes.All(
            suffix => !bucketName.EndsWith(suffix, StringComparison.Ordinal));
        bool isNotIpAddress = !IPAddress.TryParse(bucketName, out _);

        if (!hasValidLength || !hasValidEnds || !hasValidCharacters || !hasValidSeparators ||
            !hasAllowedPrefix || !hasAllowedSuffix || !isNotIpAddress)
        {
            throw new ArgumentException(
                "The bucket name does not satisfy the Amazon S3 general-purpose bucket naming rules.",
                nameof(bucketName));
        }
    }

    private static bool IsLowerLetterOrDigit(char character) =>
        char.IsAsciiLetterLower(character) || char.IsAsciiDigit(character);
}

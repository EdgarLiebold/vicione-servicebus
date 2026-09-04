using System;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>
/// Provides a service bus message entity configurator implementation.
/// </summary>
public abstract class ServiceBusMessageEntityConfigurator :
    ServiceBusEntityConfigurator,
    IServiceBusMessageEntityConfigurator
{
    string? _basePath;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="path">The path value.</param>
    protected ServiceBusMessageEntityConfigurator(string path)
    {
        Path = path;

        AutoDeleteOnIdle = Defaults.AutoDeleteOnIdle;
        DefaultMessageTimeToLive = Defaults.DefaultMessageTimeToLive;
        EnableBatchedOperations = true;
    }

    /// <summary>
    /// Gets or sets the path value.
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// Gets or sets the base path value.
    /// </summary>
    public string? BasePath
    {
        get => _basePath;
        set => _basePath = value?.Trim('/');
    }

    /// <summary>
    /// Gets the full path value.
    /// </summary>
    public string FullPath => string.IsNullOrEmpty(BasePath) ? Path : $"{BasePath}/{Path.Trim('/')}";

    /// <summary>
    /// Gets or sets the duplicate detection history time window value.
    /// </summary>
    public TimeSpan? DuplicateDetectionHistoryTimeWindow { get; set; }

    /// <summary>
    /// Gets or sets the enable partitioning value.
    /// </summary>
    public bool? EnablePartitioning { get; set; }

    /// <summary>
    /// Gets or sets the max size in megabytes value.
    /// </summary>
    public long? MaxSizeInMegabytes { get; set; }

    /// <summary>
    /// Gets or sets the max message size in kilobytes value.
    /// </summary>
    public long? MaxMessageSizeInKilobytes { get; set; }

    /// <summary>
    /// Gets or sets the requires duplicate detection value.
    /// </summary>
    public bool? RequiresDuplicateDetection { get; set; }

    /// <summary>
    /// Performs the enable duplicate detection operation.
    /// </summary>
    /// <param name="historyTimeWindow">The history time window value.</param>
    public void EnableDuplicateDetection(TimeSpan historyTimeWindow)
    {
        RequiresDuplicateDetection = true;
        DuplicateDetectionHistoryTimeWindow = historyTimeWindow;
    }
}

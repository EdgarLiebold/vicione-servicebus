using System;
using System.IO;
using System.Reflection;

namespace ViciOne.ServiceBus.Metadata;

/// <summary>Provides process-wide host metadata and deployment-environment detection.</summary>
public static class HostMetadataCache
{
    static bool? _isRunningInContainer;
    static bool? _isRunningInKubernetes;

    /// <summary>Gets the immutable metadata captured for the current process.</summary>
    public static HostInfo Host => Cached.HostInfo;

    /// <summary>Gets an empty host-metadata value for messages that contain no host information.</summary>
    public static HostInfo Empty => Cached.EmptyHostInfo;

    /// <summary>Gets whether the .NET runtime identifies the process as containerized.</summary>
    public static bool IsRunningInContainer =>
        _isRunningInContainer ??= bool.TryParse(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), out var inDocker) && inDocker;

    /// <summary>Gets whether Kubernetes service discovery or mounted service-account data is present.</summary>
    public static bool IsRunningInKubernetes =>
        _isRunningInKubernetes ??= Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST") != null
            || Directory.Exists("/var/run/secrets/kubernetes.io");

    /// <summary>Gets the source revision suffix embedded in the ServiceBus informational version.</summary>
    /// <returns>The revision suffix after <c>+</c>, or <see langword="null" /> when none is embedded.</returns>
    public static string? GetCommitHash()
    {
        var assembly = typeof(IBus).Assembly;

        var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        if (attribute == null)
            return null;

        int splitIndex = attribute.InformationalVersion.IndexOf('+');
        if (splitIndex > 0)
            return attribute.InformationalVersion[(splitIndex + 1)..];

        return null;
    }
}


static class Cached
{
    internal static readonly HostInfo HostInfo = BusHostInfo.CaptureCurrent().Freeze();
    internal static readonly HostInfo EmptyHostInfo = new BusHostInfo().Freeze();
}

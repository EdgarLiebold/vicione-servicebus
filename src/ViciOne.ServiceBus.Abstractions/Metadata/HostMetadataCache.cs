using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ViciOne.ServiceBus.Metadata;

/// <summary>Caches host metadata data.</summary>
public static class HostMetadataCache
{
    static bool? _isRunningInContainer;
    static bool? _isRunningInKubernetes;

    static bool? _isNetFramework;
    /// <summary>Gets the host.</summary>
    public static HostInfo Host => Cached.HostInfo;
    /// <summary>Gets the empty.</summary>
    public static HostInfo Empty => Cached.EmptyHostInfo;

    /// <summary>Gets a value indicating whether running in container.</summary>
    public static bool IsRunningInContainer =>
        _isRunningInContainer ??= bool.TryParse(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), out var inDocker) && inDocker;

    /// <summary>Gets a value indicating whether kubernetes.</summary>
    public static bool IsKubernetes =>
        _isRunningInKubernetes ??= Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST") != null
            || Directory.Exists("/var/run/secrets/kubernetes.io");

    /// <summary>Gets a value indicating whether net framework.</summary>
    public static bool IsNetFramework => _isNetFramework ??= RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework");

    /// <summary>Gets commit hash.</summary>
    /// <returns>The commit hash.</returns>
    public static string? GetCommitHash()
    {
        var assembly = typeof(IBus).Assembly;

        var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        if (attribute == null)
            return null;

        var splitIndex = attribute.InformationalVersion.IndexOf('+');
        if (splitIndex > 0)
            return attribute.InformationalVersion.Substring(splitIndex + 1);

        return null;
    }
}


static class Cached
{
    internal static readonly HostInfo HostInfo = BusHostInfo.CaptureCurrent();
    internal static readonly HostInfo EmptyHostInfo = new BusHostInfo();
}

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ViciOne.ServiceBus.Metadata;

public static class HostMetadataCache
{
    static bool? _isRunningInContainer;
    static bool? _isRunningInKubernetes;

    static bool? _isNetFramework;
    public static HostInfo Host => Cached.HostInfo;
    public static HostInfo Empty => Cached.EmptyHostInfo;

    public static bool IsRunningInContainer =>
        _isRunningInContainer ??= bool.TryParse(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), out var inDocker) && inDocker;

    public static bool IsKubernetes =>
        _isRunningInKubernetes ??= Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST") != null
            || Directory.Exists("/var/run/secrets/kubernetes.io");

    public static bool IsNetFramework => _isNetFramework ??= RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework");

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

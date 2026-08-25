using System;
using System.IO;
using System.Reflection;

namespace ViciOne.ServiceBus.Metadata;

[Serializable]
public sealed class BusHostInfo : HostInfo
{
    public BusHostInfo()
    {
    }

    public string? MachineName { get; set; }

    public string? ProcessName { get; set; }

    public int ProcessId { get; set; }

    public string? Assembly { get; set; }

    public string? AssemblyVersion { get; set; }

    public string? FrameworkVersion { get; set; }

    public string? ViciOneServiceBusVersion { get; set; }

    public string? OperatingSystemVersion { get; set; }

    internal static BusHostInfo CaptureCurrent()
    {
        System.Reflection.Assembly entryAssembly = System.Reflection.Assembly.GetEntryAssembly()
            ?? typeof(HostMetadataCache).Assembly;
        AssemblyName assemblyName = entryAssembly.GetName();

        return new BusHostInfo
        {
            MachineName = Environment.MachineName,
            ProcessName = GetProcessName(entryAssembly),
            ProcessId = Environment.ProcessId,
            Assembly = assemblyName.Name,
            AssemblyVersion = assemblyName.Version?.ToString() ?? "Unknown",
            FrameworkVersion = Environment.Version.ToString(),
            ViciOneServiceBusVersion = typeof(HostInfo).Assembly.GetName().Version?.ToString(),
            OperatingSystemVersion = Environment.OSVersion.ToString(),
        };
    }

    private static string GetProcessName(System.Reflection.Assembly entryAssembly)
    {
        string? processName = Path.GetFileNameWithoutExtension(Environment.ProcessPath);

        return string.IsNullOrWhiteSpace(processName) || processName.Equals("dotnet", StringComparison.OrdinalIgnoreCase)
            ? entryAssembly.GetName().Name ?? "Unknown"
            : processName;
    }
}

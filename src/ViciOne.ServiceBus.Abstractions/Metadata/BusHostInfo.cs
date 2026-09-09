using System;
using System.IO;
using System.Reflection;

namespace ViciOne.ServiceBus.Metadata;

/// <summary>Represents serializable process and runtime metadata for a message-producing host.</summary>
public sealed class BusHostInfo : HostInfo
{
    /// <summary>Initializes an empty host-metadata value for serialization.</summary>
    public BusHostInfo()
    {
    }

    /// <summary>Gets or sets the machine or container host name.</summary>
    public string? MachineName { get; set; }

    /// <summary>Gets or sets the executable or entry-assembly name.</summary>
    public string? ProcessName { get; set; }

    /// <summary>Gets or sets the operating-system process identifier.</summary>
    public int ProcessId { get; set; }

    /// <summary>Gets or sets the entry-assembly name.</summary>
    public string? Assembly { get; set; }

    /// <summary>Gets or sets the entry-assembly version.</summary>
    public string? AssemblyVersion { get; set; }

    /// <summary>Gets or sets the runtime version.</summary>
    public string? FrameworkVersion { get; set; }

    /// <summary>Gets or sets the ViciOne.ServiceBus assembly version.</summary>
    public string? ViciOneServiceBusVersion { get; set; }

    /// <summary>Gets or sets the operating-system version description.</summary>
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

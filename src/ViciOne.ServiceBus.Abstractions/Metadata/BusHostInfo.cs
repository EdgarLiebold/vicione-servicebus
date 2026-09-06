using System;
using System.IO;
using System.Reflection;

namespace ViciOne.ServiceBus.Metadata;

/// <summary>Carries diagnostic information for bus host.</summary>
public sealed class BusHostInfo : HostInfo
{
    /// <summary>Initializes a new instance.</summary>
    public BusHostInfo()
    {
    }

    /// <summary>Gets or sets the machine name.</summary>
    public string? MachineName { get; set; }

    /// <summary>Gets or sets the process name.</summary>
    public string? ProcessName { get; set; }

    /// <summary>Gets or sets the process id.</summary>
    public int ProcessId { get; set; }

    /// <summary>Gets or sets the assembly.</summary>
    public string? Assembly { get; set; }

    /// <summary>Gets or sets the assembly version.</summary>
    public string? AssemblyVersion { get; set; }

    /// <summary>Gets or sets the framework version.</summary>
    public string? FrameworkVersion { get; set; }

    /// <summary>Gets or sets the vici one service bus version.</summary>
    public string? ViciOneServiceBusVersion { get; set; }

    /// <summary>Gets or sets the operating system version.</summary>
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

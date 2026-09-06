using System;
using System.IO;
using System.Reflection;

namespace ViciOne.ServiceBus.Metadata;

/// <summary>
/// Provides a bus host info implementation.
/// </summary>
public sealed class BusHostInfo : HostInfo
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public BusHostInfo()
    {
    }

    /// <summary>
    /// Gets or sets the machine name value.
    /// </summary>
    public string? MachineName { get; set; }

    /// <summary>
    /// Gets or sets the process name value.
    /// </summary>
    public string? ProcessName { get; set; }

    /// <summary>
    /// Gets or sets the process id value.
    /// </summary>
    public int ProcessId { get; set; }

    /// <summary>
    /// Gets or sets the assembly value.
    /// </summary>
    public string? Assembly { get; set; }

    /// <summary>
    /// Gets or sets the assembly version value.
    /// </summary>
    public string? AssemblyVersion { get; set; }

    /// <summary>
    /// Gets or sets the framework version value.
    /// </summary>
    public string? FrameworkVersion { get; set; }

    /// <summary>
    /// Gets or sets the vici one service bus version value.
    /// </summary>
    public string? ViciOneServiceBusVersion { get; set; }

    /// <summary>
    /// Gets or sets the operating system version value.
    /// </summary>
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

using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ViciOne.ServiceBus.Metadata;

/// <summary>Represents serializable process and runtime metadata for a message-producing host.</summary>
/// <remarks>New wire values remain mutable. Instances returned by <see cref="HostMetadataCache"/> are read-only cached snapshots.</remarks>
public sealed class BusHostInfo : HostInfo
{
    static readonly ConditionalWeakTable<BusHostInfo, ReadOnlyState> ReadOnlyInstances = new();

    /// <summary>Initializes an empty host-metadata value for serialization.</summary>
    public BusHostInfo()
    {
    }

    /// <summary>Gets or sets the machine or container host name.</summary>
    /// <exception cref="InvalidOperationException">The instance is a read-only cached host snapshot.</exception>
    public string? MachineName
    {
        get;
        set
        {
            ThrowIfReadOnly();
            field = value;
        }
    }

    /// <summary>Gets or sets the executable or entry-assembly name.</summary>
    /// <exception cref="InvalidOperationException">The instance is a read-only cached host snapshot.</exception>
    public string? ProcessName
    {
        get;
        set
        {
            ThrowIfReadOnly();
            field = value;
        }
    }

    /// <summary>Gets or sets the operating-system process identifier.</summary>
    /// <exception cref="InvalidOperationException">The instance is a read-only cached host snapshot.</exception>
    public int ProcessId
    {
        get;
        set
        {
            ThrowIfReadOnly();
            field = value;
        }
    }

    /// <summary>Gets or sets the entry-assembly name.</summary>
    /// <exception cref="InvalidOperationException">The instance is a read-only cached host snapshot.</exception>
    public string? Assembly
    {
        get;
        set
        {
            ThrowIfReadOnly();
            field = value;
        }
    }

    /// <summary>Gets or sets the entry-assembly version.</summary>
    /// <exception cref="InvalidOperationException">The instance is a read-only cached host snapshot.</exception>
    public string? AssemblyVersion
    {
        get;
        set
        {
            ThrowIfReadOnly();
            field = value;
        }
    }

    /// <summary>Gets or sets the runtime version.</summary>
    /// <exception cref="InvalidOperationException">The instance is a read-only cached host snapshot.</exception>
    public string? FrameworkVersion
    {
        get;
        set
        {
            ThrowIfReadOnly();
            field = value;
        }
    }

    /// <summary>Gets or sets the ViciOne.ServiceBus assembly version.</summary>
    /// <exception cref="InvalidOperationException">The instance is a read-only cached host snapshot.</exception>
    public string? ViciOneServiceBusVersion
    {
        get;
        set
        {
            ThrowIfReadOnly();
            field = value;
        }
    }

    /// <summary>Gets or sets the operating-system version description.</summary>
    /// <exception cref="InvalidOperationException">The instance is a read-only cached host snapshot.</exception>
    public string? OperatingSystemVersion
    {
        get;
        set
        {
            ThrowIfReadOnly();
            field = value;
        }
    }

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

    internal BusHostInfo Freeze()
    {
        ReadOnlyInstances.GetValue(this, static _ => new ReadOnlyState());
        return this;
    }

    void ThrowIfReadOnly()
    {
        if (ReadOnlyInstances.TryGetValue(this, out _))
            throw new InvalidOperationException("Cached host metadata is read-only.");
    }

    sealed class ReadOnlyState;

    private static string GetProcessName(System.Reflection.Assembly entryAssembly)
    {
        string? processName = Path.GetFileNameWithoutExtension(Environment.ProcessPath);

        return string.IsNullOrWhiteSpace(processName) || processName.Equals("dotnet", StringComparison.OrdinalIgnoreCase)
            ? entryAssembly.GetName().Name ?? "Unknown"
            : processName;
    }
}

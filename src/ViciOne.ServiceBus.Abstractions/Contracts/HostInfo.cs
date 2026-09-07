namespace ViciOne.ServiceBus;

/// <summary>Describes the process and runtime that emitted a message or diagnostic event.</summary>
public interface HostInfo
{
    /// <summary>Gets the machine or role-instance name of the host.</summary>
    string? MachineName { get; }

    /// <summary>Gets the name of the hosting process.</summary>
    string? ProcessName { get; }

    /// <summary>Gets the identifier of the hosting process.</summary>
    int ProcessId { get; }

    /// <summary>Gets the entry assembly name.</summary>
    string? Assembly { get; }

    /// <summary>Gets the entry assembly version.</summary>
    string? AssemblyVersion { get; }

    /// <summary>Gets the .NET runtime version reported by the host.</summary>
    string? FrameworkVersion { get; }

    /// <summary>Gets the ViciOne.ServiceBus version loaded by the process.</summary>
    string? ViciOneServiceBusVersion { get; }

    /// <summary>Gets the operating-system description reported by the host.</summary>
    string? OperatingSystemVersion { get; }
}

using System;
using System.Diagnostics;

namespace ViciOne.ServiceBus.NewIdProviders;

/// <summary>Provides current process id services.</summary>
public class CurrentProcessIdProvider :
    IProcessIdProvider
{
    /// <summary>Gets process id.</summary>
    /// <returns>The process id.</returns>
    public byte[] GetProcessId()
    {
        var processId = BitConverter.GetBytes(Process.GetCurrentProcess().Id);

        if (processId.Length < 2)
            throw new InvalidOperationException("Current Process Id is of insufficient length");

        return processId;
    }
}

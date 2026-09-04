using System;

namespace ViciOne.ServiceBus.Util.Scanning;

public class AssemblyScanRecord
{
    public Exception LoadException;
    public string Name;

    public override string ToString()
    {
        return LoadException == null ? Name : $"{Name} (Failed)";
    }
}

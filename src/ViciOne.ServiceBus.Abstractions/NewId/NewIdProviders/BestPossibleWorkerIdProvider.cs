using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.NewIdProviders;

/// <summary>Provides best possible worker id services.</summary>
public class BestPossibleWorkerIdProvider :
    IWorkerIdProvider
{
    /// <summary>Gets worker id.</summary>
    /// <param name="index">The index.</param>
    /// <returns>The worker id.</returns>
    public byte[] GetWorkerId(int index)
    {
        var exceptions = new List<Exception>();

        try
        {
            return new NetworkAddressWorkerIdProvider().GetWorkerId(index);
        }
        catch (Exception ex)
        {
            exceptions.Add(ex);
        }

        try
        {
            return new HostNameHashWorkerIdProvider().GetWorkerId(index);
        }
        catch (Exception ex)
        {
            exceptions.Add(ex);
        }

        throw new AggregateException(exceptions);
    }
}

using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.NewIdProviders;

/// <summary>
/// Provides a best possible worker id provider implementation.
/// </summary>
public class BestPossibleWorkerIdProvider :
    IWorkerIdProvider
{
    /// <summary>
    /// Gets worker id.
    /// </summary>
    /// <param name="index">The index value.</param>
    /// <returns>The result of the operation.</returns>
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

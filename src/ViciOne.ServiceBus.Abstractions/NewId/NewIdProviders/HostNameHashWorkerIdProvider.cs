using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace ViciOne.ServiceBus.NewIdProviders;

/// <summary>Provides host name hash worker id services.</summary>
public class HostNameHashWorkerIdProvider :
    IWorkerIdProvider
{
    /// <summary>Gets worker id.</summary>
    /// <param name="index">The index.</param>
    /// <returns>The worker id.</returns>
    public byte[] GetWorkerId(int index)
    {
        return GetNetworkAddress();
    }

    static byte[] GetNetworkAddress()
    {
        try
        {
            var hostName = Dns.GetHostName();

            byte[] hash;
            using (var hasher = SHA1.Create())
            {
                hash = hasher.ComputeHash(Encoding.UTF8.GetBytes(hostName));
            }

            var bytes = new byte[6];
            Buffer.BlockCopy(hash, 12, bytes, 0, 6);
            bytes[0] |= 0x80;

            return bytes;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Unable to retrieve hostname", ex);
        }
    }
}

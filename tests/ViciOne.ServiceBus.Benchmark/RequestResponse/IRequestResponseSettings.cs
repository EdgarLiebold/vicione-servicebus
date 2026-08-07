// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOneServiceBusBenchmark.RequestResponse
{
    using System;


    public interface IRequestResponseSettings
    {
        long MessageCount { get; }

        int ConcurrencyLimit { get; }

        ushort PrefetchCount { get; }

        bool Durable { get; }

        int Clients { get; }

        TimeSpan RequestTimeout { get; }
    }
}

// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOneServiceBusBenchmark
{
    using System;
    using NDesk.Options;


    public class InMemoryOptionSet :
        OptionSet
    {
        public InMemoryOptionSet()
        {
            Add<int>("limit=", "The transport concurrency limit", x => TransportConcurrencyLimit = x);

            TransportConcurrencyLimit = Environment.ProcessorCount;
        }

        public int TransportConcurrencyLimit { get; private set; }

        public void ShowOptions()
        {
            Console.WriteLine("Transport Concurrency Limit: {0}", TransportConcurrencyLimit);
        }
    }
}

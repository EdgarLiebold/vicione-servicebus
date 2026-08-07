// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Monitoring.Performance.StatsD
{
    public class StatsDConfiguration
    {
        public StatsDConfiguration(string hostname, int port)
        {
            Hostname = hostname;
            Port = port;
        }

        public string Hostname { get; set; }
        public int Port { get; set; }

        public static StatsDConfiguration Defaults()
        {
            return new StatsDConfiguration("localhost", 8125);
        }
    }
}

namespace ViciOne.ServiceBus.ActiveMqTransport.Configuration
{
    using System;
    using System.Collections.Generic;


    public class ActiveMqHostConfigurator :
        IActiveMqHostConfigurator
    {
        readonly ConfigurationHostSettings _settings;

        public ActiveMqHostConfigurator(Uri address)
        {
            switch (address.Scheme.ToLowerInvariant())
            {
                case ActiveMqHostAddress.AmqpScheme:
                    _settings = new AmqpHostSettings(address);
                    break;
                default:
                    _settings = new OpenWireHostSettings(address);
                    break;
            }
        }

        public ActiveMqHostSettings Settings => _settings;

        public void Username(string username)
        {
            _settings.Username = username;
        }

        public void Password(string password)
        {
            _settings.Password = password;
        }

        public void UseSsl(bool enabled = true)
        {
            _settings.UseSsl = enabled;
        }

        public void FailoverHosts(params Uri[] hosts)
        {
            ArgumentNullException.ThrowIfNull(hosts);
            _settings.FailoverHosts = Array.AsReadOnly((Uri[])hosts.Clone());
        }

        public void TransportOptions(IEnumerable<KeyValuePair<string, string>> options)
        {
            ArgumentNullException.ThrowIfNull(options);

            foreach (KeyValuePair<string, string> option in options)
            {
                if (string.IsNullOrWhiteSpace(option.Key))
                    throw new ArgumentException("An ActiveMQ transport option key must not be empty or whitespace.", nameof(options));
                if (option.Value == null)
                    throw new ArgumentException($"The ActiveMQ transport option '{option.Key}' must have a value.", nameof(options));
                if (!_settings.TransportOptions.TryAdd(option.Key, option.Value))
                    throw new ArgumentException($"The ActiveMQ transport option '{option.Key}' was specified more than once.", nameof(options));
            }
        }

        public void EnableAsyncSend()
        {
            _settings.TransportOptions["nms.AsyncSend"] = "true";
        }

        public void EnableOptimizeAcknowledge()
        {
            _settings.TransportOptions["jms.optimizeAcknowledge"] = "true";
        }

        public void SetPrefetchPolicy(int limit)
        {
            _settings.TransportOptions["jms.prefetchPolicy.all"] = limit.ToString();
        }

        public void SetQueuePrefetchPolicy(int limit)
        {
            _settings.TransportOptions["jms.prefetchPolicy.queuePrefetch"] = limit.ToString();
        }
    }
}

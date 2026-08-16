#nullable enable
namespace ViciOne.ServiceBus.Logging
{
    using Microsoft.Extensions.Logging;


    public class SingleLoggerFactory :
        ILoggerFactory
    {
        readonly ILogger _logger;

        public SingleLoggerFactory(ILogger logger)
        {
            _logger = logger;
        }

        public ILogger CreateLogger(string name)
        {
            return _logger;
        }

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public void Dispose()
        {
        }
    }
}

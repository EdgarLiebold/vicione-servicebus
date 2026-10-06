using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Logging;

namespace LoggingLifetime;

public sealed class DelegatingContext(ILogContext inner) : ILogContext
{
    public ILogger Logger => inner.Logger;
    public ILogContext Messages => inner.Messages;
    public EnabledLogger? Critical => inner.Critical;
    public EnabledLogger? Debug => inner.Debug;
    public EnabledLogger? Error => inner.Error;
    public EnabledLogger? Info => inner.Info;
    public EnabledLogger? Trace => inner.Trace;
    public EnabledLogger? Warning => inner.Warning;
    public ILogContext CreateLogContext(string categoryName) => inner.CreateLogContext(categoryName);
}

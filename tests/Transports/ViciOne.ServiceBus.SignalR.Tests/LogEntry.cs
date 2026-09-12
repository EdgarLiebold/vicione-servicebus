using Microsoft.Extensions.Logging;

namespace ViciOne.ServiceBus.SignalR.Tests;

internal sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);

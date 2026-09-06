namespace ViciOne.ServiceBus.Providers.Persistence;

internal sealed class ReliableInboxRetryRequiredException(Exception innerException) :
    Exception("The reliable inbox retained the failed attempt and requires a fresh pipeline invocation.", innerException);

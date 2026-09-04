using System;

namespace ViciOne.ServiceBus;

[Serializable]
public abstract class AbstractUriException :
    ViciOneServiceBusException
{
    protected AbstractUriException()
    {
    }

    protected AbstractUriException(Uri uri)
    {
        Uri = uri;
    }

    protected AbstractUriException(Uri uri, string message)
        : base($"{uri} => {message}")
    {
        Uri = uri;
    }

    protected AbstractUriException(Uri uri, string message, Exception innerException)
        : base($"{uri} => {message}", innerException)
    {
        Uri = uri;
    }

    public Uri? Uri { get; protected set; }
}

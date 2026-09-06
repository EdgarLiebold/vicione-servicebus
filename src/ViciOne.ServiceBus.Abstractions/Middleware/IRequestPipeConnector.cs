using System;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Connect a request pipe to the pipeline.</summary>
public interface IRequestPipeConnector
{
    /// <summary>Connect the consume pipe to the pipeline for messages with the specified RequestId header.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="requestId">The request id.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectRequestPipe<T>(Guid requestId, IPipe<ConsumeContext<T>> pipe)
        where T : class;
}

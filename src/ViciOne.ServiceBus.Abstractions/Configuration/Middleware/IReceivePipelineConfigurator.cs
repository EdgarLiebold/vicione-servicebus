using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides access to transport receive, dead-letter, error, and transport middleware.</summary>
public interface IReceivePipelineConfigurator
{
    /// <summary>Configures middleware that processes transport receive contexts.</summary>
    /// <param name="callback">The callback that adds receive middleware.</param>
    void ConfigureReceive(Action<IReceivePipeConfigurator> callback);

    /// <summary>Configures middleware invoked when a received message is not consumed.</summary>
    /// <param name="callback">The callback that adds dead-letter middleware.</param>
    void ConfigureDeadLetter(Action<IPipeConfigurator<ReceiveContext>> callback);

    /// <summary>Configures middleware invoked for unhandled receive or consumer exceptions.</summary>
    /// <param name="callback">The callback that adds error middleware.</param>
    void ConfigureError(Action<IPipeConfigurator<ExceptionReceiveContext>> callback);

    /// <summary>Configures transport delivery capacity.</summary>
    /// <param name="callback">The callback that configures transport limits.</param>
    void ConfigureTransport(Action<ITransportConfigurator> callback);
}

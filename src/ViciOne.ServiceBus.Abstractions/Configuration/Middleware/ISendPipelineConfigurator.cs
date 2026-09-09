using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides access to middleware applied while messages are sent.</summary>
public interface ISendPipelineConfigurator
{
    /// <summary>Configures the send pipeline.</summary>
    /// <param name="callback">The callback that adds send middleware.</param>
    void ConfigureSend(Action<ISendPipeConfigurator> callback);
}

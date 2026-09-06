using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures send pipeline.</summary>
public interface ISendPipelineConfigurator
{
    /// <summary>Configure the Send pipeline.</summary>
    /// <param name="callback">The callback invoked by the operation.</param>
    void ConfigureSend(Action<ISendPipeConfigurator> callback);
}

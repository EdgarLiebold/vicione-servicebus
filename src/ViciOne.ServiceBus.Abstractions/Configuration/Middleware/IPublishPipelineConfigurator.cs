using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures publish pipeline.</summary>
public interface IPublishPipelineConfigurator
{
    /// <summary>Configure the Publish pipeline.</summary>
    /// <param name="callback">The callback invoked by the operation.</param>
    void ConfigurePublish(Action<IPublishPipeConfigurator> callback);
}

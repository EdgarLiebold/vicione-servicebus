using System;
using System.Collections.Generic;
using System.Net.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>
/// Provides a rabbit mq registration bus factory implementation.
/// </summary>
public class RabbitMqRegistrationBusFactory :
    TransportRegistrationBusFactory<IRabbitMqReceiveEndpointConfigurator>
{
    readonly RabbitMqBusConfiguration _busConfiguration;
    readonly Action<IBusRegistrationContext, IRabbitMqBusFactoryConfigurator> _configure;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    public RabbitMqRegistrationBusFactory(Action<IBusRegistrationContext, IRabbitMqBusFactoryConfigurator>? configure)
        : this(new RabbitMqBusConfiguration(new RabbitMqTopologyConfiguration(RabbitMqBusFactory.CreateMessageTopology())), configure)
    {
    }

    RabbitMqRegistrationBusFactory(RabbitMqBusConfiguration busConfiguration,
        Action<IBusRegistrationContext, IRabbitMqBusFactoryConfigurator>? configure)
        : base(busConfiguration.HostConfiguration)
    {
        _configure = configure ?? ((_, _) => { });

        _busConfiguration = busConfiguration;
    }

    /// <summary>
    /// Creates bus.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="specifications">The specifications value.</param>
    /// <param name="busName">The bus name value.</param>
    /// <returns>The result of the operation.</returns>
    public override IBusInstance CreateBus(IBusRegistrationContext context, IEnumerable<IBusInstanceSpecification> specifications, string busName)
    {
        var configurator = new RabbitMqBusFactoryConfigurator(_busConfiguration);

        var options = context.GetRequiredService<IOptionsMonitor<RabbitMqTransportOptions>>().Get(busName);

        configurator.Host(options.Host, options.Port, options.VHost, options.ConnectionName, h =>
        {
            if (!string.IsNullOrWhiteSpace(options.User))
                h.Username(options.User);

            if (!string.IsNullOrWhiteSpace(options.Pass))
                h.Password(options.Pass);

            if (options.UseSsl)
            {
                h.UseSsl(s =>
                {
                    var sslOptions = context.GetRequiredService<IOptionsMonitor<RabbitMqSslOptions>>().Get(busName);

                    if (!string.IsNullOrWhiteSpace(sslOptions.ServerName))
                        s.ServerName = sslOptions.ServerName;
                    if (!string.IsNullOrWhiteSpace(sslOptions.CertPath))
                        s.CertificatePath = sslOptions.CertPath;
                    if (!string.IsNullOrWhiteSpace(sslOptions.CertPassphrase))
                        s.CertificatePassphrase = sslOptions.CertPassphrase;
                    s.UseCertificateAsAuthenticationIdentity = sslOptions.CertIdentity;

                    s.Protocol = sslOptions.Protocol;

                    if (sslOptions.Trust)
                    {
                        s.AllowPolicyErrors(SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateChainErrors
                            | SslPolicyErrors.RemoteCertificateNotAvailable);
                    }
                });
            }
        });

        return CreateBus(configurator, context, _configure, specifications);
    }
}

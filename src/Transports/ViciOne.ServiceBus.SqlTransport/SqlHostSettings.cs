using System;
using System.Data;
using ViciOne.ServiceBus.SqlTransport;
using ViciOne.ServiceBus.SqlTransport.Configuration;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Settings to configure a DbTransport host explicitly without requiring the fluent interface.</summary>
public interface SqlHostSettings :
    ISpecification
{
    /// <summary>Gets the host address.</summary>
    Uri HostAddress { get; }

    /// <summary>Gets the connection tag.</summary>
    string? ConnectionTag { get; }

    /// <summary>Gets the virtual host.</summary>
    string? VirtualHost { get; }
    /// <summary>Gets the area.</summary>
    string? Area { get; }

    /// <summary>Gets the isolation level.</summary>
    IsolationLevel IsolationLevel { get; }

    /// <summary>Gets the connection limit.</summary>
    int ConnectionLimit { get; }

    /// <summary>Gets the maintenance enabled.</summary>
    bool MaintenanceEnabled { get; }
    /// <summary>Gets the maintenance interval.</summary>
    TimeSpan MaintenanceInterval { get; }
    /// <summary>Gets the queue cleanup interval.</summary>
    TimeSpan QueueCleanupInterval { get; }
    /// <summary>Gets the maintenance batch size.</summary>
    int MaintenanceBatchSize { get; }

    /// <summary>Creates connection context factory.</summary>
    /// <param name="configuration">The SQL host configuration used to create the connection context factory.</param>
    /// <returns>The created connection context factory.</returns>
    ConnectionContextFactory CreateConnectionContextFactory(ISqlHostConfiguration configuration);

}

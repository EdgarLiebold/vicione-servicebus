#nullable enable
namespace ViciOne.ServiceBus
{
    using System;
    using System.Data;
    using SqlTransport;
    using SqlTransport.Configuration;


    /// <summary>
    /// Settings to configure a DbTransport host explicitly without requiring the fluent interface
    /// </summary>
    public interface SqlHostSettings :
        ISpecification
    {
        Uri HostAddress { get; }

        string? ConnectionTag { get; }

        string? VirtualHost { get; }
        string? Area { get; }

        IsolationLevel IsolationLevel { get; }

        int ConnectionLimit { get; }

        bool MaintenanceEnabled { get; }
        TimeSpan MaintenanceInterval { get; }
        TimeSpan QueueCleanupInterval { get; }
        int MaintenanceBatchSize { get; }

        ConnectionContextFactory CreateConnectionContextFactory(ISqlHostConfiguration configuration);

    }
}

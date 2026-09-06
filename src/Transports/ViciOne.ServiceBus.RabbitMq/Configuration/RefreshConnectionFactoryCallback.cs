using System.Threading.Tasks;
using RabbitMQ.Client;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Refreshes RabbitMQ client-factory settings immediately before a connection attempt.</summary>
/// <param name="connectionFactory">The factory that will create the connection.</param>
/// <returns>A task that completes when the settings have been refreshed.</returns>
public delegate Task RefreshConnectionFactoryCallback(ConnectionFactory connectionFactory);

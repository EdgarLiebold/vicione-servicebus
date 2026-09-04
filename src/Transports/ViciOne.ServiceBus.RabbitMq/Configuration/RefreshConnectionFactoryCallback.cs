using System.Threading.Tasks;
using RabbitMQ.Client;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Represents the method that handles refresh connection factory callback.
/// </summary>
/// <param name="connectionFactory">The connection factory value.</param>
/// <returns>The result of the operation.</returns>
public delegate Task RefreshConnectionFactoryCallback(ConnectionFactory connectionFactory);

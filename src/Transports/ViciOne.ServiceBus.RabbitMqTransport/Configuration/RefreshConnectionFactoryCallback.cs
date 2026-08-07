// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus;

using System.Threading.Tasks;
using RabbitMQ.Client;


public delegate Task RefreshConnectionFactoryCallback(ConnectionFactory connectionFactory);

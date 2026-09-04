using System.Threading.Tasks;
using RabbitMQ.Client;

namespace ViciOne.ServiceBus;

public delegate Task RefreshConnectionFactoryCallback(ConnectionFactory connectionFactory);

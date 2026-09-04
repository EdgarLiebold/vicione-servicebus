using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

public interface ISendContextPipe
{
    Task Send<T>(SendContext<T> context)
        where T : class;
}

#nullable enable
namespace ViciOne.ServiceBus.SqlTransport
{
    using System;
    using System.Threading.Tasks;
    using Transports;


    public interface IConnectionContextSupervisor :
        ITransportSupervisor<ConnectionContext>
    {
        Task<ISendTransport> CreateSendTransport(SqlReceiveEndpointContext context, Uri address);

        Task<ISendTransport> CreatePublishTransport<T>(SqlReceiveEndpointContext context, Uri? publishAddress)
            where T : class;

        Uri NormalizeAddress(Uri address);
    }
}

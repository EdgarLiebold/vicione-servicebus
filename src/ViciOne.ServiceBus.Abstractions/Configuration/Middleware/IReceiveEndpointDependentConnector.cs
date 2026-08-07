// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using Transports;


    public interface IReceiveEndpointDependentConnector
    {
        /// <summary>
        /// Add the dependent to receive endpoint. Receive endpoint will be stopped when dependent is Completed
        /// </summary>
        /// <param name="dependent"></param>
        void AddDependent(IReceiveEndpointDependent dependent);
    }
}

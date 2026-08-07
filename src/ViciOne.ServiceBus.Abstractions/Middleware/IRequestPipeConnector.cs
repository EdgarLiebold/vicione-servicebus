// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;


    /// <summary>
    /// Connect a request pipe to the pipeline
    /// </summary>
    public interface IRequestPipeConnector
    {
        /// <summary>
        /// Connect the consume pipe to the pipeline for messages with the specified RequestId header
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="requestId"></param>
        /// <param name="pipe"></param>
        /// <returns></returns>
        ConnectHandle ConnectRequestPipe<T>(Guid requestId, IPipe<ConsumeContext<T>> pipe)
            where T : class;
    }
}

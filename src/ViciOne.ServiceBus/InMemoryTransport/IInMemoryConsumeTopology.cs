// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using Configuration;


    public interface IInMemoryConsumeTopology :
        IConsumeTopology
    {
        new IInMemoryMessageConsumeTopology<T> GetMessageTopology<T>()
            where T : class;

        /// <summary>
        /// Apply the entire topology to the builder
        /// </summary>
        /// <param name="builder"></param>
        void Apply(IMessageFabricConsumeTopologyBuilder builder);
    }
}

// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    /// <summary>
    /// Used to visit the state machine structure, so it can be displayed, etc.
    /// </summary>
    public interface IVisitable :
        IProbeSite
    {
        /// <summary>
        /// A visitable site can accept the visitor and pass control to internal elements
        /// </summary>
        /// <param name="visitor"></param>
        void Accept(StateMachineVisitor visitor);
    }
}

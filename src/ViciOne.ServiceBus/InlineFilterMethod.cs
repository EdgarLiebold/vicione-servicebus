// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System.Threading.Tasks;


    public delegate Task InlineFilterMethod<T>(T context, IPipe<T> next)
        where T : class, PipeContext;
}

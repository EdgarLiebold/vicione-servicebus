// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Middleware
{
    public class PipeRouter :
        DynamicRouter<PipeContext>,
        IPipeRouter
    {
        public PipeRouter()
            : base(new PipeContextConverterFactory())
        {
        }
    }
}

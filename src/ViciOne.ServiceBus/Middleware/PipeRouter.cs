namespace ViciOne.ServiceBus.Middleware;

/// <summary>Routes pipe operations.</summary>
public class PipeRouter :
    DynamicRouter<PipeContext>,
    IPipeRouter
{
    /// <summary>Initializes a new instance.</summary>
    public PipeRouter()
        : base(new PipeContextConverterFactory())
    {
    }
}

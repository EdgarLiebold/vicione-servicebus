namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Provides a pipe router implementation.
/// </summary>
public class PipeRouter :
    DynamicRouter<PipeContext>,
    IPipeRouter
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public PipeRouter()
        : base(new PipeContextConverterFactory())
    {
    }
}

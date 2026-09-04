using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

public delegate Task InlineFilterMethod<T>(T context, IPipe<T> next)
    where T : class, PipeContext;

using System.Reflection;

namespace ViciOne.ServiceBus.Tests.JobService.Api;

internal static class ResponseFactory
{
    public static Response<TResponse> Create<TResponse>(TResponse message)
        where TResponse : class
    {
        Response<TResponse> response = DispatchProxy.Create<Response<TResponse>, ResponseProxy<TResponse>>();
        ((ResponseProxy<TResponse>)(object)response).Message = message;
        return response;
    }

    private class ResponseProxy<TResponse> : DispatchProxy
        where TResponse : class
    {
        public TResponse Message { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "get_Message")
                return Message;

            throw new NotSupportedException($"Member '{targetMethod?.Name}' is not supported by this test response.");
        }
    }
}

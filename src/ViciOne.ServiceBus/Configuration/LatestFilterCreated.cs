namespace ViciOne.ServiceBus
{
    using Middleware;


    public delegate void LatestFilterCreated<T>(ILatestFilter<T> filter)
        where T : class, PipeContext;
}

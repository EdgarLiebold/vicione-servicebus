using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus;

public delegate void LatestFilterCreated<T>(ILatestFilter<T> filter)
    where T : class, PipeContext;

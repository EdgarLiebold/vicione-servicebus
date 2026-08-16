namespace ViciOne.ServiceBus.Configuration
{
    public delegate IRetryPolicy RetryPolicyFactory(IExceptionFilter filter);
}

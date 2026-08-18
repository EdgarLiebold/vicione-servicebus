namespace ViciOne.ServiceBus.TestInfrastructure
{
    using System;


    /// <summary>
    /// Raised when a fixture is asked for an endpoint the runner did not publish completely. It is raised
    /// before any connection attempt, so a missing contract reads as a missing contract instead of as a
    /// refused connection.
    /// </summary>
    public sealed class TestRunnerContractException :
        Exception
    {
        public TestRunnerContractException(string message)
            : base(message)
        {
        }
    }
}

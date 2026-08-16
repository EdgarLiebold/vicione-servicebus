namespace ViciOne.ServiceBus.Contracts.JobService
{
    public enum JobSlotDisposition
    {
        Completed = 0,
        Faulted = 1,
        Canceled = 2,
        Suspect = 3,
    }
}

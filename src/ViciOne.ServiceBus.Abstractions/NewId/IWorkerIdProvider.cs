namespace ViciOne.ServiceBus;

public interface IWorkerIdProvider
{
    byte[] GetWorkerId(int index);
}

namespace ViciOne.ServiceBus.Testing.Internal;

interface ITestContextRetention
{
    void ConfigureRetention(TestContextSaveMode saveMode, int maximumSavedElements);
}

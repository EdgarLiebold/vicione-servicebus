#nullable enable
namespace ViciOne.ServiceBus.Testing.Implementations;

interface ITestContextRetention
{
    void ConfigureRetention(TestContextSaveMode saveMode, int maximumSavedElements);
}

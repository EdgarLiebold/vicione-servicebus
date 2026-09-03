#nullable enable

namespace ViciOne.ServiceBus.Tests.InternalAccess.Initializers;

using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.Conventions;


public static class InitializerConventionRegistryTestDriver
{
    public static InitializerConventionRegistrySnapshot ExerciseLifecycle()
    {
        var registry = new InitializerConventionRegistry([]);

        registry.Add<DefaultInitializerConvention>();
        registry.Add<DefaultInitializerConvention>();
        IReadOnlyList<IInitializerConvention> first = registry.Conventions;
        IReadOnlyList<IInitializerConvention> second = registry.Conventions;

        InvalidOperationException? exception = null;
        try
        {
            registry.Add<DictionaryInitializerConvention>();
        }
        catch (InvalidOperationException caught)
        {
            exception = caught;
        }

        return new InitializerConventionRegistrySnapshot(
            first.Count,
            ReferenceEquals(first, second),
            first.Single().GetType(),
            exception);
    }
}

public sealed record InitializerConventionRegistrySnapshot(
    int Count,
    bool ReusedSnapshot,
    Type ConventionType,
    InvalidOperationException? LateMutationException);

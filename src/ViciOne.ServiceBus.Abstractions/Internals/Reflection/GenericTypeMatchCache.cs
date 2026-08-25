namespace ViciOne.ServiceBus.Internals
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;


    internal sealed class GenericTypeMatchCache
    {
        readonly ConditionalWeakTable<Type, ConditionalWeakTable<Type, MatchSet>> _matches = new();

        public Type[] GetMatches(Type type, Type genericTypeDefinition)
        {
            return GetMatchSet(type, genericTypeDefinition).All;
        }

        public Type[] GetClosedMatches(Type type, Type genericTypeDefinition)
        {
            return GetMatchSet(type, genericTypeDefinition).Closed;
        }

        MatchSet GetMatchSet(Type type, Type genericTypeDefinition)
        {
            ConditionalWeakTable<Type, MatchSet> definitions = _matches.GetValue(
                type,
                static _ => new ConditionalWeakTable<Type, MatchSet>());

            return definitions.GetValue(genericTypeDefinition, definition => FindMatches(type, definition));
        }

        static MatchSet FindMatches(Type type, Type genericTypeDefinition)
        {
            IEnumerable<Type> candidates = genericTypeDefinition.IsInterface
                ? GetInterfaceCandidates(type)
                : GetClassCandidates(type);

            Type[] all = candidates
                .Where(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == genericTypeDefinition)
                .Distinct()
                .OrderBy(GetStableTypeIdentity, StringComparer.Ordinal)
                .ToArray();

            return new MatchSet(all, all.Where(static type => !type.IsGenericTypeDefinition && !type.ContainsGenericParameters).ToArray());
        }

        static IEnumerable<Type> GetInterfaceCandidates(Type type)
        {
            if (type.IsInterface)
                yield return type;

            foreach (var interfaceType in type.GetInterfaces())
                yield return interfaceType;
        }

        static IEnumerable<Type> GetClassCandidates(Type type)
        {
            for (var candidate = type; candidate != null && candidate != typeof(object); candidate = candidate.BaseType)
                yield return candidate;
        }

        static string GetStableTypeIdentity(Type type)
        {
            return type.AssemblyQualifiedName ?? type.FullName ?? type.Name;
        }

        sealed record MatchSet(Type[] All, Type[] Closed);
    }
}

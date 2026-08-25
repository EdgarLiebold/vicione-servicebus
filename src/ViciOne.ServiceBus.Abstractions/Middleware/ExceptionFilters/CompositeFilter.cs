namespace ViciOne.ServiceBus.ExceptionFilters
{
    class CompositeFilter<T>
    {
        readonly CompositePredicate<T> _excludes = new CompositePredicate<T>();
        readonly CompositePredicate<T> _includes = new CompositePredicate<T>();

        public CompositePredicate<T> Includes
        {
            get => _includes;
            set { }
        }

        public CompositePredicate<T> Excludes
        {
            get => _excludes;
            set { }
        }

        public bool Matches(T target)
        {
            return Includes.MatchesAny(target) && Excludes.DoesNotMatchAny(target);
        }

        public CompositeFilter<T> CreateSnapshot()
        {
            var snapshot = new CompositeFilter<T>();
            foreach (var predicate in _includes.CreateSnapshot())
                snapshot.Includes.Add(predicate);
            foreach (var predicate in _excludes.CreateSnapshot())
                snapshot.Excludes.Add(predicate);

            return snapshot;
        }
    }
}

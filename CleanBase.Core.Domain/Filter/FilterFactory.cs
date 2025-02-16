using CleanBase.Core.Domain.Exceptions;
using CleanBase.Core.Domain.Filter.DefaultFilters;
using CleanBase.Core.Filter;

namespace CleanBase.Core.Domain.Filter
{
    public class FilterFactory : IFilterFactory
    {
        private readonly Dictionary<string, Func<BaseFilter>> _filterMappings = new(StringComparer.OrdinalIgnoreCase);

        public FilterFactory()
        {
            RegisterDefaultFilters();
        }

        protected virtual void RegisterDefaultFilters()
        {
            RegisterFilter("Equals", () => new EqualsFilter());
            RegisterFilter("NotEquals", () => new NotEqualsFilter());
            RegisterFilter("Contains", () => new ContainsFilter());
            RegisterFilter("StartsWith", () => new StartsWithFilter());
            RegisterFilter("EndsWith", () => new EndsWithFilter());
            RegisterFilter("GreaterThan", () => new GreaterThanFilter());
            RegisterFilter("LessThan", () => new LessThanFilter());
            RegisterFilter("Range", () => new RangeFilter());
            RegisterFilter("In", () => new InFilter());
            RegisterFilter("NotIn", () => new NotInFilter());
        }

        public BaseFilter CreateFilter(string filterType)
        {
            if (string.IsNullOrWhiteSpace(filterType))
                throw new DomainException(message: $"Filter type cannot be null or empty: {nameof(filterType)}");

            if (!_filterMappings.TryGetValue(filterType, out var creator))
                throw new DomainException(message: $"Filter type '{filterType}' is not supported.");

            return creator();
        }

        public void RegisterFilter(string filterType, Func<BaseFilter> filterCreator)
        {
            if (string.IsNullOrWhiteSpace(filterType))
                throw new DomainException(message: $"Filter type cannot be null or empty: {nameof(filterType)}");

            _filterMappings[filterType] = filterCreator ?? throw new DomainException(message: $"Filter creator cannot be null: {nameof(filterCreator)}");
        }
    }
}

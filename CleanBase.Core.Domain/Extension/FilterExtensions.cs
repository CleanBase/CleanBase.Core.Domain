using CleanBase.Core.CustomAttribute;
using CleanBase.Core.Domain.Exceptions;
using CleanBase.Core.Domain.Filter.DefaultFilters;
using CleanBase.Core.Entities.Base;
using CleanBase.Core.Filter;
using CleanBase.Core.ViewModels.Request.Filter;
using System.Collections;
using System.Reflection;

namespace CleanBase.Core.Domain.Extension
{
    public static class FilterExtensions
    {
        /// <summary>
        /// Safely builds filters from the given DTO using the specified filter factory.
        /// </summary>
        public static List<BaseFilter> BuildFiltersSafely(this IFilterRequestBase dto, IFilterFactory factory)
        {
            var filters = new List<BaseFilter>();

            if (dto == null) return filters;

            foreach (var property in dto.GetType().GetProperties())
            {
                var attribute = property.GetCustomAttribute<FilterFieldAttribute>();
                if (attribute == null) continue;

                var value = property.GetValue(dto);
                if (value == null) continue;

                var filter = factory.CreateFilter(attribute.FilterType);
                filter.FieldName = attribute.BindingField ?? property.Name;

                if (filter is RangeFilter rangeFilter && IsDataRange(value))
                {
                    ApplyRangeFilter(rangeFilter, value);
                }
                else if (filter is BaseFilter baseFilter)
                {
                    try
                    {
                        AssignValueToFilter(baseFilter, value);
                    }
                    catch (Exception ex)
                    {
                        throw new DomainException(
                            message: $"Error assigning value to filter type {baseFilter.GetType().Name}.",
                            innerException: ex
                        );
                    }
                }
                else
                {
                    throw new DomainException(message: $"Unsupported filter type: {filter.GetType().Name}");
                }

                filters.Add(filter);
            }

            return filters;
        }

        /// <summary>
        /// Applies range-specific values to a RangeFilter.
        /// </summary>
        private static void ApplyRangeFilter(RangeFilter rangeFilter, object range)
        {
            var type = range.GetType();
            var startProperty = type.GetProperty("Start");
            var endProperty = type.GetProperty("End");

            if (startProperty == null || endProperty == null)
                throw new DomainException(message: $"Invalid DataRange type: {type.FullName}");

            rangeFilter.StartValue = startProperty.GetValue(range);
            rangeFilter.EndValue = endProperty.GetValue(range);
        }

        /// <summary>
        /// Assigns a value to a filter based on its type.
        /// </summary>
        private static void AssignValueToFilter(BaseFilter filter, object value)
        {
            switch (filter)
            {
                case EqualsFilter:
                case NotEqualsFilter:
                case GreaterThanFilter:
                case LessThanFilter:
                    AssignSimpleValue(filter, value);
                    break;

                case ContainsFilter:
                case StartsWithFilter:
                case EndsWithFilter:
                    AssignStringValue(filter, value);
                    break;

                case InFilter:
                case NotInFilter:
                    AssignEnumerableValue(filter, value);
                    break;

                default:
                    throw new DomainException(message: $"Unsupported filter type: {filter.GetType().Name}");
            }
        }

        /// <summary>
        /// Assigns a simple value to a filter.
        /// </summary>
        private static void AssignSimpleValue(BaseFilter filter, object value)
        {
            if (value == null)
                throw new DomainException(message: $"Value cannot be null for filter type {filter.GetType().Name}.");

            switch (filter)
            {
                case EqualsFilter equalsFilter:
                    equalsFilter.Value = value;
                    break;
                case NotEqualsFilter notEqualsFilter:
                    notEqualsFilter.Value = value;
                    break;
                case GreaterThanFilter greaterThanFilter:
                    greaterThanFilter.Value = value;
                    break;
                case LessThanFilter lessThanFilter:
                    lessThanFilter.Value = value;
                    break;
            }
        }

        /// <summary>
        /// Assigns a string value to a filter.
        /// </summary>
        private static void AssignStringValue(BaseFilter filter, object value)
        {
            if (value is not string stringValue)
                throw new DomainException(message: $"Value must be a string for filter type {filter.GetType().Name}.");

            switch (filter)
            {
                case ContainsFilter containsFilter:
                    containsFilter.Value = stringValue;
                    break;
                case StartsWithFilter startsWithFilter:
                    startsWithFilter.Value = stringValue;
                    break;
                case EndsWithFilter endsWithFilter:
                    endsWithFilter.Value = stringValue;
                    break;
            }
        }

        /// <summary>
        /// Assigns an enumerable value to a filter while ensuring proper type conversion.
        /// </summary>
        private static void AssignEnumerableValue(BaseFilter filter, object value)
        {
            if (value is not IEnumerable enumerable)
                throw new DomainException(message: $"Value must be an enumerable for filter type {filter.GetType().Name}.");

            var convertedValues = ConvertEnumerableValues(enumerable, filter);

            switch (filter)
            {
                case InFilter inFilter:
                    inFilter.Values = convertedValues;
                    break;
                case NotInFilter notInFilter:
                    notInFilter.Values = convertedValues;
                    break;
            }
        }

        /// <summary>
        /// Converts an enumerable object to a strongly typed list based on the filter's expected type.
        /// </summary>
        private static IEnumerable<object> ConvertEnumerableValues(IEnumerable values, BaseFilter filter)
        {
            var elementType = values.Cast<object>().FirstOrDefault()?.GetType();
            if (elementType == null)
                throw new DomainException($"Unable to determine the type of values for filter {filter.GetType().Name}.");

            var convertedValues = new List<object>();

            foreach (var value in values)
            {
                try
                {
                    var convertedValue = ConvertValue(elementType, value);
                    convertedValues.Add(convertedValue);
                }
                catch (Exception ex)
                {
                    throw new DomainException($"Error converting value '{value}' for filter {filter.GetType().Name}.", ex.Message);
                }
            }

            return convertedValues;
        }

        /// <summary>
        /// Converts a given value to the specified target type.
        /// </summary>
        private static object ConvertValue(Type targetType, object value)
        {
            if (value == null) return null;

            try
            {
                if (Nullable.GetUnderlyingType(targetType) != null)
                    targetType = Nullable.GetUnderlyingType(targetType);

                if (targetType == typeof(Guid))
                {
                    if (value is Guid guidValue)
                        return guidValue;
                    if (value is string guidString && Guid.TryParse(guidString, out Guid parsedGuid))
                        return parsedGuid;
                    throw new InvalidOperationException($"Cannot convert value '{value}' to Guid.");
                }

                if (targetType.IsEnum)
                {
                    if (value is string stringValue)
                        return Enum.Parse(targetType, stringValue, ignoreCase: true);
                    return Enum.ToObject(targetType, value);
                }

                return Convert.ChangeType(value, targetType);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Cannot convert value '{value}' to type '{targetType.Name}'.", ex);
            }
        }

        /// <summary>
        /// Checks if an object is a DataRange type.
        /// </summary>
        private static bool IsDataRange(object obj)
        {
            var type = obj.GetType();
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(DataRange<>);
        }
    }
}

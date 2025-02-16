using CleanBase.Core.Filter;
using System.Linq.Expressions;

namespace CleanBase.Core.Domain.Filter.DefaultFilters
{
    /// <summary>
    /// Represents a filter that selects entities where a field value is contained within a specified set of values.
    /// </summary>
    public class InFilter : BaseFilter
    {
        /// <summary>
        /// The set of values to compare with the field.
        /// </summary>
        public IEnumerable<object> Values { get; set; } = new List<object>();

        /// <summary>
        /// Builds a LINQ expression to filter entities where the specified field value is in the given set of values.
        /// </summary>
        /// <typeparam name="T">The type of the entity being filtered.</typeparam>
        /// <returns>An expression that can be used in LINQ queries.</returns>
        public override Expression<Func<T, bool>> BuildExpression<T>()
        {
            if (Values == null || !Values.Any())
                throw new ArgumentException("Values cannot be null or empty.", nameof(Values));

            // Create parameter representing the entity (e.g., "x").
            var parameter = Expression.Parameter(typeof(T), "x");

            // Get the property or field to be compared.
            var member = Expression.PropertyOrField(parameter, FieldName);

            // Ensure the member type is correctly handled, including Nullable<T>.
            Type memberType = Nullable.GetUnderlyingType(member.Type) ?? member.Type;

            // Convert Values to a strongly typed List<T>.
            var convertedValues = Values
                .Where(value => value != null)
                .Select(value => ConvertValue(memberType, value))
                .ToList();

            // Ensure the converted list is strongly typed.
            var typedValues = Array.CreateInstance(memberType, convertedValues.Count);
            for (int i = 0; i < convertedValues.Count; i++)
            {
                typedValues.SetValue(convertedValues[i], i);
            }

            var constant = Expression.Constant(typedValues);

            // Use LINQ Contains method.
            var containsMethod = typeof(Enumerable)
                .GetMethods()
                .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
                .MakeGenericMethod(memberType);

            var body = Expression.Call(containsMethod, constant, member);

            // Return the Lambda Expression.
            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }

        /// <summary>
        /// Converts a given value to the target field type for proper comparison.
        /// </summary>
        /// <param name="targetType">The type of the field being compared.</param>
        /// <param name="value">The value to convert.</param>
        /// <returns>The converted value.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the value cannot be converted.</exception>
        private object ConvertValue(Type targetType, object value)
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
    }
}

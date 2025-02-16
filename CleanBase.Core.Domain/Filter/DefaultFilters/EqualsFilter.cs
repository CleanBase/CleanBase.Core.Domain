using CleanBase.Core.Filter;
using System.Linq.Expressions;

namespace CleanBase.Core.Domain.Filter.DefaultFilters
{
    /// <summary>
    /// Represents a filter that checks for equality between a specified field and a given value.
    /// </summary>
    public class EqualsFilter : BaseFilter
    {
        /// <summary>
        /// The value to compare with the field.
        /// </summary>
        public object? Value { get; set; }

        /// <summary>
        /// Builds a LINQ expression to filter entities where the specified field equals the given value.
        /// </summary>
        /// <typeparam name="T">The type of the entity being filtered.</typeparam>
        /// <returns>An expression that can be used in LINQ queries.</returns>
        public override Expression<Func<T, bool>> BuildExpression<T>()
        {
            // Throw an exception if the Value is null, as equality comparisons require a valid value.
            if (Value == null)
                throw new ArgumentNullException(nameof(Value), "Value cannot be null.");

            // Create a parameter expression representing the entity being filtered (e.g., "x").
            var parameter = Expression.Parameter(typeof(T), "x");

            // Get the property or field to be compared using the FieldName.
            var member = Expression.PropertyOrField(parameter, FieldName);

            // Convert the Value to match the type of the field being compared.
            var constant = Expression.Constant(ConvertValue(member.Type, Value), member.Type);

            // Create an equality comparison expression (e.g., x.FieldName == Value).
            var body = Expression.Equal(member, constant);

            // Return the complete lambda expression.
            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }

        /// <summary>
        /// Converts the provided value to the target type of the field, ensuring type compatibility.
        /// </summary>
        /// <param name="targetType">The type of the field being compared.</param>
        /// <param name="value">The value to convert.</param>
        /// <returns>The converted value, matching the target type.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the value cannot be converted to the target type.</exception>
        private object ConvertValue(Type targetType, object value)
        {
            // If the value is already null or matches the target type, return it as is.
            if (value == null || targetType.IsAssignableFrom(value.GetType()))
                return value;

            try
            {
                // If the target type is an enum, attempt to parse the value into the enum type.
                if (targetType.IsEnum)
                {
                    if (value is string stringValue)
                        return Enum.Parse(targetType, stringValue, ignoreCase: true);

                    return Enum.ToObject(targetType, value);
                }

                // If the target type is Guid, parse the string into a Guid.
                if (targetType == typeof(Guid) && value is string guidString)
                {
                    return Guid.Parse(guidString);
                }

                // Handle nullable types by extracting their underlying type.
                if (Nullable.GetUnderlyingType(targetType) != null)
                {
                    targetType = Nullable.GetUnderlyingType(targetType);
                }

                // Convert the value to the target type.
                return Convert.ChangeType(value, targetType);
            }
            catch (Exception ex)
            {
                // Throw an exception with detailed information if the conversion fails.
                throw new InvalidOperationException($"Cannot convert value '{value}' to type '{targetType.Name}'.", ex);
            }
        }
    }
}

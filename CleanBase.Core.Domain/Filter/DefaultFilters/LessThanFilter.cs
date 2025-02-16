using CleanBase.Core.Filter;
using System.Linq.Expressions;

namespace CleanBase.Core.Domain.Filter.DefaultFilters
{
    /// <summary>
    /// Represents a filter that selects entities where a field value is less than a specified value.
    /// </summary>
    public class LessThanFilter : BaseFilter
    {
        /// <summary>
        /// The value to compare with the field.
        /// </summary>
        public object? Value { get; set; }

        /// <summary>
        /// Builds a LINQ expression to filter entities where the specified field value is less than the given value.
        /// </summary>
        /// <typeparam name="T">The type of the entity being filtered.</typeparam>
        /// <returns>An expression that can be used in LINQ queries.</returns>
        public override Expression<Func<T, bool>> BuildExpression<T>()
        {
            if (Value == null)
                throw new ArgumentNullException(nameof(Value), "Value cannot be null.");

            // Create a parameter expression representing the entity being filtered (e.g., "x").
            var parameter = Expression.Parameter(typeof(T), "x");

            // Get the property or field to be compared using the FieldName.
            var member = Expression.PropertyOrField(parameter, FieldName);

            // Convert the Value to match the type of the field being compared.
            var constant = Expression.Constant(ConvertValue(member.Type, Value), member.Type);

            // Create the LessThan expression.
            var body = Expression.LessThan(member, constant);

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
            if (value == null || targetType.IsAssignableFrom(value.GetType()))
                return value;

            try
            {
                if (targetType.IsEnum)
                {
                    if (value is string stringValue)
                        return Enum.Parse(targetType, stringValue, ignoreCase: true);

                    return Enum.ToObject(targetType, value);
                }

                if (Nullable.GetUnderlyingType(targetType) != null)
                {
                    targetType = Nullable.GetUnderlyingType(targetType);
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

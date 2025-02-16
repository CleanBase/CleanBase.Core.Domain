using CleanBase.Core.Filter;
using System.Linq.Expressions;

namespace CleanBase.Core.Domain.Filter.DefaultFilters
{
    /// <summary>
    /// Represents a filter that selects entities where a field value falls within a specified range.
    /// </summary>
    public class RangeFilter : BaseFilter
    {
        /// <summary>
        /// The start value of the range.
        /// </summary>
        public object? StartValue { get; set; }

        /// <summary>
        /// The end value of the range.
        /// </summary>
        public object? EndValue { get; set; }

        /// <summary>
        /// Builds a LINQ expression to filter entities where the specified field value falls within the given range.
        /// </summary>
        /// <typeparam name="T">The type of the entity being filtered.</typeparam>
        /// <returns>An expression that can be used in LINQ queries.</returns>
        public override Expression<Func<T, bool>> BuildExpression<T>()
        {
            if (StartValue == null && EndValue == null)
                throw new InvalidOperationException("RangeFilter requires at least a StartValue or EndValue.");

            // Create a parameter expression representing the entity being filtered (e.g., "x").
            var parameter = Expression.Parameter(typeof(T), "x");

            // Get the property or field to be compared using the FieldName.
            var member = Expression.PropertyOrField(parameter, FieldName);

            // Convert StartValue and EndValue to match the type of the field being compared.
            var lowerBound = StartValue != null
                ? Expression.GreaterThanOrEqual(member, Expression.Constant(ConvertValue(member.Type, StartValue), member.Type))
                : null;

            var upperBound = EndValue != null
                ? Expression.LessThanOrEqual(member, Expression.Constant(ConvertValue(member.Type, EndValue), member.Type))
                : null;

            // Combine the lower and upper bounds into a single expression.
            Expression body = null;

            if (lowerBound != null && upperBound != null)
            {
                body = Expression.AndAlso(lowerBound, upperBound);
            }
            else if (lowerBound != null)
            {
                body = lowerBound;
            }
            else if (upperBound != null)
            {
                body = upperBound;
            }

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

                if (targetType == typeof(Guid) && value is string guidString)
                {
                    return Guid.Parse(guidString);
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

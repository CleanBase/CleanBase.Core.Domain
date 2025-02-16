using CleanBase.Core.Filter;
using System.Linq.Expressions;

namespace CleanBase.Core.Domain.Filter.DefaultFilters
{
    /// <summary>
    /// Represents a filter that selects entities where a string field ends with a specified value.
    /// </summary>
    public class EndsWithFilter : BaseFilter
    {
        /// <summary>
        /// The value to check for at the end of the string field.
        /// </summary>
        public string? Value { get; set; }

        /// <summary>
        /// Indicates whether the comparison should be case-insensitive.
        /// </summary>
        public bool IgnoreCase { get; set; } = true;

        /// <summary>
        /// Builds a LINQ expression to filter entities where the specified string field ends with the given value.
        /// </summary>
        /// <typeparam name="T">The type of the entity being filtered.</typeparam>
        /// <returns>An expression that can be used in LINQ queries.</returns>
        public override Expression<Func<T, bool>> BuildExpression<T>()
        {
            if (string.IsNullOrEmpty(Value))
                throw new ArgumentException("Value cannot be null or empty.", nameof(Value));

            // Create a parameter expression representing the entity being filtered (e.g., "x").
            var parameter = Expression.Parameter(typeof(T), "x");

            // Get the property or field to be compared using the FieldName.
            var member = Expression.PropertyOrField(parameter, FieldName);

            // Handle nullable strings by adding a null check.
            Expression notNullCheck = null;
            if (Nullable.GetUnderlyingType(member.Type) != null)
            {
                notNullCheck = Expression.NotEqual(member, Expression.Constant(null, member.Type));
            }

            // Ensure the field is of type string or nullable string.
            var memberType = Nullable.GetUnderlyingType(member.Type) ?? member.Type;
            if (memberType != typeof(string))
                throw new InvalidOperationException($"The field '{FieldName}' must be of type 'string'.");

            // Convert the field and value to lowercase if case-insensitivity is required.
            Expression memberAccess = member;
            if (IgnoreCase)
            {
                var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes);
                memberAccess = Expression.Call(member, toLowerMethod);
            }

            var valueToLower = IgnoreCase ? Value.ToLowerInvariant() : Value;

            // Create the EndsWith expression.
            var endsWithMethod = typeof(string).GetMethod("EndsWith", new[] { typeof(string) });
            var constant = Expression.Constant(valueToLower);
            var endsWithExpression = Expression.Call(memberAccess, endsWithMethod, constant);

            // Combine the null check and EndsWith expression if necessary.
            Expression body = notNullCheck != null
                ? Expression.AndAlso(notNullCheck, endsWithExpression)
                : endsWithExpression;

            // Return the complete lambda expression.
            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }
    }
}

using Domain.Behaviours;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Application.Extentions
{
    /// <summary>
    /// Builds a predicate from list query filters of the form <c>Filters[property.operation]=value</c>.
    /// Only scalar properties without <see cref="NotFilterableAttribute"/> can be filtered.
    /// Invalid filters throw <see cref="ArgumentException"/>, which the API returns as 400.
    /// </summary>
    public static class FilterExpressionBuilder
    {
        private static readonly MethodInfo StringContains = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
        private static readonly MethodInfo StringStartsWith = typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!;
        private static readonly MethodInfo StringEndsWith = typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string)])!;

        public static Expression<Func<T, bool>>? BuildPredicate<T>(this Dictionary<string, string> filters)
        {
            if (filters == null || filters.Count == 0)
                return null;

            ParameterExpression param = Expression.Parameter(typeof(T), "x");
            Expression? combined = null;

            foreach (var filter in filters)
            {
                var parts = filter.Key.Split('.', 2);
                if (parts.Length != 2)
                    throw new ArgumentException($"Filter '{filter.Key}' must have the form 'property.operation'.");

                string propertyName = parts[0];
                string operation = parts[1].ToLowerInvariant();
                string valueString = filter.Value;

                PropertyInfo? property = typeof(T).GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                // Unknown and forbidden properties get the same answer, so the error does not reveal which exist.
                if (property == null || property.IsDefined(typeof(NotFilterableAttribute)) || !IsScalar(property.PropertyType))
                    throw new ArgumentException($"Filtering by '{propertyName}' is not allowed.");

                var isString = property.PropertyType == typeof(string);
                Expression left = Expression.Property(param, property);

                Expression condition = operation switch
                {
                    "contains" when isString => Expression.Call(left, StringContains, Expression.Constant(valueString)),
                    "startswith" when isString => Expression.Call(left, StringStartsWith, Expression.Constant(valueString)),
                    "endswith" when isString => Expression.Call(left, StringEndsWith, Expression.Constant(valueString)),
                    "equals" => Expression.Equal(left, TypedConstant(valueString, property)),
                    "notequals" => Expression.NotEqual(left, TypedConstant(valueString, property)),
                    "greaterthan" or "morethan" when !isString => Expression.GreaterThan(left, TypedConstant(valueString, property)),
                    "lessthan" when !isString => Expression.LessThan(left, TypedConstant(valueString, property)),
                    _ => throw new ArgumentException($"Operation '{parts[1]}' cannot be used with '{propertyName}'.")
                };

                combined = combined == null ? condition : Expression.AndAlso(combined, condition);
            }

            return combined == null ? null : Expression.Lambda<Func<T, bool>>(combined, param);
        }

        private static bool IsScalar(Type type)
        {
            var actualType = Nullable.GetUnderlyingType(type) ?? type;
            return actualType.IsPrimitive
                || actualType.IsEnum
                || actualType == typeof(string)
                || actualType == typeof(decimal)
                || actualType == typeof(Guid)
                || actualType == typeof(DateTime);
        }

        private static ConstantExpression TypedConstant(string value, PropertyInfo property)
        {
            try
            {
                return Expression.Constant(ConvertToPropertyType(value, property.PropertyType), property.PropertyType);
            }
            catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException or ArgumentException)
            {
                throw new ArgumentException($"Value '{value}' is not valid for '{property.Name}'.");
            }
        }

        private static object ConvertToPropertyType(string value, Type targetType)
        {
            var actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (actualType == typeof(Guid))
                return Guid.Parse(value);

            if (actualType.IsEnum)
                return Enum.Parse(actualType, value, ignoreCase: true);

            if (actualType == typeof(DateTime))
                return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);

            return Convert.ChangeType(value, actualType, CultureInfo.InvariantCulture);
        }

    }
}

using Application.Exceptions;
using Application.Models.Requests.Abstractions;
using Domain.Abstractions;
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
    /// Invalid filters throw <see cref="BadRequestException"/>, which the API returns as 400.
    /// Also builds the list query's <c>SearchText</c> and <c>SortBy</c>/<c>SortDirection</c> parts.
    /// </summary>
    public static class FilterExpressionBuilder
    {
        private static readonly MethodInfo StringContains = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
        private static readonly MethodInfo StringStartsWith = typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!;
        private static readonly MethodInfo StringEndsWith = typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string)])!;
        private static readonly MethodInfo StringToLower = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;

        /// <summary>
        /// Filters and search text of a list query as one predicate (both must match), or null when neither is given.
        /// </summary>
        public static Expression<Func<T, bool>>? BuildPredicate<T>(this BaseGetListQuery query)
        {
            ParameterExpression param = Expression.Parameter(typeof(T), "x");
            var filters = BuildFilterBody<T>(query.Filters, param);
            var search = BuildSearchBody<T>(query.SearchText, param);

            Expression? combined = (filters, search) switch
            {
                (null, null) => null,
                (null, _) => search,
                (_, null) => filters,
                _ => Expression.AndAlso(filters, search)
            };
            return combined == null ? null : Expression.Lambda<Func<T, bool>>(combined, param);
        }

        /// <summary>
        /// Ordering from <c>SortBy</c>/<c>SortDirection</c>, or null when <c>SortBy</c> is empty (the service then uses its default).
        /// Rows with equal sort values are ordered by Id, so pages do not overlap or skip rows.
        /// </summary>
        public static Func<IQueryable<T>, IOrderedQueryable<T>>? BuildOrderBy<T>(this BaseGetListQuery query)
            where T : BaseEntity
        {
            if (string.IsNullOrWhiteSpace(query.SortBy))
                return null;

            var descending = (query.SortDirection ?? "desc").ToLowerInvariant() switch
            {
                "asc" => false,
                "desc" => true,
                _ => throw new BadRequestException("SortDirection must be 'asc' or 'desc'.")
            };

            PropertyInfo? property = typeof(T).GetProperty(query.SortBy, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            // Sorting reveals a value's order as much as a range filter does, so the same properties are refused.
            if (property == null || property.IsDefined(typeof(NotFilterableAttribute)) || !IsScalar(property.PropertyType))
                throw new BadRequestException($"Sorting by '{query.SortBy}' is not allowed.");

            ParameterExpression param = Expression.Parameter(typeof(T), "x");
            var keySelector = Expression.Lambda(Expression.Property(param, property), param);
            var method = descending ? nameof(Queryable.OrderByDescending) : nameof(Queryable.OrderBy);

            return source =>
            {
                var ordered = (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(
                    Expression.Call(typeof(Queryable), method, [typeof(T), property.PropertyType], source.Expression, Expression.Quote(keySelector)));
                return descending ? ordered.ThenByDescending(x => x.Id) : ordered.ThenBy(x => x.Id);
            };
        }

        /// <summary>
        /// Case-insensitive "contains" over the properties marked <see cref="SearchableAttribute"/>; any of them may match.
        /// </summary>
        private static Expression? BuildSearchBody<T>(string? searchText, ParameterExpression param)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return null;

            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType == typeof(string) && p.IsDefined(typeof(SearchableAttribute)))
                .ToList();
            if (properties.Count == 0)
                throw new BadRequestException("Search is not supported for this list.");

            var term = Expression.Constant(searchText.Trim().ToLowerInvariant());
            Expression? any = null;
            foreach (var property in properties)
            {
                Expression value = Expression.Property(param, property);
                // lower(column) LIKE in SQL; a null column simply does not match.
                Expression match = Expression.AndAlso(
                    Expression.NotEqual(value, Expression.Constant(null, typeof(string))),
                    Expression.Call(Expression.Call(value, StringToLower), StringContains, term));
                any = any == null ? match : Expression.OrElse(any, match);
            }
            return any;
        }

        private static Expression? BuildFilterBody<T>(Dictionary<string, string>? filters, ParameterExpression param)
        {
            if (filters == null || filters.Count == 0)
                return null;

            Expression? combined = null;

            foreach (var filter in filters)
            {
                var parts = filter.Key.Split('.', 2);
                if (parts.Length != 2)
                    throw new BadRequestException($"Filter '{filter.Key}' must have the form 'property.operation'.");

                string propertyName = parts[0];
                string operation = parts[1].ToLowerInvariant();
                string valueString = filter.Value;

                PropertyInfo? property = typeof(T).GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                // Unknown and forbidden properties get the same answer, so the error does not reveal which exist.
                if (property == null || property.IsDefined(typeof(NotFilterableAttribute)) || !IsScalar(property.PropertyType))
                    throw new BadRequestException($"Filtering by '{propertyName}' is not allowed.");

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
                    _ => throw new BadRequestException($"Operation '{parts[1]}' cannot be used with '{propertyName}'.")
                };

                combined = combined == null ? condition : Expression.AndAlso(combined, condition);
            }

            return combined;
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
                throw new BadRequestException($"Value '{value}' is not valid for '{property.Name}'.");
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Application.Extentions
{
    public static class FilterExpressionBuilder
    {
        public static Expression<Func<T, bool>>? BuildPredicate<T>(this Dictionary<string, string> filters)
        {
            if (filters == null || filters.Count == 0)
                return null;

            ParameterExpression param = Expression.Parameter(typeof(T), "x");
            Expression? combined = null;

            foreach (var filter in filters)
            {
                var parts = filter.Key.Split('.', 2);
                if (parts.Length != 2) continue;

                string propertyName = parts[0];
                string operation = parts[1];
                string valueString = filter.Value;

                PropertyInfo? property = typeof(T).GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (property == null) continue;

                Expression left = Expression.Property(param, property);
                object? typedValue = Convert.ChangeType(valueString, Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType);
                Expression right = Expression.Constant(typedValue, property.PropertyType);

                Expression? condition = operation.ToLower() switch
                {
                    "equals" => Expression.Equal(left, right),
                    "notequals" => Expression.NotEqual(left, right),
                    "greaterthan" or "morethan" => Expression.GreaterThan(left, right),
                    "lessthan" => Expression.LessThan(left, right),
                    "contains" => Expression.Call(left, typeof(string).GetMethod("Contains", [typeof(string)])!, Expression.Constant(valueString)),
                    "startswith" => Expression.Call(left, typeof(string).GetMethod("StartsWith", [typeof(string)])!, Expression.Constant(valueString)),
                    "endswith" => Expression.Call(left, typeof(string).GetMethod("EndsWith", [typeof(string)])!, Expression.Constant(valueString)),
                    _ => null
                };

                if (condition != null)
                    combined = combined == null ? condition : Expression.AndAlso(combined, condition);
            }

            if (combined == null) return null;

            return Expression.Lambda<Func<T, bool>>(combined, param);
        }
    }
}

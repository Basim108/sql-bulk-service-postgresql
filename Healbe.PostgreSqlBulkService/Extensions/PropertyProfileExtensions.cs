using System;
using System.Linq.Expressions;
using JetBrains.Annotations;

namespace Hrimsoft.PostgresSqlBulkService
{
    /// <summary>
    /// Methods to extend PropertyProfile type
    /// </summary>
    public static class PropertyProfileExtensions
    {
        /// <summary>
        /// Calculates value of an item's property
        /// </summary>
        /// <param name="item">item with values</param>
        /// <param name="propInfo">information about which property it is needed to get value</param>
        /// <typeparam name="TEntity">Type of entity</typeparam>
        /// <returns>Returns</returns>
        // ReSharper disable once MemberCanBePrivate.Global  Needed to be public for unit testing purpose
        public static object GetPropertyValue<TEntity>([NotNull] this PropertyProfile propInfo, TEntity item)
            where TEntity : class
        {
            object value = null;
            if (propInfo.PropertyExpression is LambdaExpression lambdaExpression)
            {
                value = lambdaExpression.Compile().DynamicInvoke(item);
            }
            else if (propInfo.PropertyExpression is MemberExpression memberExpression)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));
                
                var convert = Expression.Convert(memberExpression, memberExpression.Type);
                var lambda = Expression.Lambda(convert, memberExpression.Expression as ParameterExpression);
                value = lambda.Compile().DynamicInvoke(item);
            }
            else if (propInfo.PropertyExpression is ConstantExpression constExpression)
            {
                value = constExpression.Value;
            }
            else if (propInfo.PropertyExpression is BinaryExpression)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));
                
                var lambda = Expression.Lambda(propInfo.PropertyExpression);
                value = lambda.Compile().DynamicInvoke(item);
            }

            return value;
        }
    }
}
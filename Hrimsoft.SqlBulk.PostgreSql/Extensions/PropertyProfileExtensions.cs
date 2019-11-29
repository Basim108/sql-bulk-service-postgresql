using System;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;

namespace Hrimsoft.SqlBulk.PostgreSql
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
        public static object GetPropertyValue<TEntity>([NotNull] this PropertyProfile propInfo, TEntity item)
            where TEntity : class
        {
            var value = propInfo.PropertyExpression?.Compile().DynamicInvoke(item);
            return value;
        }

        /// <summary>
        /// Updates value of an item's property
        /// </summary>
        /// <param name="item">TEntity instance that has to be updated</param>
        /// <param name="propInfo">information about which property it is needed to set value</param>
        /// <param name="value"></param>
        /// <typeparam name="TEntity">Type of entity</typeparam>
        public static void SetPropertyValue<TEntity>([NotNull] this PropertyProfile propInfo, [NotNull] TEntity item, object value)
            where TEntity : class
        {
            if (propInfo.PropertyExpression.Body is MemberExpression memberSelectorExpression)
            {
                var property = memberSelectorExpression.Member as PropertyInfo;
                if (property != null)
                    property.SetValue(item, value, null);
            }
            else
                throw new ArgumentException($"{nameof(propInfo.PropertyExpression)} must be an instance of {nameof(MemberExpression)} type, but it is {propInfo.PropertyExpression.GetType().FullName}");
        }
    }
}
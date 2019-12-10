using System;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using NpgsqlTypes;

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
        /// <param name="profile">information about which property it is needed to get value</param>
        /// <typeparam name="TEntity">Type of entity</typeparam>
        /// <returns>Returns</returns>
        public static object GetPropertyValue<TEntity>([NotNull] this PropertyProfile profile, TEntity item)
            where TEntity : class
        {
            var value = profile.PropertyExpression?.Compile().DynamicInvoke(item);
            return value;
        }

        /// <summary>
        /// Updates value of an item's property
        /// </summary>
        /// <param name="item">TEntity instance that has to be updated</param>
        /// <param name="profile">information about which property it is needed to set value</param>
        /// <param name="value"></param>
        /// <typeparam name="TEntity">Type of entity</typeparam>
        public static void SetPropertyValue<TEntity>([NotNull] this PropertyProfile profile, [NotNull] TEntity item, object value)
            where TEntity : class
        {
            if (profile.PropertyExpression.Body is MemberExpression memberSelectorExpression)
            {
                var property = memberSelectorExpression.Member as PropertyInfo;
                if (property != null)
                    property.SetValue(item, value, null);
            }
            else
                throw new ArgumentException($"{nameof(profile.PropertyExpression)} must be an instance of {nameof(MemberExpression)} type, but it is {profile.PropertyExpression.GetType().FullName}");
        }
    }
}
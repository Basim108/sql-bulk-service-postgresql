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
        public static object GetPropertyValue<TEntity>(this PropertyProfile profile, TEntity item)
            where TEntity : class
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));
            
            var value = profile.PropertyExpression?.Compile().DynamicInvoke(item);
            if (value != null && value.GetType().IsEnum)
            {
                switch (profile.DbColumnType)
                {
                    case NpgsqlDbType.Bigint: value = (long) value; break;
                    case NpgsqlDbType.Integer: value = (int) value; break;
                    case NpgsqlDbType.Varchar: value = value.ToString(); break;
                }
                value = (int) value;
            }
            return value;
        }

        /// <summary>
        /// Updates value of an item's property
        /// </summary>
        /// <param name="item">TEntity instance that has to be updated</param>
        /// <param name="profile">information about which property it is needed to set value</param>
        /// <param name="value"></param>
        /// <typeparam name="TEntity">Type of entity</typeparam>
        public static void SetPropertyValue<TEntity>(this PropertyProfile profile, TEntity item, object value)
            where TEntity : class
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            if (profile?.PropertyExpression.Body is MemberExpression memberSelectorExpression)
            {
                var property = memberSelectorExpression.Member as PropertyInfo;
                property?.SetValue(item, value, null);
            }
            else
                throw new ArgumentException(
                    $"{nameof(profile.PropertyExpression)} must be an instance of {nameof(MemberExpression)} type, but it is {profile?.PropertyExpression.GetType().FullName}");
        }
    }
}
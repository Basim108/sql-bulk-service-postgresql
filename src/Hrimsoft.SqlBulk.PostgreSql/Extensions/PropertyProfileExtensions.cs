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

        /// <summary>
        /// Map CLR types on DbType.
        /// If you don't like this mapping then you can set your type by calling HasColumnType on a PropertyProfile instance.
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="propertyType"></param>
        public static void SetDbColumnType([NotNull] this PropertyProfile profile, [NotNull] Type propertyType)
        {
            if(propertyType == typeof(byte))
            {
                profile.HasColumnType(NpgsqlDbType.Smallint);
            }
            else if(propertyType == typeof(int))
            {
                profile.HasColumnType(NpgsqlDbType.Integer);
            }
            else if (propertyType == typeof(long))
            {
                profile.HasColumnType(NpgsqlDbType.Bigint);
            }
            else if (propertyType == typeof(bool))
            {
                profile.HasColumnType(NpgsqlDbType.Boolean);
            }
            else if (propertyType == typeof(string))
            {
                profile.HasColumnType(NpgsqlDbType.Text);
            }
            else if (propertyType == typeof(Decimal))
            {
                profile.HasColumnType(NpgsqlDbType.Double);
            }
            else if (propertyType == typeof(double))
            {
                profile.HasColumnType(NpgsqlDbType.Double);
            }
            else if (propertyType == typeof(float))
            {
                profile.HasColumnType(NpgsqlDbType.Real);
            }
            else if (propertyType == typeof(DateTime))
            {
                profile.HasColumnType(NpgsqlDbType.Timestamp);
            }
            else if (propertyType == typeof(DateTimeOffset))
            {
                profile.HasColumnType(NpgsqlDbType.TimestampTz);
            }
            else if (propertyType == typeof(TimeSpan))
            {
                profile.HasColumnType(NpgsqlDbType.Integer);
            }
        }
    }
}
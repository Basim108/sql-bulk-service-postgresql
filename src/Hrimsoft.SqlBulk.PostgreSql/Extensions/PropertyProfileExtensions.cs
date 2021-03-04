using System;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using NpgsqlTypes;

namespace Hrimsoft.SqlBulk.PostgreSql
{
    /// <summary>
    /// Methods to extend PropertyProfile type
    /// </summary>
    public static class PropertyProfileExtensions
    {
        /// <summary> Calculates property values of item </summary>
        /// <param name="item">item with values</param>
        /// <param name="profile">information about which property it is needed to get value</param>
        /// <typeparam name="TEntity">Type of entity</typeparam>
        /// <typeparam name="TResult">Type of property</typeparam>
        /// <returns>
        /// Returns string representation of the value.
        /// For nullable value returns "null" string.
        /// For those cases where it is impossible to calculate property value without boxing, returns null
        /// </returns>
        public static string GetPropertyValueAsString<TEntity>(this PropertyProfile profile, TEntity item)
            where TEntity : class
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            var propertyName = "";

            if (profile.PropertyExpression.Body is MemberExpression memberExpression)
                propertyName = memberExpression.Member.Name;
            else if (profile.PropertyExpression.Body is ParameterExpression parameterExpression)
                propertyName = parameterExpression.Name;
            else if (profile.PropertyExpression.Body is UnaryExpression unaryExpression &&
                     unaryExpression.Operand is MemberExpression operand)
                propertyName = operand.Member.Name;
            else
                return null;
            try
            {
                var propInfo = typeof(TEntity).GetProperty(propertyName);
                if (propInfo == null)
                    throw new ArgumentException($"Entity: {typeof(TEntity).FullName} doesn't have property: '{propertyName}'");
                switch (profile.DbColumnType)
                {
                    case NpgsqlDbType.Bigint:
                        if (profile.IsNullable)
                        {
                            var longNullableGetter = (Func<TEntity, long?>) Delegate.CreateDelegate(typeof(Func<TEntity, long?>), propInfo.GetGetMethod());
                            var longNullableValue  = longNullableGetter(item);
                            return longNullableValue?.ToString(CultureInfo.InvariantCulture) ?? "null";
                        }
                        var longGetter = (Func<TEntity, long>) Delegate.CreateDelegate(typeof(Func<TEntity, long>), propInfo.GetGetMethod());
                        var longValue  = longGetter(item);
                        return longValue.ToString(CultureInfo.InvariantCulture);
                    case NpgsqlDbType.Integer:
                        if (profile.IsNullable)
                        {
                            var intNullableGetter = (Func<TEntity, int?>) Delegate.CreateDelegate(typeof(Func<TEntity, int?>), propInfo.GetGetMethod());
                            var intNullableValue  = intNullableGetter(item);
                            return intNullableValue?.ToString(CultureInfo.InvariantCulture) ?? "null";
                        }
                        var intGetter = (Func<TEntity, int>) Delegate.CreateDelegate(typeof(Func<TEntity, int>), propInfo.GetGetMethod());
                        var intValue  = intGetter(item);
                        return intValue.ToString(CultureInfo.InvariantCulture);
                    case NpgsqlDbType.Real:
                        if (profile.IsNullable)
                        {
                            var floatNullableGetter = (Func<TEntity, float?>) Delegate.CreateDelegate(typeof(Func<TEntity, float?>), propInfo.GetGetMethod());
                            var floatNullableValue  = floatNullableGetter(item);
                            return floatNullableValue?.ToString(CultureInfo.InvariantCulture) ?? "null";
                        }
                        var floatGetter = (Func<TEntity, float>) Delegate.CreateDelegate(typeof(Func<TEntity, float>), propInfo.GetGetMethod());
                        var floatValue  = floatGetter(item);
                        return floatValue.ToString(CultureInfo.InvariantCulture);
                    case NpgsqlDbType.Double:
                        if (profile.IsNullable)
                        {
                            var doubleNullableGetter = (Func<TEntity, double?>) Delegate.CreateDelegate(typeof(Func<TEntity, double?>), propInfo.GetGetMethod());
                            var doubleNullableValue  = doubleNullableGetter(item);
                            return doubleNullableValue?.ToString(CultureInfo.InvariantCulture) ?? "null";
                        }
                        var doubleGetter = (Func<TEntity, double>) Delegate.CreateDelegate(typeof(Func<TEntity, double>), propInfo.GetGetMethod());
                        var doubleValue  = doubleGetter(item);
                        return doubleValue.ToString(CultureInfo.InvariantCulture);
                    case NpgsqlDbType.Numeric:
                        if (profile.IsNullable)
                        {
                            var decimalNullableGetter = (Func<TEntity, decimal?>) Delegate.CreateDelegate(typeof(Func<TEntity, decimal?>), propInfo.GetGetMethod());
                            var decimalNullableValue  = decimalNullableGetter(item);
                            return decimalNullableValue?.ToString(CultureInfo.InvariantCulture) ?? "null";
                        }
                        var decimalGetter = (Func<TEntity, decimal>) Delegate.CreateDelegate(typeof(Func<TEntity, decimal>), propInfo.GetGetMethod());
                        var decimalValue  = decimalGetter(item);
                        return decimalValue.ToString(CultureInfo.InvariantCulture);
                    case NpgsqlDbType.Boolean:
                        if (profile.IsNullable)
                        {
                            var boolNullableGetter = (Func<TEntity, bool?>) Delegate.CreateDelegate(typeof(Func<TEntity, bool?>), propInfo.GetGetMethod());
                            var boolNullableValue  = boolNullableGetter(item);
                            return boolNullableValue?.ToString(CultureInfo.InvariantCulture).ToLowerInvariant() ?? "null";
                        }
                        var boolGetter = (Func<TEntity, bool>) Delegate.CreateDelegate(typeof(Func<TEntity, bool>), propInfo.GetGetMethod());
                        var boolValue  = boolGetter(item);
                        return boolValue.ToString(CultureInfo.InvariantCulture).ToLowerInvariant();
                    case NpgsqlDbType.Smallint:
                        if (profile.IsNullable)
                        {
                            var shortNullableGetter = (Func<TEntity, short?>) Delegate.CreateDelegate(typeof(Func<TEntity, short?>), propInfo.GetGetMethod());
                            var shortNullableValue  = shortNullableGetter(item);
                            return shortNullableValue?.ToString(CultureInfo.InvariantCulture).ToLowerInvariant() ?? "null";
                        }
                        var shortGetter = (Func<TEntity, short>) Delegate.CreateDelegate(typeof(Func<TEntity, short>), propInfo.GetGetMethod());
                        var shortValue  = shortGetter(item);
                        return shortValue.ToString(CultureInfo.InvariantCulture).ToLowerInvariant();
                }
                return null;
            }
            catch (Exception ex)
            {
                var message = $"an error occurred while calculating '{propertyName}' property of {typeof(TEntity).FullName} entity";
                throw new SqlGenerationException(SqlOperation.Insert, message, ex);
            }
        }


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

            var value = profile.PropertyExpression.Compile().DynamicInvoke(item);
            if (value != null && value.GetType().IsEnum)
            {
                switch (profile.DbColumnType)
                {
                    case NpgsqlDbType.Bigint:
                        value = (long) value;
                        break;
                    case NpgsqlDbType.Integer:
                        value = (int) value;
                        break;
                    case NpgsqlDbType.Varchar:
                        value = value.ToString();
                        break;
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
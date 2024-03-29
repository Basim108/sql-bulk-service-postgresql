using System;
using System.ComponentModel.DataAnnotations;
using System.Data;
using Hrimsoft.Core.ValueTypes;
using NpgsqlTypes;

namespace Hrimsoft.SqlBulk.PostgreSql
{
    public static class DataTypeExtensions
    {
        /// <summary>
        /// Convert common data annotation type to an NpgsqlDbType
        /// </summary>
        public static NpgsqlDbType ToNpgsql(this DataType type)
        {
            switch (type) {
                case DataType.Currency: return NpgsqlDbType.Money;
                case DataType.Date:     return NpgsqlDbType.Date;
                case DataType.Duration: return NpgsqlDbType.Interval;
                case DataType.Text:     return NpgsqlDbType.Text;
                case DataType.Time:     return NpgsqlDbType.Time;
                case DataType.DateTime: return NpgsqlDbType.Timestamp;
            }
            throw new NotSupportedException($"Type mapping from type '{type}' is not supported");
        }

        /// <summary>
        /// Convert common data annotation type to an NpgsqlDbType
        /// </summary>
        public static NpgsqlDbType ToNpgsql(this Type propertyType)
        {
            if (propertyType == typeof(char)) {
                return NpgsqlDbType.Char;
            }
            if (propertyType == typeof(byte) || propertyType == typeof(Nullable<byte>)) {
                return NpgsqlDbType.Smallint;
            }
            if (propertyType == typeof(int) || propertyType == typeof(Nullable<int>)) {
                return NpgsqlDbType.Integer;
            }
            if (propertyType == typeof(short) || propertyType == typeof(Nullable<short>)) {
                return NpgsqlDbType.Smallint;
            }
            if (propertyType == typeof(long) || propertyType == typeof(Nullable<long>)) {
                return NpgsqlDbType.Bigint;
            }
            if (propertyType == typeof(decimal) || propertyType == typeof(Nullable<decimal>)) {
                return NpgsqlDbType.Numeric;
            }
            if (propertyType == typeof(bool) || propertyType == typeof(Nullable<bool>)) {
                return NpgsqlDbType.Boolean;
            }
            if (propertyType == typeof(string)) {
                return NpgsqlDbType.Text;
            }
            if (propertyType == typeof(Decimal)) {
                return NpgsqlDbType.Numeric;
            }
            if (propertyType == typeof(double) || propertyType == typeof(Nullable<double>)) {
                return NpgsqlDbType.Double;
            }
            if (propertyType == typeof(float) || propertyType == typeof(Nullable<float>)) {
                return NpgsqlDbType.Real;
            }
            if (propertyType == typeof(TimeSpan)) {
                return NpgsqlDbType.Interval;
            }
            if (propertyType == typeof(Date) || propertyType == typeof(Nullable<Date>)) {
                return NpgsqlDbType.Date;
            }
            if (propertyType == typeof(DateTime) || propertyType == typeof(Nullable<DateTime>)) {
                return NpgsqlDbType.Timestamp;
            }
            if (propertyType == typeof(DateTimeOffset) || propertyType == typeof(Nullable<DateTimeOffset>)) {
                return NpgsqlDbType.TimestampTz;
            }
            if (propertyType == typeof(TimeSpan) || propertyType == typeof(Nullable<TimeSpan>)) {
                return NpgsqlDbType.Interval;
            }
            if (propertyType.IsEnum) {
                return NpgsqlDbType.Integer;
            }
            throw new NotSupportedException($"Type mapping from type '{propertyType.FullName}' is not supported");
        }

        /// <summary>
        /// Convert common data annotation type to an NpgsqlDbType
        /// </summary>
        public static NpgsqlDbType ToNpgsql(this SqlDbType type)
        {
            switch (type) {
                case SqlDbType.Money:          return NpgsqlDbType.Money;
                case SqlDbType.Date:           return NpgsqlDbType.Date;
                case SqlDbType.Text:           return NpgsqlDbType.Text;
                case SqlDbType.Time:           return NpgsqlDbType.Time;
                case SqlDbType.DateTime:       return NpgsqlDbType.Timestamp;
                case SqlDbType.DateTime2:      return NpgsqlDbType.TimestampTz;
                case SqlDbType.DateTimeOffset: return NpgsqlDbType.TimestampTz;
                case SqlDbType.Binary:         return NpgsqlDbType.Bytea;
                case SqlDbType.Bit:            return NpgsqlDbType.Bit;
                case SqlDbType.Char:           return NpgsqlDbType.Char;
                case SqlDbType.Decimal:        return NpgsqlDbType.Numeric;
                case SqlDbType.Float:          return NpgsqlDbType.Double;
                case SqlDbType.Real:           return NpgsqlDbType.Real;
                case SqlDbType.Int:            return NpgsqlDbType.Integer;
                case SqlDbType.BigInt:         return NpgsqlDbType.Bigint;
                case SqlDbType.Timestamp:      return NpgsqlDbType.Timestamp;
            }
            throw new NotSupportedException($"Type mapping from type '{type}' is not supported");
        }
    }
}
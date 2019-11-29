using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using JetBrains.Annotations;

namespace Hrimsoft.SqlBulk.PostgreSql
{
    /// <summary>
    /// Information about all properties of one entity type that have to be mapped on the database table
    /// </summary>
    public class EntityProfile
    {
        /// <inheritdoc />
        public EntityProfile([NotNull] Type entityType)
        {
            this.EntityType = entityType;
            this.Properties = new Dictionary<string, PropertyProfile>();
        }

        /// <summary>
        /// The maximum number of elements that have to be included into one command.
        /// If 0 then unlimited. If n then all elements will be split into n-sized arrays and will be send one after another.  
        /// </summary>
        public int MaximumSentElements { get; protected set; }

        /// <summary>
        /// Type of entity that owns all these mapped properties
        /// </summary>
        [NotNull]
        public Type EntityType { get; }

        private string _tableName;
        public string TableName
        {
            get {
                if (string.IsNullOrWhiteSpace(_tableName))
                    _tableName = $"\"{EntityType.Name.ToSnakeCase()}\"";

                return _tableName;
            }
            private set => _tableName = value;
        }

        /// <summary>
        /// Collection of properties 
        /// </summary>
        public IDictionary<string, PropertyProfile> Properties { get; }

        /// <summary>
        /// Sets database table name that has to be mapped onto this entity
        /// </summary>
        /// <param name="dbTableName">custom table name. if null or whitespace than EntityType name, converted to snake case, will be used</param>
        /// <param name="schema">schema where the table is located</param>
        public void ToTable(string dbTableName, string schema)
        {
            this.TableName = string.IsNullOrWhiteSpace(dbTableName)
                ? $"\"{EntityType.Name.ToSnakeCase()}\""
                : $"\"{dbTableName}\"";
            if (!string.IsNullOrWhiteSpace(schema))
            {
                this.TableName = $"\"{schema}\".{this.TableName}";
            }
        }

        /// <summary>
        /// Adds a mapping a property to its snake cased column equivalent 
        /// </summary>
        /// <param name="propertyExpression">Expression to the entity's property that has to be mapped onto that db column</param>
        public PropertyProfile HasProperty<TEntity, TProperty>([NotNull] Expression<Func<TEntity, TProperty>> propertyExpression)
        {
            return HasProperty("", propertyExpression);
        }

        /// <summary>
        /// Adds a specific mapping 
        /// </summary>
        /// <param name="column">Column name in database table</param>
        /// <param name="propertyExpression">Expression to the entity's property that has to be mapped onto that db column</param>
        public PropertyProfile HasProperty<TEntity, TProperty>(string column, [NotNull] Expression<Func<TEntity, TProperty>> propertyExpression)
        {
            var propertyName = "";

            if (propertyExpression.Body is MemberExpression memberExpression)
                propertyName = memberExpression.Member.Name;
            else if (propertyExpression.Body is ParameterExpression parameterExpression)
                propertyName = parameterExpression.Name;

            if (string.IsNullOrWhiteSpace(propertyName))
                propertyName = column;
            
            var columnName = string.IsNullOrWhiteSpace(column)
                ? propertyName.ToSnakeCase()
                : column;

            if (string.IsNullOrWhiteSpace(columnName))
                throw new ArgumentException("Cannot calculate column name, so the argument must be set manually", nameof(column));

            if (this.Properties.ContainsKey(columnName))
                throw new SqlBulkServiceException($"{nameof(EntityProfile)} already contains a property with name {propertyName}");

            var propertyProfile = new PropertyProfile(columnName, propertyExpression);
            this.Properties.Add(columnName, propertyProfile);

            return propertyProfile;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using JetBrains.Annotations;

namespace Hrimsoft.PostgresSqlBulkService
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
        
        public string TableName { get; private set; }

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
            var memberExpression = propertyExpression.Body as MemberExpression;
            if (memberExpression == null 
                || propertyExpression.Body is ConstantExpression)
                throw new ArgumentException($"Wrong type of expression body. It should be a MemberExpression, but it is {propertyExpression.Body.GetType().Name}", nameof(propertyExpression));
            
            var propertyName = memberExpression.Member.Name;
            
            PropertyProfile propertyProfile = null;
            if (this.Properties.ContainsKey(propertyName))
            {
                propertyProfile = this.Properties[propertyName];
            }
            else
            {
                var columnName = string.IsNullOrWhiteSpace(column)
                    ? propertyName.ToSnakeCase()
                    : column;
                propertyProfile = new PropertyProfile(columnName, memberExpression);
                this.Properties.Add(propertyName, propertyProfile);
            }

            return propertyProfile;
        }
    }
}
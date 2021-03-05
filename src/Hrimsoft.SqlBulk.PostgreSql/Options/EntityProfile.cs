using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using Hrimsoft.StringCases;
using JetBrains.Annotations;

namespace Hrimsoft.SqlBulk.PostgreSql
{
    /// <summary>
    /// Information about all properties of one entity type that have to be mapped on the database table
    /// </summary>
    public class EntityProfile
    {
        /// <inheritdoc />
        public EntityProfile(Type entityType)
        {
            if (entityType == null)
                throw new ArgumentNullException(nameof(entityType));

            this.EntityType = entityType;
            this.Properties = new Dictionary<string, PropertyProfile>();
        }

        /// <summary>
        /// Type of entity that owns all these mapped properties
        /// </summary>
        public Type EntityType { get; }

        #region command execution options

        /// <summary>
        /// The maximum number of elements that have to be included into one command.
        /// If 0 then unlimited. If n then all elements will be split into n-sized arrays and will be send one after another.  
        /// </summary>
        public int? MaximumSentElements { get; protected set; }

        #endregion

        #region Error management options

        /// <summary>
        /// Defines a strategy of how bulk service will process sql command failures.
        /// This strategy is defined only for this particular entity type
        /// 
        /// It will override the option defined in the <see cref="BulkServiceOptions"/> 
        /// </summary>
        public FailureStrategies? FailureStrategy { get; set; }

        /// <summary>
        /// If true, when an <see cref="SqlBulkExecutionException{TEntity}"/> exception is thrown,
        /// not operated elements will be set in the exception property <see cref="SqlBulkExecutionException{TEntity}.NotOperatedElements"/> 
        /// It will override the option defined in the <see cref="BulkServiceOptions"/>
        /// Default: false
        /// </summary>
        public bool? IsNotOperatedElementsEnabled { get; set; }

        /// <summary>
        /// If true, when an <see cref="SqlBulkExecutionException{TEntity}"/> exception is thrown,
        /// those elements that were successfully operated will be set in the exception property <see cref="SqlBulkExecutionException{TEntity}.OperatedElements"/>
        /// It will override the option defined in the <see cref="BulkServiceOptions"/>
        /// Default: false 
        /// </summary>
        public bool? IsOperatedElementsEnabled { get; set; }

        /// <summary>
        /// If true, when an <see cref="SqlBulkExecutionException{TEntity}"/> exception is thrown,
        /// those portion of elements that caused an exception will be set in the exception property <see cref="SqlBulkExecutionException{TEntity}.ProblemElements"/> 
        /// It will override the option defined in the <see cref="BulkServiceOptions"/>
        /// Default: false
        /// </summary>
        public bool? IsProblemElementsEnabled { get; set; }

        #endregion

        #region mapping information

        private string _tableName;

        /// <summary>
        /// A table name that represents this entity in the database
        /// </summary>
        public string TableName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_tableName))
                    _tableName = $"\"{EntityType.Name.ToSnakeCase()}\"";

                return _tableName;
            }
            private set => _tableName = value;
        }

        /// <summary>
        /// Collection of properties. key is property name, value is property profile.
        /// </summary>
        public IDictionary<string, PropertyProfile> Properties { get; }

        /// <summary>
        /// Information about properties that all together make a unique value among all  the entity instances in the database
        /// </summary>
        public EntityUniqueConstraint UniqueConstraint { get; private set; }

        #endregion

        #region methods to tune mapping

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
        /// Adds a database defined unique constraint 
        /// </summary>
        public void HasUniqueConstraint(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            if (UniqueConstraint == null)
                UniqueConstraint = new EntityUniqueConstraint(name);
            else
                UniqueConstraint.Name = name;
        }

        /// <summary>
        /// Adds a mapping a property to its snake cased column equivalent 
        /// </summary>
        /// <param name="propertyExpression">Expression to the entity's property that has to be mapped onto that db column</param>
        public PropertyProfile HasPropertyAsPartOfUniqueConstraint<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression)
        {
            return HasPropertyAsPartOfUniqueConstraint("", propertyExpression);
        }

        /// <summary>
        /// Adds a mapping a property to its snake cased column equivalent 
        /// </summary>
        /// <param name="column">Column name in database table</param>
        /// <param name="propertyExpression">Expression to the entity's property that has to be mapped onto that db column</param>
        public PropertyProfile HasPropertyAsPartOfUniqueConstraint<TEntity, TProperty>(string column, Expression<Func<TEntity, TProperty>> propertyExpression)
        {
            var propertyProfile = HasProperty(column, propertyExpression, true);
            if (UniqueConstraint == null)
                UniqueConstraint = new EntityUniqueConstraint(propertyProfile);

            UniqueConstraint.UniqueProperties.Add(propertyProfile);

            return propertyProfile;
        }

        /// <summary>
        /// Adds a mapping a property to its snake cased column equivalent 
        /// </summary>
        /// <param name="propertyExpression">Expression to the entity's property that has to be mapped onto that db column</param>
        public PropertyProfile HasProperty<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression)
        {
            return HasProperty("", propertyExpression, false);
        }

        /// <summary>
        /// Adds a specific mapping 
        /// </summary>
        /// <param name="column">Column name in database table</param>
        /// <param name="propertyExpression">Expression to the entity's property that has to be mapped onto that db column</param>
        public PropertyProfile HasProperty<TEntity, TProperty>(string column, Expression<Func<TEntity, TProperty>> propertyExpression)
        {
            return HasProperty(column, propertyExpression, false);
        }

        /// <summary>
        /// Adds a specific mapping 
        /// </summary>
        /// <param name="column">Column name in database table</param>
        /// <param name="propertyExpression">Expression to the entity's property that has to be mapped onto that db column</param>
        /// <param name="isPartOfUniqueConstraint">Is this property a part of unique constraint</param>
        public PropertyProfile HasProperty<TEntity, TProperty>(string column, Expression<Func<TEntity, TProperty>> propertyExpression, bool isPartOfUniqueConstraint)
        {
            if (propertyExpression == null)
                throw new ArgumentNullException(nameof(propertyExpression));

            var propertyName = "";

            if (propertyExpression.Body is MemberExpression memberExpression)
                propertyName = memberExpression.Member.Name;
            else if (propertyExpression.Body is ParameterExpression parameterExpression)
                propertyName = parameterExpression.Name;
            else if (propertyExpression.Body is UnaryExpression unaryExpression &&
                     unaryExpression.Operand is MemberExpression operand)
                propertyName = operand.Member.Name;
            if (string.IsNullOrWhiteSpace(propertyName))
                propertyName = column;

            var columnName = string.IsNullOrWhiteSpace(column)
                ? propertyName.ToSnakeCase()
                : column;

            if (string.IsNullOrWhiteSpace(columnName))
                throw new ArgumentException("Cannot calculate column name, so the argument must be set manually", nameof(column));

            if (this.Properties.ContainsKey(propertyName))
                throw new TypeMappingException(EntityType, propertyName, $"{nameof(EntityProfile)} already contains a property with name {propertyName}");

            var propertyProfile = new PropertyProfile(columnName, propertyExpression);
            propertyProfile.HasColumnType(typeof(TProperty).ToNpgsql(), typeof(TProperty));
            if (isPartOfUniqueConstraint)
                propertyProfile.ThatIsPartOfUniqueConstraint();

            this.Properties.Add(propertyName, propertyProfile);

            return propertyProfile;
        }

        #endregion
    }
}
using System;
using System.Collections.Generic;
using Hrimsoft.Core.Exceptions;
using JetBrains.Annotations;

namespace Hrimsoft.SqlBulk.PostgreSql
{
    /// <summary>
    /// Information about mapping business entities to the postgres entities 
    /// </summary>
    public class BulkServiceOptions : IBulkServiceOptions
    {
        /// <inheritdoc />
        public BulkServiceOptions() { }

        /// <inheritdoc />
        public BulkServiceOptions(int maximumSentElements)
        {
            this.MaximumSentElements = maximumSentElements;
            this.FailureStrategy     = FailureStrategies.StopEverything;
        }
        
        #region Error management options
        /// <summary>
        /// Defines a strategy of how bulk service will process sql command failures.
        /// This strategy is defined for all registered entity types.
        /// It could be overriden by the same property in the <see cref="EntityProfile"/>
        ///
        /// Default: StopEverything 
        /// </summary>
        public FailureStrategies FailureStrategy { get; set; }
        
        /// <summary>
        /// If true, when an <see cref="SqlBulkExecutionException{TEntity}"/> exception is thrown,
        /// not operated elements will be set in the exception property <see cref="SqlBulkExecutionException{TEntity}.NotOperatedElements"/> 
        /// Default: false
        /// </summary>
        public bool IsNotOperatedElementsEnabled { get; set; }
        
        /// <summary>
        /// If true, when an <see cref="SqlBulkExecutionException{TEntity}"/> exception is thrown,
        /// those elements that were successfully operated will be set in the exception property <see cref="SqlBulkExecutionException{TEntity}.OperatedElements"/>
        ///
        /// Default: false 
        /// </summary>
        public bool IsOperatedElementsEnabled { get; set; }
        
        /// <summary>
        /// If true, when an <see cref="SqlBulkExecutionException{TEntity}"/> exception is thrown,
        /// those portion of elements that caused an exception will be set in the exception property <see cref="SqlBulkExecutionException{TEntity}.ProblemElements"/> 
        /// Default: false
        /// </summary>
        public bool IsProblemElementsEnabled { get; set; }
        #endregion

        #region command execution options
        /// <summary>
        /// The maximum number of elements that have to be included into one command.
        /// If 0 then unlimited. If n then all elements will be split into n-sized arrays and will be send one after another.
        /// It could be overriden by the same property in the <see cref="EntityProfile"/>
        /// 
        /// Default: 0 
        /// </summary>
        public int MaximumSentElements { get; set; }        
        #endregion
        
        #region mapping options
        private readonly Dictionary<Type, EntityProfile> _supportedEntityTypes = new Dictionary<Type, EntityProfile>();

        /// <summary>
        /// Information about mapping business entities to the postgres entities
        /// </summary>
        public IReadOnlyDictionary<Type, EntityProfile> SupportedEntityTypes => _supportedEntityTypes;

        /// <summary>
        /// Register an entity profile that describes entity mapping and other options
        /// </summary>
        public void AddEntityProfile<TEntity>(EntityProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));
            var entityType = typeof(TEntity);
            if (_supportedEntityTypes.ContainsKey(entityType))
                throw new ConfigurationException($"Cannot add profile for {entityType.FullName} as it already exists in configuration.");
            _supportedEntityTypes.Add(typeof(TEntity), profile);
        }
        #endregion
    }
}
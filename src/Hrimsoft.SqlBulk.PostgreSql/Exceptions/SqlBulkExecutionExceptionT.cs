using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace Hrimsoft.SqlBulk.PostgreSql
{
    /// <summary>
    /// Exception raised during executing a bulk sql command
    /// </summary>
    public class SqlBulkExecutionException<TEntity>: Exception
    where TEntity: class
    {
        /// <inheritdoc />
        public SqlBulkExecutionException(string message) :base(message) { }

        /// <inheritdoc />
        public SqlBulkExecutionException(string message, [NotNull] Exception innerException)
        :base(message, innerException) { }

        /// <inheritdoc />
        public SqlBulkExecutionException([NotNull] Exception innerException)
            :base(innerException.Message, innerException) { }

        /// <summary>
        /// Exception occured during operating a portion of elements  
        /// </summary>
        /// <param name="innerException">Exception that impeded to operate successfully</param>
        /// <param name="usedStrategy">Strategy that was used to manage the failed bulk operation</param>
        /// <param name="notOperated">
        /// Elements that were not operated as an inner exception was thrown.
        /// All changes with these elements were rollbacked
        /// </param>
        public SqlBulkExecutionException(
            [NotNull] Exception innerException,
            FailureStrategies usedStrategy,
            [NotNull] ICollection<TEntity> notOperated)
            :base(innerException.Message, innerException)
        {
            UsedStrategy = usedStrategy;
            NotOperatedElements = notOperated;
        }
        
        /// <summary>
        /// Strategy that was used to manage the failed bulk operation
        /// </summary>
        public FailureStrategies UsedStrategy { get; private set; }
        
        /// <summary>
        /// Elements that were not operated as an inner exception was thrown.
        /// All changes with these elements were rollbacked
        /// </summary>
        public ICollection<TEntity> NotOperatedElements { get; private set; }
    }
}
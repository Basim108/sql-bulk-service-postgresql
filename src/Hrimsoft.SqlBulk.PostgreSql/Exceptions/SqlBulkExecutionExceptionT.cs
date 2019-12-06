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
        /// <param name="problemElements">
        /// A portion of elements that caused an exception
        /// <see cref="BulkServiceOptions.IsProblemElementsEnabled"/>
        /// <see cref="EntityProfile.IsProblemElementsEnabled"/>
        /// </param>
        /// <param name="notOperated">
        /// Elements that were not operated as an inner exception was thrown.
        /// All changes with these elements were rollbacked.
        /// <see cref="BulkServiceOptions.IsNotOperatedElementsEnabled"/>
        /// <see cref="EntityProfile.IsNotOperatedElementsEnabled"/>
        /// </param>
        /// <param name="operated">
        /// Elements that were successfully operated.
        /// <see cref="BulkServiceOptions.IsOperatedElementsEnabled"/>
        /// <see cref="EntityProfile.IsOperatedElementsEnabled"/>
        /// </param>
        public SqlBulkExecutionException(
            [NotNull] Exception innerException,
            FailureStrategies usedStrategy,
            ICollection<TEntity> problemElements,
            ICollection<TEntity> notOperated,
            ICollection<TEntity> operated)
            :base(innerException.Message, innerException)
        {
            UsedStrategy = usedStrategy;
            NotOperatedElements = notOperated;
            OperatedElements = operated;
            ProblemElements = problemElements;
        }
        
        /// <summary>
        /// Strategy that was used to manage the failed bulk operation
        /// </summary>
        public FailureStrategies UsedStrategy { get; private set; }
        
        /// <summary>
        /// Elements that were not operated as an inner exception was thrown.
        /// According to <see cref="BulkServiceOptions.IsNotOperatedElementsEnabled"/> or <see cref="EntityProfile.IsNotOperatedElementsEnabled"/>  it will be set or not. 
        /// </summary>
        public ICollection<TEntity> NotOperatedElements { get; private set; }
        
        /// <summary>
        /// Elements that were successfully operated
        /// According to <see cref="BulkServiceOptions.IsOperatedElementsEnabled"/> or <see cref="EntityProfile.IsOperatedElementsEnabled"/>  it will be set or not. 
        /// </summary>
        public ICollection<TEntity> OperatedElements { get; private set; }
        
        /// <summary>
        /// That portion of elements that caused an exception
        /// According to <see cref="BulkServiceOptions.IsProblemElementsEnabled"/> or <see cref="EntityProfile.IsProblemElementsEnabled"/>  it will be set or not. 
        /// </summary>
        public ICollection<TEntity> ProblemElements  { get; private set; }
    }
}
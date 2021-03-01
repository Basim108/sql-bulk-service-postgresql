using System;
using System.Collections.Generic;

namespace Hrimsoft.SqlBulk.PostgreSql
{
    public interface IBulkServiceOptions
    {
        /// <summary>
        /// Defines a strategy of how bulk service will process sql command failures.
        /// This strategy is defined for all registered entity types.
        /// It could be overriden by the same property in the <see cref="EntityProfile"/>
        ///
        /// Default: StopEverything 
        /// </summary>
        FailureStrategies FailureStrategy { get; }

        /// <summary>
        /// If true, when an <see cref="SqlBulkExecutionException{TEntity}"/> exception is thrown,
        /// not operated elements will be set in the exception property <see cref="SqlBulkExecutionException{TEntity}.NotOperatedElements"/> 
        /// Default: false
        /// </summary>
        bool IsNotOperatedElementsEnabled { get; }

        /// <summary>
        /// If true, when an <see cref="SqlBulkExecutionException{TEntity}"/> exception is thrown,
        /// those elements that were successfully operated will be set in the exception property <see cref="SqlBulkExecutionException{TEntity}.OperatedElements"/>
        ///
        /// Default: false 
        /// </summary>
        bool IsOperatedElementsEnabled { get; }

        /// <summary>
        /// If true, when an <see cref="SqlBulkExecutionException{TEntity}"/> exception is thrown,
        /// those portion of elements that caused an exception will be set in the exception property <see cref="SqlBulkExecutionException{TEntity}.ProblemElements"/> 
        /// Default: false
        /// </summary>
        bool IsProblemElementsEnabled { get; }

        /// <summary>
        /// The maximum number of elements that have to be included into one command.
        /// If 0 then unlimited. If n then all elements will be split into n-sized arrays and will be send one after another.
        /// It could be overriden by the same property in the <see cref="EntityProfile"/>
        /// 
        /// Default: 0 
        /// </summary>
        int MaximumSentElements { get; }

        /// <summary>
        /// Information about mapping business entities to the postgres entities
        /// </summary>
        IReadOnlyDictionary<Type, EntityProfile> SupportedEntityTypes { get; }
    }
}
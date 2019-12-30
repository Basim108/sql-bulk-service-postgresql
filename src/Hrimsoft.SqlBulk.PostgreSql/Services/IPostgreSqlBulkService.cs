using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Npgsql;

namespace Hrimsoft.SqlBulk.PostgreSql
{
    public interface IPostgreSqlBulkService
    {
        /// <summary>
        /// Inserts elements
        /// </summary>
        /// <param name="connection">Connection to a database</param>
        /// <param name="elements">Elements that have to be inserted</param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TEntity">Type of instances that have to be inserted</typeparam>
        /// <returns>
        /// Returning value depends on failure strategy option.
        /// Returns null if current failure strategy is not a FailureStrategies.Ignore <see cref="FailureStrategies"/>.
        /// Otherwise returns lists of operated and not operated elements 
        /// </returns>
        Task<BulkOperationResult<TEntity>> InsertAsync<TEntity>(NpgsqlConnection connection, ICollection<TEntity> elements, CancellationToken cancellationToken)
            where TEntity : class;
        
        /// <summary>
        /// Update elements
        /// </summary>
        /// <param name="connection">Connection to a database</param>
        /// <param name="elements">Elements that have to be updated</param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TEntity">Type of instances that have to be updated</typeparam>
        /// <returns>
        /// Returning value depends on failure strategy option.
        /// Returns null if current failure strategy is not a FailureStrategies.Ignore <see cref="FailureStrategies"/>.
        /// Otherwise returns lists of operated and not operated elements 
        /// </returns>
        Task<BulkOperationResult<TEntity>> UpdateAsync<TEntity>( NpgsqlConnection connection, ICollection<TEntity> elements, CancellationToken cancellationToken)
            where TEntity : class;
        
        /// <summary>
        /// Delete elements
        /// </summary>
        /// <param name="connection">Connection to a database</param>
        /// <param name="elements">Elements that have to be deleted</param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TEntity">Type of instances that have to be deleted</typeparam>
        /// <returns>
        /// Returning value depends on failure strategy option.
        /// Returns null if current failure strategy is not a FailureStrategies.Ignore <see cref="FailureStrategies"/>.
        /// Otherwise returns lists of operated and not operated elements 
        /// </returns>
        Task<BulkOperationResult<TEntity>> DeleteAsync<TEntity>( NpgsqlConnection connection, ICollection<TEntity> elements, CancellationToken cancellationToken)
            where TEntity : class;

        /// <summary>
        /// Insert new elements and update those already existed ones 
        /// </summary>
        /// <param name="connection">Connection to a database</param>
        /// <param name="elements">Elements that have to be inserted or updated</param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TEntity">Type of instances that have to be inserted or updated</typeparam>
        /// <returns>
        /// Returning value depends on failure strategy option.
        /// Returns null if current failure strategy is not a FailureStrategies.Ignore <see cref="FailureStrategies"/>.
        /// Otherwise returns lists of operated and not operated elements 
        /// </returns>
        Task<BulkOperationResult<TEntity>> UpsertAsync<TEntity>( NpgsqlConnection connection, ICollection<TEntity> elements, CancellationToken cancellationToken)
            where TEntity : class;
    }
}
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
        /// <returns>The same collection of items with updated from the storage properties that marked as mast update after update (see PropertyProfile.MustBeUpdatedAfterInsert)</returns>
        Task<ICollection<TEntity>> InsertAsync<TEntity>([NotNull] NpgsqlConnection connection, [NotNull] ICollection<TEntity> elements, CancellationToken cancellationToken)
            where TEntity : class;
        
        /// <summary>
        /// Update elements
        /// </summary>
        /// <param name="connection">Connection to a database</param>
        /// <param name="elements">Elements that have to be updated</param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TEntity">Type of instances that have to be updated</typeparam>
        /// <returns>The same collection of items with updated from the storage properties that marked as mast update after insert (see PropertyProfile.MustBeUpdatedAfterUpdate)</returns>
        Task<ICollection<TEntity>> UpdateAsync<TEntity>([NotNull] NpgsqlConnection connection, [NotNull] ICollection<TEntity> elements, CancellationToken cancellationToken)
            where TEntity : class;
    }
}
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Npgsql;

namespace Hrimsoft.PostgresSqlBulkService
{
    public interface IPostgreSqlBulkService
    {
        /// <summary>
        /// Inserts elements
        /// </summary>
        /// <param name="connection">Connection to a database</param>
        /// <param name="elements">Elements that have to be inserted</param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        Task<ICollection<TEntity>> InsertAllAsync<TEntity>([NotNull] NpgsqlConnection connection, [NotNull] ICollection<TEntity> elements, CancellationToken cancellationToken);
    }
}
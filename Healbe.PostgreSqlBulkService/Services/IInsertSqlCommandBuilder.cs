using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;
using Npgsql;

namespace Hrimsoft.PostgresSqlBulkService
{
    /// <summary>
    /// Sql bulk inset command generator
    /// </summary>
    public interface IInsertSqlCommandBuilder
    {
        /// <summary>
        /// Generates sql inset command for bunch of elements
        /// </summary>
        /// <param name="elements">elements that have to be inserted into the table</param>
        /// <param name="entityProfile">elements type profile (contains mapping and other options)</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns a text of an sql inset command and collection of database parameters</returns>
        (string Command, ICollection<NpgsqlParameter> Parameters) Generate<TEntity>([NotNull] ICollection<TEntity> elements, [NotNull] EntityProfile entityProfile, CancellationToken cancellationToken)
            where TEntity : class;
    }
}
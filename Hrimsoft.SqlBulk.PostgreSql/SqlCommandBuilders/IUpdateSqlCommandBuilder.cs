using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;
using Npgsql;

namespace Hrimsoft.SqlBulk.PostgreSql
{
    /// <summary>
    /// Generates bulk update sql command
    /// </summary>
    public interface IUpdateSqlCommandBuilder
    {
        /// <summary>
        /// Generates bulk update sql command
        /// </summary>
        /// <param name="elements">elements that have to be updated</param>
        /// <param name="entityProfile">elements type profile (contains mapping and other options)</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns a text of an sql update command and collection of database parameters</returns>
        (string Command, ICollection<NpgsqlParameter> Parameters) Generate<TEntity>([NotNull] ICollection<TEntity> elements, [NotNull] EntityProfile entityProfile, CancellationToken cancellationToken)
            where TEntity : class;
    }
}
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;
using Npgsql;

namespace Hrimsoft.SqlBulk.PostgreSql
{
    /// <summary>
    /// Sql bulk command generator
    /// </summary>
    public interface ISqlCommandBuilder
    {
        /// <summary>
        /// Generates sql command for bunch of elements
        /// </summary>
        /// <param name="elements">elements that have to be operated</param>
        /// <param name="entityProfile">elements type profile (contains mapping and other options)</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns a text of an sql command and a collection of database parameters</returns>
        [NotNull]SqlCommandBuilderResult Generate<TEntity>([NotNull] ICollection<TEntity> elements, [NotNull] EntityProfile entityProfile, CancellationToken cancellationToken)
            where TEntity : class;
    }
}
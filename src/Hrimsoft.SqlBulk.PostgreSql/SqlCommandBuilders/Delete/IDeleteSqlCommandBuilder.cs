using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;
using Npgsql;

namespace Hrimsoft.SqlBulk.PostgreSql
{
    /// <summary>
    /// Generates bulk delete sql command
    /// </summary>
    public interface IDeleteSqlCommandBuilder: ISqlCommandBuilder
    {
    }
}
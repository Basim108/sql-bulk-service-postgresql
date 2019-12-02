using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;
using Npgsql;

namespace Hrimsoft.SqlBulk.PostgreSql
{
    /// <summary>
    /// Sql bulk inset command generator
    /// </summary>
    public interface IInsertSqlCommandBuilder : ISqlCommandBuilder
    {
    }
}
using System.Collections.Generic;
using Npgsql;

namespace Hrimsoft.SqlBulk.PostgreSql
{
    /// <summary>
    /// Describes the result of any sql command generation result
    /// </summary>
    public class SqlCommandBuilderResult
    {
        public SqlCommandBuilderResult(string                 command,
                                       IList<NpgsqlParameter> sqlParameters,
                                       bool                   isThereReturningClause,
                                       int                    elementsCount)
        {
            Command                = command;
            SqlParameters          = sqlParameters;
            IsThereReturningClause = isThereReturningClause;
            ElementsCount          = elementsCount;
        }

        /// <summary>
        /// Text of generated sql command
        /// </summary>
        public string Command { get; }

        /// <summary>
        /// Parameters that were included into the command
        /// </summary>
        public IList<NpgsqlParameter> SqlParameters { get; }

        /// <summary>
        /// Does the sql command contains a returning clause
        /// </summary>
        public bool IsThereReturningClause { get; }

        public int ElementsCount { get; }
    }
}
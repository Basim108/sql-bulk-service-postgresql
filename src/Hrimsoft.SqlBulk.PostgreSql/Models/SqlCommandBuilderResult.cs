using System.Collections.Generic;
using Npgsql;

namespace Hrimsoft.SqlBulk.PostgreSql
{
    /// <summary>
    /// Describes the result of any sql command generation result
    /// </summary>
    public class SqlCommandBuilderResult
    {
        /// <summary>
        /// Text of generated sql command
        /// </summary>
        public string Command { get; set; }
        
        /// <summary>
        /// Parameters that were included into the command
        /// </summary>
        public ICollection<NpgsqlParameter> Parameters { get; set; }
        
        /// <summary>
        /// Does the sql command contains a returning clause
        /// </summary>
        public bool IsThereReturningClause { get; set; }
    }
}
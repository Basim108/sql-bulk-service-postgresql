using System.Collections.Generic;
using System.Text;
using System.Threading;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Hrimsoft.SqlBulk.PostgreSql
{
    /// <summary>
    /// Sql bulk inset command generator 
    /// </summary>
    public class InsertSqlCommandBuilder : IInsertSqlCommandBuilder
    {
        private readonly ILogger<InsertSqlCommandBuilder> _logger;

        /// <inheritdoc />
        public InsertSqlCommandBuilder(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<InsertSqlCommandBuilder>();
        }

        /// <summary>
        /// Generates sql inset command for bunch of elements
        /// </summary>
        /// <param name="elements">elements that have to be inserted into the table</param>
        /// <param name="entityProfile">elements type profile (contains mapping and other options)</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns a text of an sql inset command and collection of database parameters</returns>
        public (string Command, ICollection<NpgsqlParameter> Parameters) Generate<TEntity>([NotNull] ICollection<TEntity> elements, [NotNull] EntityProfile entityProfile, CancellationToken cancellationToken)
            where TEntity : class
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug($"{nameof(TEntity)}: {typeof(TEntity).FullName}");
                _logger.LogDebug($"{nameof(elements)}.Count: {elements.Count}");
            }

            var (columns, returningClause) = this.GenerateColumnsAndReturningClauses(entityProfile.Properties.Values);

            cancellationToken.ThrowIfCancellationRequested();

            var command = $"insert into {entityProfile.TableName} ({columns}) values ";
            var approximateEntireCommandLength = command.Length + returningClause.Length + columns.Length * elements.Count;

            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug($"approximateEntireCommandLength: {approximateEntireCommandLength}");

            var commandParameters = new List<NpgsqlParameter>(); 
            var resultBuilder = new StringBuilder(approximateEntireCommandLength);
            resultBuilder.Append(command);
            var paramIndex = 1;
            var elementIndex = -1;
            using (var elementsEnumerator = elements.GetEnumerator())
            {
                while (elementsEnumerator.MoveNext())
                {
                    elementIndex++;
                    var item = elementsEnumerator.Current;
                    cancellationToken.ThrowIfCancellationRequested();
                    resultBuilder.Append('(');
                    var firstPropertyValue = true;
                    foreach (var propInfo in entityProfile.Properties.Values)
                    {
                        if (propInfo.IsAutoGeneratedKey)
                            continue;
                        var delimiter = firstPropertyValue
                            ? ""
                            : ", ";

                        var paramName = $"@param{paramIndex++}";
                        commandParameters.Add(new NpgsqlParameter(paramName, propInfo.DbColumnType)
                        {
                            Value = propInfo.GetPropertyValue(item)
                        });

                        resultBuilder.Append(delimiter);
                        resultBuilder.Append(paramName);

                        firstPropertyValue = false;
                    }

                    resultBuilder.Append(')');
                    //  Finished with properties 
                    if (elements.Count > 1 && elementIndex < elements.Count - 1)
                        resultBuilder.Append(", ");
                }
            }

            resultBuilder.Append(" ");
            resultBuilder.Append(returningClause);
            resultBuilder.Append(";");
            
            var result = resultBuilder.ToString();
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug($"result command: {result}");

            return (Command: result, Parameters: commandParameters);
        }

        /// <summary>
        /// In one pass generates both columns and returning clauses 
        /// </summary>
        /// <param name="properties">Information about entity properties</param>
        /// <returns>
        /// Returns named tuple with generated columns and returning clauses.
        /// If there is no properties that has to be included into returning clause then ReturningClause item in the result tuple will be an empty string.
        /// </returns>
        // ReSharper disable once MemberCanBePrivate.Global  Needed to be public for unit testing purpose
        public (string Columns, string ReturningClause) GenerateColumnsAndReturningClauses([NotNull] ICollection<PropertyProfile> properties)
        {
            var returningClause = "";
            var firstReturningColumn = true;

            var columns = "";
            var firstColumn = true;
            foreach (var propInfo in properties)
            {
                if (propInfo.UpdateAfterOperationComplete)
                {
                    var returningDelimiter = firstReturningColumn
                        ? "returning "
                        : ", ";

                    returningClause += $"{returningDelimiter}\"{propInfo.DbColumnName}\"";
                    firstReturningColumn = false;
                }

                if (propInfo.IsAutoGeneratedKey)
                    continue;

                var delimiter = firstColumn
                    ? ""
                    : ", ";
                columns += $"{delimiter}\"{propInfo.DbColumnName}\"";

                firstColumn = false;
            }

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug($"columns: {columns}");
                _logger.LogDebug($"returningClause: {returningClause}");
            }

            return (columns, returningClause);
        }
    }
}
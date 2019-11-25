using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Hrimsoft.PostgresSqlBulkService
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

            var command = $"insert into {entityProfile.TableName} ({columns}) values (";
            var approximateEntireCommandLength = command.Length + returningClause.Length + columns.Length * elements.Count;

            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug($"approximateEntireCommandLength: {approximateEntireCommandLength}");

            var commandParameters = new List<NpgsqlParameter>(); 
            var resultBuilder = new StringBuilder(approximateEntireCommandLength);
            resultBuilder.Append(command);
            var firstItem = true;
            var paramIndex = 1;
            foreach (var item in elements)
            {
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
                        Value = GetValue(item, propInfo)
                    });
                        
                    resultBuilder.Append(delimiter);
                    resultBuilder.Append('"');
                    resultBuilder.Append(paramName);
                    resultBuilder.Append('"');

                    firstPropertyValue = false;
                }

                resultBuilder.Append(')');
                //  Finished with properties 
                if (!firstItem)
                    resultBuilder.Append(", ");

                firstItem = false;
            }

            resultBuilder.Append(returningClause);

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

            var columns = "(";
            var firstColumn = true;
            foreach (var propInfo in properties)
            {
                if (propInfo.IncludeInReturning)
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

            columns += ")";

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug($"columns: {columns}");
                _logger.LogDebug($"returningClause: {returningClause}");
            }

            return (columns, returningClause);
        }

        /// <summary>
        /// Calculates value of an item's property
        /// </summary>
        /// <param name="item">item with values</param>
        /// <param name="propInfo">information about which property it is needed to get value</param>
        /// <typeparam name="TEntity">Type of entity</typeparam>
        /// <returns>Returns</returns>
        // ReSharper disable once MemberCanBePrivate.Global  Needed to be public for unit testing purpose
        public object GetValue<TEntity>([NotNull] TEntity item, [NotNull] PropertyProfile propInfo)
            where TEntity : class
        {
            var idMemberExpression = propInfo.PropertyExpresion;
            var convert = Expression.Convert(idMemberExpression, idMemberExpression.Type);
            var lambda = Expression.Lambda(convert, idMemberExpression.Expression as ParameterExpression);
            var value = lambda.Compile().DynamicInvoke(item);
            return value;
        }
    }
}
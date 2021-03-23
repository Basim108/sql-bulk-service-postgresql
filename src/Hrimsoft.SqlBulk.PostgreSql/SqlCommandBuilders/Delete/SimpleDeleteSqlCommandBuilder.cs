using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Hrimsoft.SqlBulk.PostgreSql
{
    /// <summary>
    /// Generates bulk delete sql command 
    /// </summary>
    public class SimpleDeleteSqlCommandBuilder : IDeleteSqlCommandBuilder
    {
        private readonly ILogger<SimpleDeleteSqlCommandBuilder> _logger;

        private const int MAX_PARAMS_PER_CMD = 65_535;

        /// <summary> </summary>
        public SimpleDeleteSqlCommandBuilder(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SimpleDeleteSqlCommandBuilder>();
        }

        /// <summary>
        /// Generates bulk delete sql command
        /// </summary>
        /// <param name="elements">elements that have to be deleted</param>
        /// <param name="entityProfile">elements type profile (contains mapping and other options)</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns a text of an sql delete command and collection of database parameters</returns>
        public IList<SqlCommandBuilderResult> Generate<TEntity>(ICollection<TEntity> elements, EntityProfile entityProfile,
                                                                CancellationToken    cancellationToken)
            where TEntity : class
        {
            if (elements == null)
                throw new ArgumentNullException(nameof(elements));
            if (entityProfile == null)
                throw new ArgumentNullException(nameof(entityProfile));

            if (elements.Count == 0)
                throw new ArgumentException($"There is no elements in the collection. At least one element must be.", nameof(elements));

            _logger.LogTrace($"Generating delete sql for {elements.Count} elements.");

            var result        = new List<SqlCommandBuilderResult>();
            var resultCommand = "";
            var paramsCount = elements.Count * entityProfile.Properties
                                                            .Values
                                                            .Count(propInfo => propInfo.IsPrivateKey);
            var sqlParameters = new List<NpgsqlParameter>(Math.Min(paramsCount, MAX_PARAMS_PER_CMD));

            if (_logger.IsEnabled(LogLevel.Debug)) {
                _logger.LogDebug($"{nameof(TEntity)}: {typeof(TEntity).FullName}");
                _logger.LogDebug($"{nameof(elements)}.Count: {elements.Count}");
            }

            cancellationToken.ThrowIfCancellationRequested();
            var allElementsAreNull = true;
            var elementIndex       = -1;
            using (var elementsEnumerator = elements.GetEnumerator()) {
                var thereIsMoreElements = true;
                // ignore all null items until find the first not null item
                while (elementsEnumerator.Current == null && thereIsMoreElements) {
                    elementIndex++;
                    thereIsMoreElements = elementsEnumerator.MoveNext();
                }
                if (thereIsMoreElements) {
                    allElementsAreNull = false;

                    var (commandForOneItem, itemParameters) = GenerateForItem(entityProfile, elementsEnumerator.Current, null, elementIndex);
                    sqlParameters.AddRange(itemParameters);

                    var commandBuilder = new StringBuilder(commandForOneItem.Length * elements.Count - elementIndex);
                    commandBuilder.AppendLine(commandForOneItem);

                    while (elementsEnumerator.MoveNext()) {
                        elementIndex++;
                        // ignore all null items 
                        if (elementsEnumerator.Current == null)
                            continue;
                        if (sqlParameters.Count + entityProfile.MaxPossibleSqlParameters > MAX_PARAMS_PER_CMD) {
                            result.Add(new SqlCommandBuilderResult(
                                           commandBuilder.ToString(),
                                           sqlParameters,
                                           isThereReturningClause: false,
                                           elementsCount: elementIndex
                                       ));
                            if (_logger.IsEnabled(LogLevel.Debug)) {
                                var (cmdSize, suffix) = ((long) commandBuilder.Length * 2).PrettifySize();
                                _logger.LogDebug($"Generated sql simple delete command for {elementIndex} {entityProfile.EntityType.Name} elements, command size {cmdSize:F2} {suffix}");
                            }
                            commandBuilder.Clear();
                            sqlParameters = new List<NpgsqlParameter>(sqlParameters.Count);
                            elementIndex  = 0;
                        }
                        (commandForOneItem, itemParameters) = GenerateForItem(entityProfile, elementsEnumerator.Current, commandBuilder, elementIndex);
                        sqlParameters.AddRange(itemParameters);
                        commandBuilder.AppendLine(commandForOneItem);
                    }
                    resultCommand = commandBuilder.ToString();
                    if (_logger.IsEnabled(LogLevel.Debug))
                        _logger.LogDebug($"result command: {resultCommand}");
                }
            }
            if (allElementsAreNull)
                throw new ArgumentException($"There is no elements in the collection. At least one element must be.", nameof(elements));
            result.Add(new SqlCommandBuilderResult(
                           resultCommand,
                           sqlParameters,
                           isThereReturningClause: false,
                           elementsCount: elementIndex + 1
                       ));
            return result;
        }

        /// <summary>
        /// Generates update sql command for one item 
        /// </summary>
        /// <param name="entityProfile">elements type profile (contains mapping and other options)</param>
        /// <param name="item">an instance that has to be updated to the database</param>
        /// <param name="externalBuilder">Builder to which the generated for an item command will be appended</param>
        /// <param name="elementIndex">As this method is called for each item, this value will be added to the sql parameter name</param>
        /// <returns> Returns named tuple with generated command and list of db parameters. </returns>
        private (string Command, ICollection<NpgsqlParameter> Parameters)
            GenerateForItem<TEntity>(EntityProfile entityProfile,
                                     TEntity       item,
                                     StringBuilder externalBuilder,
                                     int           elementIndex)
            where TEntity : class
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            if (entityProfile == null)
                throw new ArgumentNullException(nameof(entityProfile));

            var commandBuilder = externalBuilder ?? new StringBuilder(192);
            commandBuilder.Append($"delete from {entityProfile.TableName} ");

            commandBuilder.Append(" where ");
            var parameters           = new List<NpgsqlParameter>();
            var firstWhereExpression = true;

            foreach (var propInfo in entityProfile.Properties.Values) {
                var paramName = $"@param_{propInfo.DbColumnName}_{elementIndex}";
                try {
                    if (!propInfo.IsPrivateKey)
                        continue;
                    var whereDelimiter = firstWhereExpression ? "" : " and ";
                    if (propInfo.IsDynamicallyInvoked()) {
                        commandBuilder.Append($"{whereDelimiter}\"{propInfo.DbColumnName}\"={paramName}");
                        parameters.Add(new NpgsqlParameter(paramName, propInfo.DbColumnType)
                                       {
                                           Value      = propInfo.GetPropertyValue(item) ?? DBNull.Value,
                                           IsNullable = propInfo.IsNullable
                                       });
                    }
                    else {
                        var propValue = propInfo.GetPropertyValueAsString(item);
                        commandBuilder.Append($"{whereDelimiter}\"{propInfo.DbColumnName}\"={propValue}");
                    }
                    firstWhereExpression = false;
                }
                catch (Exception ex) {
                    throw new SqlGenerationException(SqlOperation.Delete,
                                                     $"an error occurred while calculating {paramName}",
                                                     ex);
                }
            }
            if (firstWhereExpression)
                throw new SqlGenerationException(SqlOperation.Delete,
                                                 $"There is no private key defined for the entity type: '{typeof(TEntity).FullName}'");
            commandBuilder.Append(";");
            var command = externalBuilder == null
                ? commandBuilder.ToString()
                : ""; // In order to allow append next elements externalBuilder must not be flashed to string.
            return (command, parameters);
        }
    }
}
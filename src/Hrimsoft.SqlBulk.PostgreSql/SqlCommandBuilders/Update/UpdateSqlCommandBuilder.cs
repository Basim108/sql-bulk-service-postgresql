using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Hrimsoft.SqlBulk.PostgreSql
{
    /// <summary>
    /// Generates bulk update sql command 
    /// </summary>
    public class UpdateSqlCommandBuilder : IUpdateSqlCommandBuilder
    {
        private readonly ILogger<UpdateSqlCommandBuilder> _logger;

        /// <summary> </summary>
        public UpdateSqlCommandBuilder(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<UpdateSqlCommandBuilder>();
        }

        /// <summary>
        /// Generates bulk update sql command
        /// </summary>
        /// <param name="elements">elements that have to be updated</param>
        /// <param name="entityProfile">elements type profile (contains mapping and other options)</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns a text of an sql update command and collection of database parameters</returns>
        public IList<SqlCommandBuilderResult> Generate<TEntity>(ICollection<TEntity> elements,
                                                                EntityProfile        entityProfile,
                                                                CancellationToken    cancellationToken)
            where TEntity : class
        {
            if (elements == null)
                throw new ArgumentNullException(nameof(elements));
            if (entityProfile == null)
                throw new ArgumentNullException(nameof(entityProfile));

            if (elements.Count == 0)
                throw new ArgumentException($"There is no elements in the collection. At least one element must be.", nameof(elements));

            _logger.LogTrace($"Generating update sql for {elements.Count} elements.");

            var resultCommand = "";

            if (_logger.IsEnabled(LogLevel.Debug)) {
                _logger.LogDebug($"{nameof(TEntity)}: {typeof(TEntity).FullName}");
                _logger.LogDebug($"{nameof(elements)}.Count: {elements.Count}");
            }

            cancellationToken.ThrowIfCancellationRequested();
            const int MAX_PARAMS_PER_CMD = 65_535;

            var paramsCount   = elements.Count * entityProfile.MaxPossibleSqlParameters;
            var sqlParameters = new List<NpgsqlParameter>(Math.Min(paramsCount, MAX_PARAMS_PER_CMD));
            var result        = new List<SqlCommandBuilderResult>();

            var isThereReturningClause = false;
            var allElementsAreNull     = true;
            var elementIndex           = 0;
            var elementAbsIndex        = 0;
            using (var elementsEnumerator = elements.GetEnumerator()) {
                var thereIsMoreElements = true;

                // I'd like to build the first update command, so I can estimate an approximate size of all commands
                // ignore all null items until find the first not null item
                while (elementsEnumerator.Current == null && thereIsMoreElements) {
                    thereIsMoreElements = elementsEnumerator.MoveNext();
                    elementAbsIndex++;
                }
                if (thereIsMoreElements) {
                    allElementsAreNull = false;

                    var (commandForOneItem, itemParameters, hasReturningClause)
                        = GenerateForItem(entityProfile, elementsEnumerator.Current, null, 0, elementAbsIndex);
                    isThereReturningClause = hasReturningClause;
                    sqlParameters.AddRange(itemParameters);

                    var maxElementsInCmd = (int) Math.Ceiling(MAX_PARAMS_PER_CMD / (double) itemParameters.Count);
                    var entireCommandLength = elements.Count * itemParameters.Count <= MAX_PARAMS_PER_CMD
                        ? commandForOneItem.Length * elements.Count
                        : commandForOneItem.Length * maxElementsInCmd;

                    if (_logger.IsEnabled(LogLevel.Debug))
                        _logger.LogDebug($"entire command length: {entireCommandLength}");

                    var commandBuilder = new StringBuilder(entireCommandLength);
                    commandBuilder.AppendLine(commandForOneItem);
                    while (elementsEnumerator.MoveNext()) {
                        elementAbsIndex++;
                        // ignore all null items 
                        if (elementsEnumerator.Current == null)
                            continue;
                        elementIndex++;
                        if (sqlParameters.Count + entityProfile.MaxPossibleSqlParameters > MAX_PARAMS_PER_CMD) {
                            result.Add(new SqlCommandBuilderResult(
                                           commandBuilder.ToString(),
                                           sqlParameters,
                                           isThereReturningClause,
                                           elementsCount: elementIndex
                                       ));
                            if (_logger.IsEnabled(LogLevel.Information)) {
                                var (cmdSize, suffix) = ((long) commandBuilder.Length * 2).PrettifySize();
                                _logger.LogInformation($"Generated sql update command for {elementIndex + 1} {entityProfile.EntityType.Name} elements, command size {cmdSize:F2} {suffix}");
                            }
                            commandBuilder.Clear();
                            sqlParameters = new List<NpgsqlParameter>(sqlParameters.Count);
                            elementIndex  = 0;
                        }
                        (commandForOneItem, itemParameters, _)
                            = GenerateForItem(entityProfile, elementsEnumerator.Current, commandBuilder, elementIndex, elementAbsIndex);
                        sqlParameters.AddRange(itemParameters);
                        commandBuilder.AppendLine(commandForOneItem);
                    }
                    resultCommand = commandBuilder.ToString();
                    if (_logger.IsEnabled(LogLevel.Debug))
                        _logger.LogDebug($"result command: {resultCommand}");
                }
            }
            if (allElementsAreNull)
                throw new ArgumentException("There is no elements in the collection. At least one element must be.", nameof(elements));

            result.Add(new SqlCommandBuilderResult(
                           command: resultCommand,
                           sqlParameters: sqlParameters,
                           isThereReturningClause: isThereReturningClause,
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
        /// <param name="elementAbsIndex"></param>
        /// <returns> Returns named tuple with generated command and list of db parameters. </returns>
        private (string Command, ICollection<NpgsqlParameter> Parameters, bool hasReturningClause)
            GenerateForItem<TEntity>(EntityProfile entityProfile,
                                     TEntity       item,
                                     StringBuilder externalBuilder,
                                     int           elementIndex,
                                     int           elementAbsIndex)
            where TEntity : class
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            if (entityProfile == null)
                throw new ArgumentNullException(nameof(entityProfile));

            var commandBuilder = externalBuilder ?? new StringBuilder(192);
            commandBuilder.Append($"update {entityProfile.TableName} set ");

            var whereClause          = " where ";
            var returningClause      = " returning ";
            var parameters           = new List<NpgsqlParameter>();
            var firstSetExpression   = true;
            var firstWhereExpression = true;
            var firstReturningColumn = true;

            foreach (var pair in entityProfile.Properties) {
                try {
                    var propInfo  = pair.Value;
                    var paramName = $"@param_{propInfo.DbColumnName}_{elementIndex}";
                    if (propInfo.IsPrivateKey) {
                        var whereDelimiter = firstWhereExpression ? "" : ",";
                        if (propInfo.IsDynamicallyInvoked()) {
                            whereClause += $"{whereDelimiter}\"{propInfo.DbColumnName}\"={paramName}";
                            parameters.Add(new NpgsqlParameter(paramName, propInfo.DbColumnType)
                                           {
                                               Value      = propInfo.GetPropertyValue(item) ?? DBNull.Value,
                                               IsNullable = propInfo.IsNullable
                                           });
                        }
                        else {
                            var keyValue = propInfo.GetPropertyValueAsString(item);
                            whereClause += $"{whereDelimiter}\"{propInfo.DbColumnName}\"={keyValue}";
                        }
                        firstWhereExpression = false;
                    }
                    if (propInfo.IsUpdatedAfterUpdate) {
                        var returningDelimiter = firstReturningColumn ? "" : ", ";
                        returningClause      += $"{returningDelimiter}\"{propInfo.DbColumnName}\"";
                        firstReturningColumn =  false;
                    }
                    if (propInfo.IsAutoGenerated)
                        continue;
                    var setClauseDelimiter = firstSetExpression ? "" : ",";

                    if (propInfo.IsDynamicallyInvoked()) {
                        commandBuilder.Append($"{setClauseDelimiter}\"{propInfo.DbColumnName}\"=");
                        var value = propInfo.GetPropertyValue(item);
                        if (value == null) {
                            commandBuilder.Append("null");
                        }
                        else {
                            commandBuilder.Append(paramName);
                            parameters.Add(new NpgsqlParameter(paramName, propInfo.DbColumnType)
                                           {
                                               Value = value
                                           });
                        }
                    }
                    else {
                        var propValue = propInfo.GetPropertyValueAsString(item);
                        commandBuilder.Append($"{setClauseDelimiter}\"{propInfo.DbColumnName}\"={propValue}");
                    }
                    firstSetExpression = false;
                }
                catch (Exception ex) {
                    var message = $"an error occurred while processing a property {pair.Key} of {entityProfile.EntityType.Name} entity, item idx: {elementAbsIndex}";
                    throw new SqlGenerationException(SqlOperation.Update, message, ex);
                }
            }
            if (firstWhereExpression)
                throw new SqlGenerationException(SqlOperation.Update, $"There is no private key defined for the entity type: '{typeof(TEntity).FullName}'");

            commandBuilder.Append(whereClause);

            if (!firstReturningColumn)
                commandBuilder.Append(returningClause);
            commandBuilder.Append(";");

            var command = externalBuilder == null
                ? commandBuilder.ToString()
                : ""; // In order to allow append next elements externalBuilder must not be flashed to string.

            if (_logger.IsEnabled(LogLevel.Debug)) {
                _logger.LogDebug($"command: {command}");
                _logger.LogDebug($"returningClause: {returningClause}");
            }

            return (command, parameters, !firstReturningColumn);
        }
    }
}
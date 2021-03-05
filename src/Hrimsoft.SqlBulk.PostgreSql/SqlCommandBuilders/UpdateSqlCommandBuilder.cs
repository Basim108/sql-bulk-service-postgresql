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

            _logger.LogTrace($"Generating update sql for {elements.Count} elements.");

            var resultCommand = "";

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug($"{nameof(TEntity)}: {typeof(TEntity).FullName}");
                _logger.LogDebug($"{nameof(elements)}.Count: {elements.Count}");
            }

            cancellationToken.ThrowIfCancellationRequested();
            const int MAX_PARAMS_PER_CMD = 65_535;

            var paramsCount       = elements.Count * entityProfile.Properties.Count;
            var commandParameters = new List<NpgsqlParameter>(Math.Min(paramsCount, MAX_PARAMS_PER_CMD));
            var result            = new List<SqlCommandBuilderResult>();

            var isThereReturningClause = false;
            var allElementsAreNull     = true;
            using (var elementsEnumerator = elements.GetEnumerator())
            {
                var thereIsMoreElements = true;
                // I'd like to build the first update command, so I can estimate an approximate size of all commands
                // ignore all null items until find the first not null item
                while (elementsEnumerator.Current == null && thereIsMoreElements)
                {
                    thereIsMoreElements = elementsEnumerator.MoveNext();
                }
                if (thereIsMoreElements)
                {
                    allElementsAreNull = false;

                    var (commandForOneItem, itemParameters, hasReturningClause)
                        = GenerateForItem(entityProfile, elementsEnumerator.Current, null, 0);
                    isThereReturningClause = hasReturningClause;
                    commandParameters.AddRange(itemParameters);

                    var maxElementsInCmd = (int) Math.Ceiling(MAX_PARAMS_PER_CMD / (double) itemParameters.Count);
                    var entireCommandLength = elements.Count * itemParameters.Count <= MAX_PARAMS_PER_CMD
                        ? commandForOneItem.Length * elements.Count
                        : commandForOneItem.Length * maxElementsInCmd;

                    if (_logger.IsEnabled(LogLevel.Debug))
                        _logger.LogDebug($"entire command length: {entireCommandLength}");

                    var resultBuilder = new StringBuilder(entireCommandLength);
                    resultBuilder.AppendLine(commandForOneItem);
                    var paramsPerElement = itemParameters.Count;
                    var elementIndex     = 0;
                    while (elementsEnumerator.MoveNext())
                    {
                        // ignore all null items 
                        if (elementsEnumerator.Current == null)
                            continue;
                        if (commandParameters.Count + paramsPerElement > MAX_PARAMS_PER_CMD)
                        {
                            result.Add(new SqlCommandBuilderResult
                                       {
                                           Command                = resultBuilder.ToString(),
                                           Parameters             = commandParameters,
                                           IsThereReturningClause = isThereReturningClause
                                       });
                            resultBuilder.Clear();
                            var remainingParams = (elements.Count - elementIndex - 1) * paramsPerElement;
                            commandParameters = new List<NpgsqlParameter>(Math.Min(remainingParams, MAX_PARAMS_PER_CMD));
                        }
                        elementIndex++;
                        (commandForOneItem, itemParameters, _)
                            = GenerateForItem(entityProfile, elementsEnumerator.Current, resultBuilder, elementIndex);
                        commandParameters.AddRange(itemParameters);
                        resultBuilder.AppendLine(commandForOneItem);
                    }
                    resultCommand = resultBuilder.ToString();
                    if (_logger.IsEnabled(LogLevel.Debug))
                        _logger.LogDebug($"result command: {resultCommand}");
                }
            }
            if (allElementsAreNull)
                throw new ArgumentException("There is no elements in the collection. At least one element must be.", nameof(elements));

            result.Add(new SqlCommandBuilderResult
                       {
                           Command                = resultCommand,
                           Parameters             = commandParameters,
                           IsThereReturningClause = isThereReturningClause
                       });
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
        public (string Command, ICollection<NpgsqlParameter> Parameters, bool hasReturningClause) GenerateForItem<TEntity>(
            EntityProfile entityProfile, TEntity item, StringBuilder externalBuilder, int elementIndex)
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

            foreach (var propInfo in entityProfile.Properties.Values)
            {
                var paramName = $"@param_{propInfo.DbColumnName}_{elementIndex}";
                try
                {
                    if (propInfo.IsPrivateKey)
                    {
                        var whereDelimiter = firstWhereExpression ? "" : ",";
                        var keyValue      = propInfo.GetPropertyValueAsString(item);
                        if (keyValue == null)
                        {
                            whereClause += $"{whereDelimiter}\"{propInfo.DbColumnName}\"={paramName}";
                            parameters.Add(new NpgsqlParameter(paramName, propInfo.DbColumnType)
                                           {
                                               Value      = propInfo.GetPropertyValue(item) ?? DBNull.Value,
                                               IsNullable = propInfo.IsNullable
                                           });
                        }
                        else
                        {
                            whereClause += $"{whereDelimiter}\"{propInfo.DbColumnName}\"={keyValue}";
                        }
                        firstWhereExpression = false;
                    }
                    if (propInfo.IsUpdatedAfterUpdate)
                    {
                        var returningDelimiter = firstReturningColumn ? "" : ", ";
                        returningClause      += $"{returningDelimiter}\"{propInfo.DbColumnName}\"";
                        firstReturningColumn =  false;
                    }
                    if (propInfo.IsAutoGenerated)
                        continue;
                    var setClauseDelimiter = firstSetExpression ? "" : ",";
                    var propValue          = propInfo.GetPropertyValueAsString(item);
                    if (propValue == null)
                    {
                        commandBuilder.Append($"{setClauseDelimiter}\"{propInfo.DbColumnName}\"={paramName}");
                        parameters.Add(new NpgsqlParameter(paramName, propInfo.DbColumnType)
                                       {
                                           Value      = propInfo.GetPropertyValue(item) ?? DBNull.Value,
                                           IsNullable = propInfo.IsNullable
                                       });
                    }
                    else
                    {
                        commandBuilder.Append($"{setClauseDelimiter}\"{propInfo.DbColumnName}\"={propValue}");
                    }
                    firstSetExpression = false;
                }
                catch (Exception ex)
                {
                    var message = $"an error occurred while calculating {paramName}";
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

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug($"command: {command}");
                _logger.LogDebug($"returningClause: {returningClause}");
            }

            return (command, parameters, !firstReturningColumn);
        }
    }
}
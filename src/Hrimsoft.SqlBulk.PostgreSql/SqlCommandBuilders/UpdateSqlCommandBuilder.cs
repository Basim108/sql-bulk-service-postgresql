using System;
using System.Collections.Generic;
using System.Text;
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

        /// <inheritdoc />
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
        public SqlCommandBuilderResult Generate<TEntity>(ICollection<TEntity> elements, EntityProfile entityProfile,
            CancellationToken cancellationToken)
            where TEntity : class
        {
            if (elements == null)
                throw new ArgumentNullException(nameof(elements));
            if (entityProfile == null)
                throw new ArgumentNullException(nameof(entityProfile));

            if (elements.Count == 0)
                throw new ArgumentException($"There is no elements in the collection. At least one element must be.", nameof(elements));

            var result = "";
            var allItemsParameters = new List<NpgsqlParameter>();

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug($"{nameof(TEntity)}: {typeof(TEntity).FullName}");
                _logger.LogDebug($"{nameof(elements)}.Count: {elements.Count}");
            }

            cancellationToken.ThrowIfCancellationRequested();
            var isThereReturningClause = false;
            var allElementsAreNull = true;
            using (var elementsEnumerator = elements.GetEnumerator())
            {
                var thereIsMoreElements = true;
                // ignore all null items until find the first not null item
                while (elementsEnumerator.Current == null && thereIsMoreElements)
                {
                    thereIsMoreElements = elementsEnumerator.MoveNext();
                }

                if (thereIsMoreElements)
                {
                    allElementsAreNull = false;

                    var (commandForOneItem, itemParameters, hasReturningClause) = GenerateForItem(entityProfile, elementsEnumerator.Current, null, 0);
                    isThereReturningClause = hasReturningClause;
                    allItemsParameters.AddRange(itemParameters);

                    var approximateEntireCommandLength = commandForOneItem.Length * elements.Count;

                    if (_logger.IsEnabled(LogLevel.Debug))
                        _logger.LogDebug($"approximateEntireCommandLength: {approximateEntireCommandLength}");

                    var resultBuilder = new StringBuilder(approximateEntireCommandLength);
                    resultBuilder.AppendLine(commandForOneItem);

                    while (elementsEnumerator.MoveNext())
                    {
                        // ignore all null items 
                        if (elementsEnumerator.Current == null)
                            continue;

                        (commandForOneItem, itemParameters, hasReturningClause) = GenerateForItem(
                            entityProfile,
                            elementsEnumerator.Current,
                            resultBuilder,
                            allItemsParameters.Count);

                        allItemsParameters.AddRange(itemParameters);
                        resultBuilder.AppendLine(commandForOneItem);
                    }

                    result = resultBuilder.ToString();
                    if (_logger.IsEnabled(LogLevel.Debug))
                        _logger.LogDebug($"result command: {result}");
                }
            }

            if (allElementsAreNull)
                throw new ArgumentException($"There is no elements in the collection. At least one element must be.", nameof(elements));

            return new SqlCommandBuilderResult
            {
                Command = result,
                Parameters = allItemsParameters,
                IsThereReturningClause = isThereReturningClause
            };
        }

        /// <summary>
        /// Generates update sql command for one item 
        /// </summary>
        /// <param name="entityProfile">elements type profile (contains mapping and other options)</param>
        /// <param name="item">an instance that has to be updated to the database</param>
        /// <param name="externalBuilder">Builder to which the generated for an item command will be appended</param>
        /// <param name="lastUsedParamIndex">As this method is called for each item, this argument indecates the last used parameter index</param>
        /// <returns> Returns named tuple with generated command and list of db parameters. </returns>
        public (string Command, ICollection<NpgsqlParameter> Parameters, bool hasReturningClause) GenerateForItem<TEntity>(
            EntityProfile entityProfile,
            TEntity item,
            StringBuilder externalBuilder,
            int lastUsedParamIndex)
            where TEntity : class
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            if (entityProfile == null)
                throw new ArgumentNullException(nameof(entityProfile));

            var commandBuilder = externalBuilder ?? new StringBuilder(192);
            commandBuilder.Append($"update {entityProfile.TableName} set ");

            var whereClause = " where ";
            var returningClause = " returning ";
            var parameters = new List<NpgsqlParameter>();
            var firstSetExpression = true;
            var firstWhereExpression = true;
            var firstReturningColumn = true;

            foreach (var propInfo in entityProfile.Properties.Values)
            {
                if (propInfo.IsPrivateKey)
                {
                    var whereDelimiter = firstWhereExpression
                        ? ""
                        : ",";
                    var idParamName = "@param" + (parameters.Count + lastUsedParamIndex);
                    whereClause += $"{whereDelimiter}\"{propInfo.DbColumnName}\"={idParamName}";

                    parameters.Add(new NpgsqlParameter(idParamName, propInfo.DbColumnType)
                    {
                        Value = propInfo.GetPropertyValue(item)
                    });

                    firstWhereExpression = false;
                }

                if (propInfo.IsUpdatedAfterUpdate)
                {
                    var returningDelimiter = firstReturningColumn
                        ? ""
                        : ", ";

                    returningClause += $"{returningDelimiter}\"{propInfo.DbColumnName}\"";
                    firstReturningColumn = false;
                }

                if (propInfo.IsAutoGenerated)
                    continue;

                var setExpressionDelimiter = firstSetExpression
                    ? ""
                    : ",";

                var setParamName = $"@param{parameters.Count + lastUsedParamIndex}";
                commandBuilder.Append($"{setExpressionDelimiter}\"{propInfo.DbColumnName}\"={setParamName}");

                parameters.Add(new NpgsqlParameter(setParamName, propInfo.DbColumnType)
                {
                    Value = propInfo.GetPropertyValue(item)
                });

                firstSetExpression = false;
            }

            if (firstWhereExpression)
                throw new SqlGenerationException($"There is no private key defined for the entity type: '{typeof(TEntity).FullName}'");

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
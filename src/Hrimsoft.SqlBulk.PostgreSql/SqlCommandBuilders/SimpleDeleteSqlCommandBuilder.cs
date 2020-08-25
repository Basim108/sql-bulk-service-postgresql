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
    /// Generates bulk delete sql command 
    /// </summary>
    public class SimpleDeleteSqlCommandBuilder : IDeleteSqlCommandBuilder
    {
        private readonly ILogger<SimpleDeleteSqlCommandBuilder> _logger;

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

            _logger.LogTrace($"Generating delete sql for {elements.Count} elements.");

            var result             = "";
            var allItemsParameters = new List<NpgsqlParameter>();

            if (_logger.IsEnabled(LogLevel.Debug)) {
                _logger.LogDebug($"{nameof(TEntity)}: {typeof(TEntity).FullName}");
                _logger.LogDebug($"{nameof(elements)}.Count: {elements.Count}");
            }

            cancellationToken.ThrowIfCancellationRequested();
            var allElementsAreNull = true;
            using (var elementsEnumerator = elements.GetEnumerator()) {
                var thereIsMoreElements = true;
                var elementIndex        = -1;
                // ignore all null items until find the first not null item
                while (elementsEnumerator.Current == null && thereIsMoreElements) {
                    elementIndex++;
                    thereIsMoreElements = elementsEnumerator.MoveNext();
                }
                if (thereIsMoreElements) {
                    allElementsAreNull = false;
                    var (commandForOneItem, itemParameters)
                        = GenerateForItem(entityProfile, elementsEnumerator.Current, null, elementIndex);
                    allItemsParameters.AddRange(itemParameters);

                    var entireCommandLength = commandForOneItem.Length * elements.Count;

                    if (_logger.IsEnabled(LogLevel.Debug))
                        _logger.LogDebug($"approximate entire command length: {entireCommandLength}");

                    var resultBuilder = new StringBuilder(entireCommandLength);
                    resultBuilder.AppendLine(commandForOneItem);

                    while (elementsEnumerator.MoveNext()) {
                        elementIndex++;
                        // ignore all null items 
                        if (elementsEnumerator.Current == null)
                            continue;
                        (commandForOneItem, itemParameters)
                            = GenerateForItem(entityProfile, elementsEnumerator.Current, resultBuilder, elementIndex);

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
                Command                = result,
                Parameters             = allItemsParameters,
                IsThereReturningClause = false
            };
        }

        /// <summary>
        /// Generates update sql command for one item 
        /// </summary>
        /// <param name="entityProfile">elements type profile (contains mapping and other options)</param>
        /// <param name="item">an instance that has to be updated to the database</param>
        /// <param name="externalBuilder">Builder to which the generated for an item command will be appended</param>
        /// <param name="elementIndex">As this method is called for each item, this value will be added to the sql parameter name</param>
        /// <returns> Returns named tuple with generated command and list of db parameters. </returns>
        public (string Command, ICollection<NpgsqlParameter> Parameters) GenerateForItem<TEntity>(EntityProfile entityProfile,
            TEntity item, StringBuilder externalBuilder, int elementIndex)
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
                    var whereDelimiter = firstWhereExpression ? "" : ",";
                    commandBuilder.Append($"{whereDelimiter}\"{propInfo.DbColumnName}\"={paramName}");
                    parameters.Add(new NpgsqlParameter(paramName, propInfo.DbColumnType)
                    {
                        Value      = propInfo.GetPropertyValue(item) ?? DBNull.Value,
                        IsNullable = propInfo.IsNullable
                    });
                    firstWhereExpression = false;
                }
                catch (Exception ex) {
                    var message = $"an error occurred while calculating {paramName}";
                    throw new SqlGenerationException(SqlOperation.Delete, message, ex);
                }
            }

            if (firstWhereExpression)
                throw new SqlGenerationException(SqlOperation.Delete, $"There is no private key defined for the entity type: '{typeof(TEntity).FullName}'");

            commandBuilder.Append(";");

            var command = externalBuilder == null
                ? commandBuilder.ToString()
                : ""; // In order to allow append next elements externalBuilder must not be flashed to string.

            if (_logger.IsEnabled(LogLevel.Debug)) {
                _logger.LogDebug($"command: {command}");
            }

            return (command, parameters);
        }
    }
}
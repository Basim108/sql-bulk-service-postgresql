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
        public (string Command, ICollection<NpgsqlParameter> Parameters) Generate<TEntity>([NotNull] ICollection<TEntity> elements, [NotNull] EntityProfile entityProfile,
            CancellationToken cancellationToken)
            where TEntity : class
        {
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

            using (var elementsEnumerator = elements.GetEnumerator())
            {
                // ignore all null items until find the first not null item
                while (elementsEnumerator.Current == null)
                    elementsEnumerator.MoveNext();

                var (commandForOneItem, itemParameters) = GenerateForItem(entityProfile, elementsEnumerator.Current, null);
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

                    (commandForOneItem, itemParameters) = GenerateForItem(entityProfile, elementsEnumerator.Current, null);

                    allItemsParameters.AddRange(itemParameters);
                    resultBuilder.AppendLine(commandForOneItem);
                }

                result = resultBuilder.ToString();
                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.LogDebug($"result command: {result}");
            }

            return (Command: result, Parameters: allItemsParameters);
        }

        /// <summary>
        /// Generates update sql command for one item 
        /// </summary>
        /// <param name="entityProfile">elements type profile (contains mapping and other options)</param>
        /// <param name="item">an instance that has to be updated to the database</param>
        /// <param name="externalBuilder">Builder to which the generated for an item command will be appended</param>
        /// <returns> Returns named tuple with generated command and list of db parameters. </returns>
        public (string Command, ICollection<NpgsqlParameter> Parameters) GenerateForItem<TEntity>(
            [NotNull] EntityProfile entityProfile, 
            [NotNull] TEntity item,
            StringBuilder externalBuilder)
            where TEntity : class
        {
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
                    var idParamName = "@param" + parameters.Count;
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

                var setParamName = "@param" + parameters.Count;
                commandBuilder.Append($"{setExpressionDelimiter}\"{propInfo.DbColumnName}\"={setParamName}");

                parameters.Add(new NpgsqlParameter(setParamName, propInfo.DbColumnType)
                {
                    Value = propInfo.GetPropertyValue(item)
                });

                firstSetExpression = false;
            }

            commandBuilder.Append(whereClause);
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

            return (command, parameters);
        }
    }
}
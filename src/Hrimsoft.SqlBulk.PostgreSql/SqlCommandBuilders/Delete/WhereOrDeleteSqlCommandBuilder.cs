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
    /// Generates bulk delete sql command with where clause using or operand
    /// </summary>
    public class WhereOrDeleteSqlCommandBuilder : IDeleteSqlCommandBuilder
    {
        private readonly ILogger<WhereOrDeleteSqlCommandBuilder> _logger;

        private const int MAX_PARAMS_PER_CMD = 65_535;
        /// <summary> </summary>
        public WhereOrDeleteSqlCommandBuilder(ILogger<WhereOrDeleteSqlCommandBuilder> logger)
        {
            _logger = logger;
        }

        /// <summary> Generates bulk delete sql command with where clause using in operand
        /// </summary>
        /// <param name="elements">elements that have to be deleted</param>
        /// <param name="entityProfile">elements type profile (contains mapping and other options)</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns a text of an sql delete command and collection of database parameters</returns>
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
                throw new ArgumentException("There is no elements in the collection. At least one element must be.", nameof(elements));

            var privateKeys = entityProfile.Properties
                                           .Values
                                           .Where(x => x.IsPrivateKey)
                                           .ToList();
            if (privateKeys.Count == 0)
                throw new ArgumentException($"Entity {entityProfile.EntityType.FullName} must have at least one private key.",
                                            nameof(entityProfile));
            _logger.LogTrace($"Generating delete sql for {elements.Count} elements.");
            if (_logger.IsEnabled(LogLevel.Debug)) {
                _logger.LogDebug($"{nameof(TEntity)}: {typeof(TEntity).FullName}");
                _logger.LogDebug($"{nameof(elements)}.Count: {elements.Count}");
            }
            cancellationToken.ThrowIfCancellationRequested();

            var commandHeader = $"delete from {entityProfile.TableName} where ";

            var result            = new List<SqlCommandBuilderResult>();
            var cmdBuilder        = new StringBuilder();
            var paramsPerItem     = privateKeys.Count(x => x.IsDynamicallyInvoked());
            var sqlParamsCapacity = paramsPerItem * elements.Count;
            var sqlParameters = sqlParamsCapacity > 0
                ? new List<NpgsqlParameter>(Math.Min(sqlParamsCapacity, MAX_PARAMS_PER_CMD))
                : null;
            cmdBuilder.Append(commandHeader);

            var elementAbsIndex = -1;
            var elementIndex    = -1;
            using (var elementsEnumerator = elements.GetEnumerator()) {
                while (elementsEnumerator.MoveNext()) {
                    elementAbsIndex++;
                    if (elementsEnumerator.Current == null)
                        continue;
                    try {
                        elementIndex++;
                        if (sqlParameters != null && sqlParameters.Count + paramsPerItem > MAX_PARAMS_PER_CMD) {
                            cmdBuilder.AppendLine(");");
                            result.Add(new SqlCommandBuilderResult
                                           (
                                            cmdBuilder.ToString(),
                                            sqlParameters,
                                            isThereReturningClause: false,
                                            elementsCount: elementIndex
                                           ));
                            if (_logger.IsEnabled(LogLevel.Debug)) {
                                var (cmdSize, suffix) = ((long) cmdBuilder.Length * 2).PrettifySize();
                                _logger.LogDebug($"Generated sql where-in-delete command for {elementIndex + 1} {entityProfile.EntityType.Name} elements, command size {cmdSize:F2} {suffix}");
                            }
                            sqlParameters = new List<NpgsqlParameter>(sqlParameters.Count);
                            cmdBuilder.Clear();
                            cmdBuilder.Append(commandHeader);
                            elementIndex   = 0;
                        }
                        if (elementIndex > 0)
                            cmdBuilder.Append(")or");
                        for (var i = 0; i < privateKeys.Count; i++) {
                            var pkDelimiter = i == 0 ? "(" : " and ";
                            var pk          = privateKeys[i];
                            if (pk.IsDynamicallyInvoked()) {
                                var paramName = $"@param_{pk.DbColumnName}_{elementIndex}";
                                cmdBuilder.Append($"{pkDelimiter}\"{pk.DbColumnName}\"={paramName}");
                                var value = pk.GetPropertyValue(elementsEnumerator.Current);
                                if (value == null)
                                    throw new ArgumentException($"Private key must not be null. property: {pk.DbColumnName}, item index: {elementAbsIndex}",
                                                                nameof(elements));
                                // ReSharper disable once PossibleNullReferenceException
                                sqlParameters.Add(new NpgsqlParameter(paramName, pk.DbColumnType)
                                                  {
                                                      Value = value
                                                  });
                            }
                            else {
                                var propValue = pk.GetPropertyValueAsString(elementsEnumerator.Current);
                                cmdBuilder.Append($"{pkDelimiter}\"{pk.DbColumnName}\"={propValue}");
                            }
                        }
                    }
                    catch (Exception ex) {
                        var message = $"an error occurred while calculating {privateKeys[0].DbColumnName} of item at index {elementAbsIndex}";
                        throw new SqlGenerationException(SqlOperation.Delete, message, ex);
                    }
                }
            }
            if (elementIndex == -1 && result.Count == 0)
                throw new ArgumentException($"There is no elements in the collection. At least one element must be.", nameof(elements));

            cmdBuilder.AppendLine(");");
            result.Add(new SqlCommandBuilderResult
                           (
                            cmdBuilder.ToString(),
                            sqlParameters ?? new List<NpgsqlParameter>(),
                            isThereReturningClause: false,
                            elementsCount: elementIndex + 1
                           ));
            return result;
        }
    }
}
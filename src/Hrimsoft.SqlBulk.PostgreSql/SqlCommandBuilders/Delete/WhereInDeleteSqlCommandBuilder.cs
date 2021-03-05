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
    /// Generates bulk delete sql command with where clause using in operand
    /// </summary>
    public class WhereInDeleteSqlCommandBuilder : IDeleteSqlCommandBuilder
    {
        private readonly ILogger<WhereInDeleteSqlCommandBuilder> _logger;

        private const int MAX_IN_CLAUSE_IDS  = 1000;
        private const int MAX_PARAMS_PER_CMD = 65_535;

        /// <summary> </summary>
        public WhereInDeleteSqlCommandBuilder(ILogger<WhereInDeleteSqlCommandBuilder> logger)
        {
            _logger = logger;
        }

        /// <summary> Generates bulk delete sql command with where clause using in operand
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

            var privateKeys = entityProfile.Properties
                                           .Values
                                           .Where(x => x.IsPrivateKey)
                                           .ToList();
            if (privateKeys.Count == 0)
                throw new ArgumentException($"Entity {entityProfile.EntityType.FullName} must have at least one private key.",
                                            nameof(entityProfile));
            if (privateKeys.Count > 1)
                throw new ArgumentException(
                    $"Cannot generate delete sql command with collapsed where clause as there are more than one private keys in the entity {entityProfile.EntityType.FullName}",
                    nameof(entityProfile));

            _logger.LogTrace($"Generating delete sql for {elements.Count} elements.");

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug($"{nameof(TEntity)}: {typeof(TEntity).FullName}");
                _logger.LogDebug($"{nameof(elements)}.Count: {elements.Count}");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var commandHeader = $"delete from {entityProfile.TableName} where \"{privateKeys[0].DbColumnName}\" in (";

            var result     = new List<SqlCommandBuilderResult>();
            var cmdBuilder = new StringBuilder();
            var parameters = privateKeys[0].IsDynamicallyInvoked()
                ? new List<NpgsqlParameter>(Math.Min(elements.Count, MAX_PARAMS_PER_CMD))
                : null;
            cmdBuilder.Append(commandHeader);

            var elementAbsIndex = -1;
            var elementIndex    = -1;
            using (var elementsEnumerator = elements.GetEnumerator())
            {
                while (elementsEnumerator.MoveNext())
                {
                    elementAbsIndex++;
                    if (elementsEnumerator.Current == null)
                        continue;
                    try
                    {
                        elementIndex++;
                        var whereDelimiter = elementIndex == 0 ? "" : ",";
                        if (parameters != null && parameters.Count + 1 > MAX_PARAMS_PER_CMD)
                        {
                            cmdBuilder.AppendLine(");");
                            result.Add(new SqlCommandBuilderResult
                                       {
                                           Command                = cmdBuilder.ToString(),
                                           Parameters             = parameters,
                                           IsThereReturningClause = false
                                       });
                            cmdBuilder.Clear();
                            cmdBuilder.Append(commandHeader);
                            parameters.Clear();
                            whereDelimiter = "";
                        }
                        else if (elementIndex == MAX_IN_CLAUSE_IDS + 1)
                        {
                            cmdBuilder.AppendLine(");");
                            cmdBuilder.Append(commandHeader);
                            whereDelimiter = "";
                        }
                        var propValue = privateKeys[0].GetPropertyValueAsString(elementsEnumerator.Current);
                        if (parameters != null)
                        {
                            var paramName = $"@param_{privateKeys[0].DbColumnName}_{elementIndex}";
                            cmdBuilder.Append($"{whereDelimiter}{paramName}");
                            var value = privateKeys[0].GetPropertyValue(elementsEnumerator.Current);
                            if (value == null)
                                throw new ArgumentException($"Private key must not be null. property: {privateKeys[0].DbColumnName}, item index: {elementAbsIndex}",
                                                            nameof(elements));
                            parameters.Add(new NpgsqlParameter(paramName, privateKeys[0].DbColumnType)
                                           {
                                               Value = value
                                           });
                        }
                        else
                        {
                            cmdBuilder.Append($"{whereDelimiter}{propValue}");
                        }
                    }
                    catch (Exception ex)
                    {
                        var message = $"an error occurred while calculating {privateKeys[0].DbColumnName} of item at index {elementAbsIndex}";
                        throw new SqlGenerationException(SqlOperation.Delete, message, ex);
                    }
                }
            }
            if (elementIndex == -1)
                throw new ArgumentException($"There is no elements in the collection. At least one element must be.", nameof(elements));
            
            cmdBuilder.AppendLine(");");
            result.Add(new SqlCommandBuilderResult
                       {
                           Command                = cmdBuilder.ToString(),
                           Parameters             = parameters ?? new List<NpgsqlParameter>(),
                           IsThereReturningClause = false
                       });
            return result;
        }
    }
}
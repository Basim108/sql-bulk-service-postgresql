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
    /// Sql bulk upset command generator 
    /// </summary>
    public class UpsertSqlCommandBuilder : IUpsertSqlCommandBuilder
    {
        private readonly ILogger<UpsertSqlCommandBuilder> _logger;

        /// <summary> </summary>
        public UpsertSqlCommandBuilder(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<UpsertSqlCommandBuilder>();
        }

        /// <summary>
        /// Generates sql upset command for bunch of elements
        /// </summary>
        /// <param name="elements">elements that have to be upserted</param>
        /// <param name="entityProfile">elements type profile (contains mapping and other options)</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns a text of an sql upset command and collection of database parameters</returns>
        public IList<SqlCommandBuilderResult> Generate<TEntity>(ICollection<TEntity> elements, EntityProfile entityProfile, CancellationToken cancellationToken)
            where TEntity : class
        {
            if (elements == null)
                throw new ArgumentNullException(nameof(elements));
            if (entityProfile == null)
                throw new ArgumentNullException(nameof(entityProfile));

            if (elements.Count == 0)
                throw new ArgumentException("There is no elements in the collection. At least one element must be.", nameof(elements));

            _logger.LogTrace($"Generating upsert sql for {elements.Count} elements.");

            if (entityProfile.UniqueConstraint == null)
                throw new ArgumentException($"There is no unique constraint defined in the {entityProfile.GetType().FullName}", nameof(entityProfile));
            if (entityProfile.UniqueConstraint.UniqueProperties.All(p => p == null))
                throw new ArgumentException($"There is no unique properties defined in the {entityProfile.GetType().FullName}", nameof(entityProfile));

            var result = new List<SqlCommandBuilderResult>();

            if (_logger.IsEnabled(LogLevel.Debug)) {
                _logger.LogDebug($"{nameof(TEntity)}: {typeof(TEntity).FullName}");
                _logger.LogDebug($"{nameof(elements)}.Count: {elements.Count}");
            }

            var (columns, returningClause, upsertClause) = this.GenerateClauses(entityProfile);
            var hasReturningClause = !string.IsNullOrWhiteSpace(returningClause);
            cancellationToken.ThrowIfCancellationRequested();

            const int MAX_PARAMS_PER_CMD = 65_535;

            var commandHeader = $"insert into {entityProfile.TableName} ({columns}) values ";
            var entireCommandLength = commandHeader.Length
                                    + (returningClause?.Length ?? 0)
                                    + columns.Length * elements.Count;

            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug($"approximateEntireCommandLength: {entireCommandLength}");

            var paramsCount    = elements.Count * entityProfile.MaxPossibleSqlParameters;
            var sqlParameters  = new List<NpgsqlParameter>(paramsCount);
            var commandBuilder = new StringBuilder(entireCommandLength);
            commandBuilder.Append(commandHeader);
            var elementIndex    = -1;
            var elementAbsIndex = -1;
            using (var elementsEnumerator = elements.GetEnumerator()) {
                while (elementsEnumerator.MoveNext()) {
                    elementAbsIndex++;
                    var item = elementsEnumerator.Current;
                    if (item == null)
                        continue;

                    elementIndex++;
                    cancellationToken.ThrowIfCancellationRequested();

                    commandBuilder.Append('(');
                    var firstPropertyValue = true;
                    foreach (var pair in entityProfile.Properties) {
                        try {
                            var propInfo = pair.Value;
                            if (propInfo.IsAutoGenerated)
                                continue;
                            var delimiter = firstPropertyValue ? "" : ", ";
                            commandBuilder.Append(delimiter);

                            if (propInfo.IsDynamicallyInvoked()) {
                                var paramName = $"@param_{propInfo.DbColumnName}_{elementIndex}";
                                var value     = propInfo.GetPropertyValue(item);
                                if (value == null) {
                                    // as count of parameters are limited, it's better to save params for non null values
                                    commandBuilder.Append("null");
                                }
                                else {
                                    sqlParameters.Add(new NpgsqlParameter(paramName, propInfo.DbColumnType)
                                                      {
                                                          Value = value
                                                      });
                                    commandBuilder.Append(paramName);
                                }
                            }
                            else {
                                var propValue = propInfo.GetPropertyValueAsString(item);
                                commandBuilder.Append(propValue);
                            }
                            firstPropertyValue = false;
                        }
                        catch (Exception ex) {
                            var message = $"an error occurred while processing a property {pair.Key} of {entityProfile.EntityType.Name} entity, item idx: {elementAbsIndex}";
                            throw new SqlGenerationException(SqlOperation.Upsert, message, ex);
                        }
                    }
                    commandBuilder.Append(')');
                    if (sqlParameters.Count + entityProfile.MaxPossibleSqlParameters > MAX_PARAMS_PER_CMD) {
                        if (hasReturningClause) {
                            commandBuilder.AppendLine(upsertClause);
                            commandBuilder.Append(" returning ");
                            commandBuilder.Append(returningClause);
                        }
                        else
                            commandBuilder.Append(upsertClause);
                        commandBuilder.Append(";");
                        result.Add(new SqlCommandBuilderResult(
                                       commandBuilder.ToString(),
                                       sqlParameters,
                                       isThereReturningClause: hasReturningClause,
                                       elementsCount: elementIndex + 1
                                   ));
                        if (_logger.IsEnabled(LogLevel.Debug)) {
                            var (cmdSize, suffix) = ((long) commandBuilder.Length * 2).PrettifySize();
                            _logger.LogDebug($"Generated sql upsert command for {elementIndex + 1} {entityProfile.EntityType.Name} elements, command size {cmdSize:F2} {suffix}");
                        }
                        sqlParameters = new List<NpgsqlParameter>(sqlParameters.Count);
                        commandBuilder.Clear();
                        commandBuilder.Append(commandHeader);
                        elementIndex = -1;
                    }
                    else {
                        //  Finished with properties 
                        if (elements.Count > 1 && elementAbsIndex < elements.Count - 1)
                            commandBuilder.Append(", ");
                    }
                }
            }
            if (elementIndex == -1 && result.Count == 0)
                throw new ArgumentException("There is no elements in the collection. At least one element must be.", nameof(elements));
            if (hasReturningClause) {
                commandBuilder.AppendLine(upsertClause);
                commandBuilder.Append(" returning ");
                commandBuilder.Append(returningClause);
            }
            else
                commandBuilder.Append(upsertClause);
            commandBuilder.Append(";");
            result.Add(new SqlCommandBuilderResult(
                           commandBuilder.ToString(),
                           sqlParameters,
                           isThereReturningClause: hasReturningClause,
                           elementsCount: elementIndex + 1
                       ));
            return result;
        }

        /// <summary>
        /// In one pass generates both columns and returning clauses 
        /// </summary>
        /// <param name="entityProfile">elements type profile (contains mapping and other options)</param>
        /// <returns>
        /// Returns named tuple with generated columns, returning and upsert clauses.
        /// If there is no properties that has to be included into returning clause then ReturningClause item in the result tuple will be an empty string.
        /// </returns>
        // ReSharper disable once MemberCanBePrivate.Global  Needed to be public for unit testing purpose
        public (string Columns, string ReturningClause, string UpsertClause) GenerateClauses(EntityProfile entityProfile)
        {
            if (entityProfile == null)
                throw new ArgumentNullException(nameof(entityProfile));

            var properties           = entityProfile.Properties.Values;
            var upsertClause         = GenerateOnConflictClause(entityProfile.UniqueConstraint);
            var firstUpdateSetColumn = true;

            var returningClause      = "";
            var firstReturningColumn = true;

            var columns     = "";
            var firstColumn = true;
            foreach (var propInfo in properties) {
                if (propInfo.IsUpdatedAfterInsert || propInfo.IsUpdatedAfterUpdate) {
                    var returningDelimiter = firstReturningColumn ? "" : ", ";
                    returningClause      += $"{returningDelimiter}\"{propInfo.DbColumnName}\"";
                    firstReturningColumn =  false;
                }
                if (propInfo.IsAutoGenerated)
                    continue;
                if (!propInfo.IsPartOfUniqueConstraint) {
                    var upsertSetDelimiter = firstUpdateSetColumn ? " do update set " : ", ";
                    upsertClause         += $"{upsertSetDelimiter}\"{propInfo.DbColumnName}\" = excluded.\"{propInfo.DbColumnName}\"";
                    firstUpdateSetColumn =  false;
                }
                var delimiter = firstColumn ? "" : ", ";
                columns     += $"{delimiter}\"{propInfo.DbColumnName}\"";
                firstColumn =  false;
            }
            if (_logger.IsEnabled(LogLevel.Debug)) {
                _logger.LogDebug($"columns: {columns}");
                _logger.LogDebug($"upsertClause: {upsertClause}");
                _logger.LogDebug($"returningClause: {returningClause}");
            }
            return (columns, returningClause, upsertClause);
        }

        /// <summary>
        /// Generate on conflict clause
        /// </summary>
        /// <param name="uniqueConstraintInfo"></param>
        /// <returns></returns>
        public string GenerateOnConflictClause(EntityUniqueConstraint uniqueConstraintInfo)
        {
            if (uniqueConstraintInfo == null)
                throw new ArgumentNullException(nameof(uniqueConstraintInfo));

            var result = " on conflict ";
            if (string.IsNullOrWhiteSpace(uniqueConstraintInfo.Name)) {
                var firstProperty = uniqueConstraintInfo.UniqueProperties.FirstOrDefault(p => p != null);
                if (firstProperty == null)
                    throw new SqlGenerationException(SqlOperation.Upsert,
                                                     "It is impossible to generate a constraint name as neither it as set in constructor nor properties have been set as a part of unique constraint.");
                result += $"(\"{firstProperty.DbColumnName}\")";
            }
            else {
                result += $"on constraint \"{uniqueConstraintInfo.Name}\"";
            }
            return result;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Hrimsoft.SqlBulk.PostgreSql
{
    /// <summary>
    /// Implementation by using Npgsql DbCommand
    /// </summary>
    public class NpgsqlCommandsBulkService : IPostgreSqlBulkService
    {
        private readonly ILogger<NpgsqlCommandsBulkService> _logger;
        private readonly BulkServiceOptions _options;
        private readonly IInsertSqlCommandBuilder _insertCommandBuilder;
        private readonly IUpdateSqlCommandBuilder _updateCommandBuilder;
        private readonly IDeleteSqlCommandBuilder _deleteCommandBuilder;
        private readonly IUpsertSqlCommandBuilder _upsertCommandBuilder;

        public NpgsqlCommandsBulkService(
            BulkServiceOptions options,
            ILoggerFactory loggerFactory,
            IInsertSqlCommandBuilder insertCommandBuilder,
            IUpdateSqlCommandBuilder updateCommandBuilder,
            IDeleteSqlCommandBuilder deleteCommandBuilder,
            IUpsertSqlCommandBuilder upsertCommandBuilder)
        {
            _options = options;
            _insertCommandBuilder = insertCommandBuilder;
            _updateCommandBuilder = updateCommandBuilder;
            _deleteCommandBuilder = deleteCommandBuilder;
            _upsertCommandBuilder = upsertCommandBuilder;
            _logger = loggerFactory.CreateLogger<NpgsqlCommandsBulkService>();
        }

        /// <summary>
        /// Delete elements
        /// </summary>
        /// <param name="connection">Connection to a database</param>
        /// <param name="elements">Elements that have to be deleted</param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TEntity">Type of instances that have to be deleted</typeparam>
        /// <returns>Returns a collection of not operated items. It won't be empty with set FailureStrategies.Ignore strategy <see cref="FailureStrategies"/></returns> 
        public async Task<ICollection<TEntity>> DeleteAsync<TEntity>(
            [NotNull] NpgsqlConnection connection,
            [NotNull] ICollection<TEntity> elements,
            CancellationToken cancellationToken)
            where TEntity : class
        {
            return await ExecuteOperationAsync(SqlOperation.Delete, connection, elements, cancellationToken);
        }

        /// <summary>
        /// Insert new elements and update those already existed ones 
        /// </summary>
        /// <param name="connection">Connection to a database</param>
        /// <param name="elements">Elements that have to be inserted or updated</param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TEntity">Type of instances that have to be inserted or updated</typeparam>
        /// <returns>Returns a collection of not operated items. It won't be empty with set FailureStrategies.Ignore strategy <see cref="FailureStrategies"/></returns>
        public Task<ICollection<TEntity>> UpsertAsync<TEntity>(
            [NotNull] NpgsqlConnection connection,
            [NotNull] ICollection<TEntity> elements,
            CancellationToken cancellationToken)
            where TEntity : class
        {
            return ExecuteOperationAsync(SqlOperation.Upsert, connection, elements, cancellationToken);
        }

        /// <summary>
        /// Update elements
        /// </summary>
        /// <param name="connection">Connection to a database</param>
        /// <param name="elements">Elements that have to be updated</param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TEntity">Type of instances that have to be updated</typeparam>
        /// <returns>Returns a collection of not operated items. It won't be empty with set FailureStrategies.Ignore strategy <see cref="FailureStrategies"/></returns>
        public Task<ICollection<TEntity>> UpdateAsync<TEntity>(
            [NotNull] NpgsqlConnection connection,
            [NotNull] ICollection<TEntity> elements,
            CancellationToken cancellationToken)
            where TEntity : class
        {
            return ExecuteOperationAsync(SqlOperation.Update, connection, elements, cancellationToken);
        }

        /// <summary>
        /// Inserts elements
        /// </summary>
        /// <param name="connection">Connection to a database</param>
        /// <param name="elements">Elements that have to be inserted</param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TEntity">Type of instances that have to be inserted</typeparam>
        /// <returns>Returns a collection of not operated items. It won't be empty with set FailureStrategies.Ignore strategy <see cref="FailureStrategies"/></returns>
        public Task<ICollection<TEntity>> InsertAsync<TEntity>(
            [NotNull] NpgsqlConnection connection,
            [NotNull] ICollection<TEntity> elements,
            CancellationToken cancellationToken)
            where TEntity : class
        {
            return ExecuteOperationAsync(SqlOperation.Insert, connection, elements, cancellationToken);
        }

        public async Task<ICollection<TEntity>> ExecuteOperationAsync<TEntity>(
            SqlOperation operation,
            [NotNull] NpgsqlConnection connection,
            [NotNull] ICollection<TEntity> elements,
            CancellationToken cancellationToken)
            where TEntity : class
        {
            var entityType = typeof(TEntity);
            if (!_options.SupportedEntityTypes.ContainsKey(entityType))
                throw new ArgumentException($"Mapping for type '{entityType.FullName}' was not found.", nameof(elements));
            
            var entityProfile = _options.SupportedEntityTypes[entityType];
            var maximumEntitiesPerSent = GetCurrentMaximumSentElements(entityProfile);

            var result = new List<TEntity>(elements.Count);

            var currentFailureStrategy = GetCurrentFailureStrategy(entityProfile);
            var (needOperatedElements, needNotOperatedElements, needProblemElements)  = GetExtendedFailureInformation(entityProfile);
            NpgsqlTransaction transaction = null;
            if (currentFailureStrategy == FailureStrategies.StopEverythingAndRollback)
            {
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync(cancellationToken);

                transaction = connection.BeginTransaction();
            }

            if (maximumEntitiesPerSent == 0 || elements.Count <= maximumEntitiesPerSent)
            {
                try
                {
                    await ExecutePortionAsync(operation, connection, elements, entityProfile, cancellationToken);
                    if (transaction != null)
                        await transaction.CommitAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    var notOperatedElements = needNotOperatedElements ? elements : null;
                    var operatedElements = needOperatedElements ? new List<TEntity>() : null;
                    var problemElements = needProblemElements ? elements : null;
                    
                    await ProcessFailureAsync(operation, currentFailureStrategy, ex, transaction, operatedElements, notOperatedElements, problemElements, cancellationToken);
                    result.AddRange(elements);
                }
            }
            else
            {
                var operated = needOperatedElements ? new List<TEntity>(elements.Count) : null;
                var notOperated = needNotOperatedElements ? new List<TEntity>(elements.Count) : null;
                var problem = needProblemElements ? new List<TEntity>(maximumEntitiesPerSent) : null;
                
                var iterations = Math.Round((decimal) elements.Count / maximumEntitiesPerSent, MidpointRounding.AwayFromZero);
                for (var i = 0; i < iterations; i++)
                {
                    var portion = elements.Skip(i * maximumEntitiesPerSent).Take(maximumEntitiesPerSent).ToList();
                    try
                    {
                        await ExecutePortionAsync(operation, connection, portion, entityProfile, cancellationToken);
                        operated?.AddRange(portion);
                    }
                    catch (Exception ex)
                    {
                        problem?.AddRange(portion);
                        
                        switch (currentFailureStrategy)
                        {
                            // Ignore strategy is ignored here because it does not throw an SqlBulkExecutionException
                            
                            case FailureStrategies.StopEverything:
                            {
                                //Since i is not incremented yet, this command will add current portion to notOperated as well as the rest of elements 
                                notOperated?.AddRange(elements.Skip(i * maximumEntitiesPerSent).ToList());
                                break;
                            }
                            case FailureStrategies.StopEverythingAndRollback:
                            {
                                operated?.Clear();
                                notOperated?.AddRange(elements);
                                break;
                            }
                        }

                        await ProcessFailureAsync(operation, currentFailureStrategy, ex, transaction, operated, notOperated, problem, cancellationToken);
                        result.AddRange(portion);
                    }
                }
            }

            return result;
        }

        private ISqlCommandBuilder GetCommandBuilder(SqlOperation operation)
        {
            ISqlCommandBuilder result = null;
            switch (operation)
            {
                case SqlOperation.Insert: result = _insertCommandBuilder; break;
                case SqlOperation.Update: result = _updateCommandBuilder; break;
                case SqlOperation.Delete: result = _deleteCommandBuilder; break;
                case SqlOperation.Upsert: result = _upsertCommandBuilder; break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(operation), operation, $"Sql operation {operation} is not supported");
            }

            return result;
        }


        private (bool NeedOperated, bool NeedNotOperated, bool NeedProblem) GetExtendedFailureInformation([NotNull] EntityProfile entityProfile)
        {
            var needOperated = entityProfile.IsOperatedElementsEnabled ?? _options.IsOperatedElementsEnabled;
            var needNotOperated = entityProfile.IsNotOperatedElementsEnabled ?? _options.IsNotOperatedElementsEnabled;
            var needProblem = entityProfile.IsProblemElementsEnabled ?? _options.IsProblemElementsEnabled;
            return (needOperated, needNotOperated, needProblem);
        }

        private async Task ProcessFailureAsync<TEntity>(
            SqlOperation operation, 
            FailureStrategies currentFailureStrategy, 
            [NotNull] Exception ex, 
            NpgsqlTransaction transaction, 
            ICollection<TEntity> operatedElements, 
            ICollection<TEntity> notOperatedElements, 
            ICollection<TEntity> problemElements, 
            CancellationToken cancellationToken) where TEntity : class
        {
            _logger.LogError(ex, "Bulk command execution failed");

            if (transaction != null)
                await transaction.RollbackAsync(cancellationToken);

            if (currentFailureStrategy == FailureStrategies.StopEverything ||
                currentFailureStrategy == FailureStrategies.StopEverythingAndRollback)
            {
                throw new SqlBulkExecutionException<TEntity>(ex, operation, currentFailureStrategy,problemElements, notOperatedElements, operatedElements);
            }
        }

        /// <summary>
        /// Makes a real command exec
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="connection"></param>
        /// <param name="elements"></param>
        /// <param name="entityProfile"></param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TEntity"></typeparam>
        public async Task ExecutePortionAsync<TEntity>(
            SqlOperation operation,
            [NotNull] NpgsqlConnection connection,
            [NotNull] ICollection<TEntity> elements,
            [NotNull] EntityProfile entityProfile,
            CancellationToken cancellationToken)
            where TEntity : class
        {
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            var commandBuilder = GetCommandBuilder(operation);
            
            var commandResult = commandBuilder.Generate(elements, entityProfile, cancellationToken);
            using (var command = new NpgsqlCommand(commandResult.Command, connection))
            {
                foreach (var param in commandResult.Parameters)
                {
                    command.Parameters.Add(param);
                }

                using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                using (var elementsEnumerator = elements.GetEnumerator())
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        if (!elementsEnumerator.MoveNext())
                        {
                            var message =
                                $"There is no more items in the elements collection, but reader still has tuples to read.elements.{nameof(elements.Count)}: {elements.Count}";
                            _logger.LogError(message);
                            throw new SqlBulkExecutionException<TEntity>(message);
                        }

                        if (commandResult.IsThereReturningClause)
                            UpdatePropertiesAfterCommandExecution(reader, elementsEnumerator.Current, entityProfile.Properties, operation, cancellationToken);
                    }

                    await reader.CloseAsync();
                }
            }
        }

        public FailureStrategies GetCurrentFailureStrategy([NotNull] EntityProfile entityProfile)
        {
            return entityProfile.FailureStrategy ?? _options.FailureStrategy;
        }

        public int GetCurrentMaximumSentElements([NotNull] EntityProfile entityProfile)
        {
            return entityProfile.MaximumSentElements ?? _options.MaximumSentElements;
        }

        /// <summary>
        /// Updates item's property from returning clause 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="item"></param>
        /// <param name="propertyProfiles"></param>
        /// <param name="operation"></param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public void UpdatePropertiesAfterCommandExecution<TEntity>(
            NpgsqlDataReader reader,
            TEntity item,
            IDictionary<string, PropertyProfile> propertyProfiles,
            SqlOperation operation,
            CancellationToken cancellationToken)
            where TEntity : class
        {
            foreach (var propInfoPair in propertyProfiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (operation == SqlOperation.Insert && propInfoPair.Value.IsUpdatedAfterInsert ||
                    operation == SqlOperation.Update && propInfoPair.Value.IsUpdatedAfterUpdate ||
                    operation == SqlOperation.Upsert && (propInfoPair.Value.IsUpdatedAfterInsert || propInfoPair.Value.IsUpdatedAfterUpdate))
                {
                    var value = reader[propInfoPair.Key];
                    propInfoPair.Value.SetPropertyValue(item, value);
                }
            }
        }
    }
}
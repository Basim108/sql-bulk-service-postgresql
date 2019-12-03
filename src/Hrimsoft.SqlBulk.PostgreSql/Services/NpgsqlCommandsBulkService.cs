using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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

        public NpgsqlCommandsBulkService(
            BulkServiceOptions options,
            ILoggerFactory loggerFactory,
            IInsertSqlCommandBuilder insertCommandBuilder,
            IUpdateSqlCommandBuilder updateCommandBuilder,
            IDeleteSqlCommandBuilder deleteCommandBuilder)
        {
            _options = options;
            _insertCommandBuilder = insertCommandBuilder;
            _updateCommandBuilder = updateCommandBuilder;
            _deleteCommandBuilder = deleteCommandBuilder;
            _logger = loggerFactory.CreateLogger<NpgsqlCommandsBulkService>();
        }
        
        /// <summary>
        /// Delete elements
        /// </summary>
        /// <param name="connection">Connection to a database</param>
        /// <param name="elements">Elements that have to be deleted</param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TEntity">Type of instances that have to be deleted</typeparam>
        public async Task DeleteAsync<TEntity>(
            [NotNull] NpgsqlConnection connection,
            [NotNull] ICollection<TEntity> elements,
            CancellationToken cancellationToken)
            where TEntity : class
        {
            await ExecuteOperationAsync(_deleteCommandBuilder, connection, elements, cancellationToken);
        }
        
        /// <summary>
        /// Update elements
        /// </summary>
        /// <param name="connection">Connection to a database</param>
        /// <param name="elements">Elements that have to be updated</param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TEntity">Type of instances that have to be updated</typeparam>
        /// <returns>The same collection of items with updated from the storage properties that marked as mast update after insert (see PropertyProfile.MustBeUpdatedAfterUpdate)</returns>
        public Task<ICollection<TEntity>> UpdateAsync<TEntity>(
            [NotNull] NpgsqlConnection connection,
            [NotNull] ICollection<TEntity> elements,
            CancellationToken cancellationToken)
            where TEntity : class
        {
            return ExecuteOperationAsync(_updateCommandBuilder, connection, elements, cancellationToken);
        }

        /// <summary>
        /// Inserts elements
        /// </summary>
        /// <param name="connection">Connection to a database</param>
        /// <param name="elements">Elements that have to be inserted</param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TEntity">Type of instances that have to be inserted</typeparam>
        /// <returns>The same collection of items with updated from the storage properties that marked as mast update after update (see PropertyProfile.MustBeUpdatedAfterInsert)</returns>
        public Task<ICollection<TEntity>> InsertAsync<TEntity>(
            [NotNull] NpgsqlConnection connection,
            [NotNull] ICollection<TEntity> elements,
            CancellationToken cancellationToken)
            where TEntity : class
        {
            return ExecuteOperationAsync(_insertCommandBuilder, connection, elements, cancellationToken);
        }

        public async Task<ICollection<TEntity>> ExecuteOperationAsync<TEntity>(
            [NotNull] ISqlCommandBuilder commandBuilder,
            [NotNull] NpgsqlConnection connection,
            [NotNull] ICollection<TEntity> elements,
            CancellationToken cancellationToken)
            where TEntity : class
        {
            var entityType = typeof(TEntity);
            if (!_options.SupportedEntityTypes.ContainsKey(entityType))
                throw new ArgumentException($"Mapping for type '{entityType.FullName}' was not found.", nameof(elements));

            var entityProfile = _options.SupportedEntityTypes[entityType];
            var maximumEntitiesPerSent = entityProfile.MaximumSentElements > 0
                ? entityProfile.MaximumSentElements
                : _options.MaximumSentElements;

            var result = new List<TEntity>(elements.Count);

            if (maximumEntitiesPerSent == 0)
            {
                var subset = await ExecutePortionAsync(commandBuilder, connection, elements, entityProfile, cancellationToken);
                result.AddRange(subset);
            }
            else
            {
                var iterations = Math.Round((decimal) elements.Count / maximumEntitiesPerSent, MidpointRounding.AwayFromZero);
                for (var i = 0; i < iterations; i++)
                {
                    var portion = elements.Skip(i * maximumEntitiesPerSent).Take(maximumEntitiesPerSent).ToList();
                    var subset = await ExecutePortionAsync(commandBuilder, connection, portion, entityProfile, cancellationToken);
                    result.AddRange(subset);
                }
            }

            return result;    
        }

        /// <summary>
        /// Makes a real command exec
        /// </summary>
        /// <param name="commandBuilder"></param>
        /// <param name="connection"></param>
        /// <param name="elements"></param>
        /// <param name="entityProfile"></param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public async Task<ICollection<TEntity>> ExecutePortionAsync<TEntity>(
            [NotNull] ISqlCommandBuilder commandBuilder,
            [NotNull] NpgsqlConnection connection,
            [NotNull] ICollection<TEntity> elements,
            [NotNull] EntityProfile entityProfile,
            CancellationToken cancellationToken)
            where TEntity : class
        {
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            var commandResult = commandBuilder.Generate(elements, entityProfile, cancellationToken);
            using (var command = new NpgsqlCommand(commandResult.Command, connection))
            {
                foreach (var param in commandResult.Parameters)
                {
                    command.Parameters.Add(param);
                }

                //TODO: Make an option where a user will be able to set transaction behaviour for each portion. Whether should we ignore situation when some portions execution failed or not.
                var transaction = connection.BeginTransaction();
                try
                {
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
                                throw new SqlBulkServiceException(message);
                            }
                            
                            if(commandResult.IsThereReturningClause)
                                await UpdatePropertiesAfterCommandExecutionAsync(reader, elementsEnumerator.Current, entityProfile.Properties, cancellationToken);
                        }

                        await reader.CloseAsync();
                    }

                    await transaction.CommitAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "bulk command execution failed");
                    await transaction.RollbackAsync(cancellationToken);
                    //TODO: Make an option where a user will be able to select the behaviour of this situation. ignore it or rise an exception higher
                }
            }

            return elements;
        }

        /// <summary>
        /// Updates item's property from returning clause 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="item"></param>
        /// <param name="propertyProfiles"></param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public async Task UpdatePropertiesAfterCommandExecutionAsync<TEntity>(
            NpgsqlDataReader reader,
            TEntity item,
            IDictionary<string, PropertyProfile> propertyProfiles,
            CancellationToken cancellationToken)
            where TEntity : class
        {
            foreach (var propInfoPair in propertyProfiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!propInfoPair.Value.IsUpdatedAfterInsert)
                    continue;
                var value = reader[propInfoPair.Key];
                propInfoPair.Value.SetPropertyValue(item, value);
            }
        }
    }
}
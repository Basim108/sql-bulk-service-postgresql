using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Hrimsoft.SqlBulk.PostgreSql
{
    /// <summary>
    /// Mediator selects the best sql-command builder for the given entity type
    /// </summary>
    public class DeleteSqlCommandMediator : IDeleteSqlCommandBuilder
    {
        private readonly WhereInDeleteSqlCommandBuilder    _whereInBuilder;
        private readonly WhereOrDeleteSqlCommandBuilder    _whereOrBuilder;
        private readonly ILogger<DeleteSqlCommandMediator> _logger;

        public DeleteSqlCommandMediator(WhereInDeleteSqlCommandBuilder    whereInBuilder,
                                        WhereOrDeleteSqlCommandBuilder    whereOrBuilder,
                                        ILogger<DeleteSqlCommandMediator> logger)
        {
            _whereInBuilder = whereInBuilder;
            _whereOrBuilder = whereOrBuilder;
            _logger         = logger;
        }

        /// <inheritdoc />
        public IList<SqlCommandBuilderResult> Generate<TEntity>(ICollection<TEntity> elements, EntityProfile entityProfile, CancellationToken cancellationToken) where TEntity : class
        {
            if (elements == null)
                throw new ArgumentNullException(nameof(elements));
            if (entityProfile == null)
                throw new ArgumentNullException(nameof(entityProfile));

            if (elements.Count == 0)
                throw new ArgumentException("There is no elements in the collection. At least one element must be.", nameof(elements));

            var privateKeyCount = entityProfile.Properties.Count(x => x.Value.IsPrivateKey);
            if (privateKeyCount == 0)
                throw new ArgumentException($"Entity {entityProfile.EntityType.FullName} must have at least one private key.",
                                            nameof(entityProfile));
            if (privateKeyCount == 1) {
                _logger.LogDebug($"WhereIn sql-delete command builder has been selected as there is only one private key in entity {entityProfile.EntityType.FullName}");
                return _whereInBuilder.Generate(elements, entityProfile, cancellationToken);
            }
            _logger.LogDebug($"WhereOr sql-delete command builder has been selected as there are more than one private keys in entity {entityProfile.EntityType.FullName}");
            return _whereOrBuilder.Generate(elements, entityProfile, cancellationToken);
        }
    }
}
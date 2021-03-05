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
        private readonly SimpleDeleteSqlCommandBuilder     _simpleBuilder;
        private readonly WhereInDeleteSqlCommandBuilder    _whereInBuilder;
        private readonly ILogger<DeleteSqlCommandMediator> _logger;

        public DeleteSqlCommandMediator(SimpleDeleteSqlCommandBuilder     simpleBuilder,
                                        WhereInDeleteSqlCommandBuilder    whereInBuilder,
                                        ILogger<DeleteSqlCommandMediator> logger)
        {
            _simpleBuilder  = simpleBuilder;
            _whereInBuilder = whereInBuilder;
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

            var privateKeys = entityProfile.Properties
                                           .Values
                                           .Where(x => x.IsPrivateKey)
                                           .ToList();
            if (privateKeys.Count == 0)
                throw new ArgumentException($"Entity {entityProfile.EntityType.FullName} must have at least one private key.",
                                            nameof(entityProfile));
            if (privateKeys.Count > 1)
            {
                _logger.LogDebug($"Simple sql-delete command builder has been selected as there are more than one private keys in entity {entityProfile.EntityType.FullName}");
                return _simpleBuilder.Generate(elements, entityProfile, cancellationToken);
            }
            _logger.LogDebug($"WhereIn sql-delete command builder has been selected as there is only one private key in entity {entityProfile.EntityType.FullName}");
            return _whereInBuilder.Generate(elements, entityProfile, cancellationToken);
        }
    }
}
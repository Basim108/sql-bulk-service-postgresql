using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Hrimsoft.SqlBulk.PostgreSql
{
    /// <summary>
    /// Information about unique constraint
    /// </summary>
    public class EntityUniqueConstraint
    {
        /// <inheritdoc />
        public EntityUniqueConstraint([NotNull] string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));
            
            Name = name;
            UniqueProperties = new List<PropertyProfile>();
        }

        public EntityUniqueConstraint([NotNull] PropertyProfile uniqueProperty)
        {
            UniqueProperties = new List<PropertyProfile>(){ uniqueProperty };
        }

        /// <summary>
        /// Database defined constraint name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Value of all these properties together are unique in the table
        /// </summary>
        public ICollection<PropertyProfile> UniqueProperties { get; }
    }
}
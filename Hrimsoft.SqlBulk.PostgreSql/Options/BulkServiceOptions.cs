using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Hrimsoft.SqlBulk.PostgreSql
{
    /// <summary>
    /// Information about mapping business entities to the postgres entities 
    /// </summary>
    public class BulkServiceOptions
    {
        private readonly Dictionary<Type, EntityProfile> _supportedEntityTypes = new Dictionary<Type, EntityProfile>();

        /// <summary>
        /// Information about mapping business entities to the postgres entities
        /// </summary>
        public IDictionary<Type, EntityProfile> SupportedEntityTypes => _supportedEntityTypes;

        /// <summary>
        /// The maximum number of elements that have to be included into one command.
        /// If 0 then unlimited. If n then all elements will be split into n-sized arrays and will be send one after another.
        ///
        /// Default: 0 
        /// </summary>
        public int MaximumSentElements { get; set; }
        
        /// <summary>
        /// If true then bulk operation will exclude those items that were not operated, for example, due to the constraint validation errors.
        /// Use of this option makes sense when MaximumSentElements is greater than 0.
        /// In this case the set of operating elements will be split on subsets of the MaximumSentElements size;
        /// And if operating (insert or update) of one of these subset fails then elements from this subset will be excluded from result set.
        ///
        /// If false then in the first failed subset the corresponding exception will be thrown
        /// 
        /// Default: false 
        /// </summary>
        public bool IgnoreWrongItems { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entityProfile"></param>
        /// <typeparam name="TEntity"></typeparam>
        public BulkServiceOptions RegisterEntityProfile<TEntity>([NotNull] EntityProfile entityProfile)
        {
            _supportedEntityTypes[entityProfile.EntityType] = entityProfile;
            
            return this;
        }
    }
}
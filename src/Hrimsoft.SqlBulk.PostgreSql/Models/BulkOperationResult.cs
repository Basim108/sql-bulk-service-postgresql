using System.Collections.Generic;

namespace Hrimsoft.SqlBulk.PostgreSql
{
    /// <summary>
    /// The result of any bulk operation 
    /// </summary>
    /// <typeparam name="TEntity">Type of operated entities</typeparam>
    public class BulkOperationResult<TEntity>
    {
        public BulkOperationResult()
        {
            Operated = new List<TEntity>();
            NotOperated = new List<TEntity>();
        }
        
        /// <summary>
        /// The result of any bulk operation
        /// </summary>
        public BulkOperationResult(int maxElementsCapacity)
        {
            Operated = new List<TEntity>(maxElementsCapacity);
            NotOperated = new List<TEntity>(maxElementsCapacity);
        }
        
        /// <summary>
        /// Elements that were successfully operated
        /// </summary>
        public List<TEntity> Operated { get; }
        
        /// <summary>
        /// Elements that were failed to operate
        /// </summary>
        public List<TEntity> NotOperated { get; }
    }
}
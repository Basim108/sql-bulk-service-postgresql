using System;

namespace Hrimsoft.SqlBulk.PostgreSql.Attributes
{
    /// <summary>
    /// Decorate with this attribute those properties and fields that have to be updated after insert operation.
    /// This makes sense when properties or fields values are changed by the database e.g. triggers
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class UpdateAfterInsertAttribute: Attribute
    {
        
    }
}
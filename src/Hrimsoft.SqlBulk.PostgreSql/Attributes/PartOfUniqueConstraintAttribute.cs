using System;

namespace Hrimsoft.SqlBulk.PostgreSql.Attributes
{
    /// <summary>
    /// Decorate with this attribute those properties and fields that are included into unique constraint
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class PartOfUniqueConstraintAttribute: Attribute
    {
        
    }
}
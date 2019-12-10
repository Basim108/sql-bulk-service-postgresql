using System;

namespace Hrimsoft.SqlBulk.PostgreSql.Attributes
{
    /// <summary>
    /// Decorate with this attribute those properties and fields which values are generated by database
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class AutoGeneratedAttribute: Attribute
    {
        
    }
}
namespace Hrimsoft.SqlBulk.PostgreSql
{
    /// <summary>
    /// Defines the set of supporting naming strategies 
    /// </summary>
    public enum NamingStrategy
    {
        /// <summary>
        /// PascalCase
        /// </summary>
        PascalCase,
        
        /// <summary>
        /// camelCase
        /// </summary>
        CamelCase,
        
        /// <summary>
        /// snake_case
        /// </summary>
        SnakeCase
    }
}
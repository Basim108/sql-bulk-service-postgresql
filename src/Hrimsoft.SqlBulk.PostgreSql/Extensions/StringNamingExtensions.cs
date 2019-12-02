using System;
using System.Linq;

namespace Hrimsoft.SqlBulk.PostgreSql
{
    /// <summary>
    /// Methods to implement conversion strings to different kinds of Naming Strategies
    /// </summary>
    internal static class StringNamingExtensions
    {
        /// <summary>
        /// Converts a string to a case that is defined by naming strategy
        /// </summary>
        /// <param name="source">source string that has to be converted</param>
        /// <param name="strategy">To what kind of case the source string should be converted</param>
        /// <exception cref="NotSupportedException">In case NamingStrategy enum would be changed without changing this method.</exception>
        /// <returns>Returns string in case defined by a strategy</returns>
        public static string ApplyNamingStrategy(this string source, NamingStrategy strategy)
        {
            if (string.IsNullOrWhiteSpace(source))
                return source;
            
            switch (strategy)
            {
               case NamingStrategy.CamelCase: return source.ToCamelCase();   
               case NamingStrategy.PascalCase: return source.ToPascalCase();   
               case NamingStrategy.SnakeCase: return source.ToSnakeCase();   
            }
            
            throw new NotSupportedException($"Strategy '{strategy}' is not supported");
        }
        
        /// <summary>
        /// Convert a snake cased string to the camel case where the first character is written in upper case.
        /// </summary>
        /// <param name="source">snake cased string like: "to_camel_case"</param>
        /// <returns>Returns camel cased string like: "ToCamelCase"</returns>
        public static string ToPascalCase(this string source)
        {
            var result = "";

            if (!string.IsNullOrWhiteSpace(source))
            {
                var parts = source.SplitOnToParts();

                foreach (var name in parts)
                    result += char.ToUpperInvariant(name[0]) + name.Substring(1);
            }

            return result;
        }

        /// <summary>
        /// Convert a snake cased string to the camel case where the first character is written in the lower case.
        /// </summary>
        /// <param name="source">snake cased string like: "to_camel_case"</param>
        /// <returns>Returns camel cased string like: "toCamelCase"</returns>
        public static string ToCamelCase(this string source)
        {
            var result = "";

            if (!string.IsNullOrWhiteSpace(source))
            {
                var parts = source.SplitOnToParts().ToList();
                result = parts[0];
                for (var i = 1; i < parts.Count; i++)
                {
                    var name = parts[i];
                    result += char.ToUpperInvariant(name[0]) + name.Substring(1);
                }
            }

            return result;
        }

        /// <summary>
        /// Convert a camel cased string to the snake cased one.
        /// </summary>
        /// <param name="source">camel cased string like: "toSnakeCase" or "ToSnakeCase"</param>
        /// <returns>Returns snake cased string like: "to_snake_case"</returns>
        public static string ToSnakeCase(this string source)
        {
            var result = "";
            if (!string.IsNullOrWhiteSpace(source))
            {
                var parts = source.SplitOnToParts();
                result = string.Join("_", parts);
            }

            return result;
        }
    }
}
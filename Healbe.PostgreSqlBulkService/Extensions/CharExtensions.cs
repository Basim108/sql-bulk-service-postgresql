namespace Hrimsoft.PostgresSqlBulkService
{
    /// <summary>
    /// Extensions for char premetives
    /// </summary>
    internal static class CharExtensions
    {
        /// <summary>
        /// Tests a symbol for being a letter
        /// </summary>
        /// <param name="symbol">tested symbol</param>
        /// <returns></returns>
        public static bool IsAnEnglishLetter(this char symbol)
        {
            var result = symbol >= 'a' && symbol <= 'z' || symbol >= 'A' && symbol <= 'Z';
            return result;
        }

        /// <summary>
        /// Tests a symbol for being upper case
        /// </summary>
        /// <param name="symbol">tested symbol</param>
        /// <returns></returns>
        public static bool IsUpperCase(this char symbol)
        {
            var result = symbol >= 'A' && symbol <= 'Z';
            return result;
        }
        
        /// <summary>
        /// Tests a symbol for being lower case
        /// </summary>
        /// <param name="symbol">tested symbol</param>
        /// <returns></returns>
        public static bool IsLowerCase(this char symbol)
        {
            var result = symbol >= 'a' && symbol <= 'z';
            return result;
        }
        
        /// <summary>
        /// Tests a symbol for being a delimiter
        /// </summary>
        /// <param name="symbol">tested symbol</param>
        /// <returns></returns>
        public static bool IsDelimiter(this char symbol)
        {
            var result = symbol == ' ' || symbol == '_' || symbol == '.';
            return result;
        }
    }
}
namespace Hrimsoft.SqlBulk.PostgreSql
{
    public static class LongExtensions
    {
        /// <summary>
        /// Prettify size in bytes to the closest readable Kb or Mb or Gb, etc
        /// </summary>
        /// <param name="sizeInBytes"></param>
        /// <returns>Returns divided size and suffix Kb, Mb, etc</returns>
        public static (float, string) PrettifySize(this long sizeInBytes)
        {
            var   suffix     = "bytes";
            var   size       = sizeInBytes;
            float castedSize = 0;
            if (size > 1024)
            {
                castedSize = size / 1024f;
                suffix     = "Kb";
            }
            else
                return (size, suffix);

            if (castedSize > 1024)
            {
                castedSize = castedSize / 1024f;
                suffix     = "Mb";
            }
            else
                return (castedSize, suffix);

            if (castedSize > 1024)
            {
                castedSize = castedSize / 1024f;
                suffix     = "Gb";
            }
            else
                return (castedSize, suffix);

            if (castedSize > 1024)
            {
                castedSize = castedSize / 1024;
                suffix     = "Tb";
            }
            return (castedSize, suffix);
        }
    }
}
using System;

namespace OllamaClientLibrary.Converters
{
    /// <summary>
    /// Provides methods to convert between different size units.
    /// </summary>
    public static class SizeConverter
    {
        public static long GigabytesToBytes(long? value)
        {
            if (!value.HasValue)
                return 0;

            return value.Value * 1024 * 1024 * 1024;
        }

        public static double BytesToGigabytes(long? bytes)
        {
            if (!bytes.HasValue)
                return 0;

            const long BytesInOneGB = 1024L * 1024 * 1024;

            return Math.Round((double)bytes / BytesInOneGB, 2);
        }
    }
}

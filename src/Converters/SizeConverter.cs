using System;

namespace OllamaClientLibrary.Converters
{
    /// <summary>
    /// Provides methods to convert between different size units.
    /// </summary>

    /// <summary>
    /// Provides methods to convert between different size units.
    /// </summary>
    public static class SizeConverter
    {
        private const long BytesInOneGB = 1_073_741_824L;

        public static long GigabytesToBytes(double? value)
        {
            if (!value.HasValue)
                return 0;

            return (long)(value.Value * BytesInOneGB);
        }

        public static double BytesToGigabytes(long? bytes)
        {
            if (!bytes.HasValue)
                return 0;

            return (double)bytes.Value / BytesInOneGB;
        }
    }

}

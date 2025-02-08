namespace Ollama.NET.Converters
{
    internal static class SizeConverter
    {
        public static long GigabytesToBytes(long value)
            => value * 1024 * 1024 * 1024;

        public static double BytesToGigabytes(long bytes)
        {
            const long BytesInOneGB = 1024L * 1024 * 1024;

            return (double)bytes / BytesInOneGB;
        }
    }
}

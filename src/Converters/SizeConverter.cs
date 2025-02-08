namespace Ollama.NET.Converters
{
    public static class SizeConverter
    {
        public static long GigabytesToBytes(long value)
            => value * 1024 * 1024 * 1024;

        public static double BytesToGigabytes(long? bytes)
        {
            if(!bytes.HasValue)
                return 0;

            const long BytesInOneGB = 1024L * 1024 * 1024;

            return Math.Round((double)bytes / BytesInOneGB, 2);
        }
    }
}

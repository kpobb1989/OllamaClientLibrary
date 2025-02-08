namespace Ollama.NET.Constants
{
    /// <summary>
    /// Provides predefined temperature values for different use cases.
    /// </summary>
    public static class Temperature
    {
        public static float CodingOrMath = 0.0f;
        public static float DataCleaningOrAnalysis = 1.0f;
        public static float GeneralConversationOrTranslation = 1.3f;
        public static float CreativityWritingOrPoetry = 1.5f;
    }
}

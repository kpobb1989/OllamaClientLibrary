using Newtonsoft.Json;

using System;

namespace OllamaClientLibrary.Dto.PullModel
{
    internal class PullModelResponse
    {
        public string? Status { get; set; }
        public string? Error { get; set; }
        public long Total { get; set; }
        public long Completed { get; set; }

        [JsonIgnore]
        public double Percentage
        {
            get
            {
                if (Total <= 0) return 0.0;  // Ensure we don't default to 100%
                double percentage = (Completed * 100.0) / Total;
                return Math.Clamp(percentage, 0.0, 100.0);  // Prevent out-of-range values
            }
        }
    }
}

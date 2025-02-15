﻿using OllamaClientLibrary.Constants;

namespace OllamaClientLibrary.Models
{
    public class OllamaChatMessage
    {
        /// <summary>
        /// The role of the message sender.
        /// </summary>
        public MessageRole Role { get; set; }

        /// <summary>
        /// The content of the message.
        /// </summary>
        public object? Content { get; set; }
    }
}

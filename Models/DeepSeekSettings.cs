namespace webprogbackend.Models
{
    public class DeepSeekSettings
    {
        public string ApiKey { get; set; } = "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";
        public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1/chat/completions";
        public string DefaultModel { get; set; } = "deepseek/deepseek-chat-v3-0324:free";
        public int Timeout { get; set; } = 60000; // milliseconds
        public int MaxRetries { get; set; } = 3;
        public string HttpReferer { get; set; } = "https://webprogbackend.com";
        public string XTitle { get; set; } = "Web Programming E-Commerce Backend";
    }
}
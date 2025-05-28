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
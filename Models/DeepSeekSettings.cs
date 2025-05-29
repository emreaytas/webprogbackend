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
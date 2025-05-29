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
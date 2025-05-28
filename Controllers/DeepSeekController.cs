using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace webprogbackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeepSeekController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DeepSeekController> _logger;

        public DeepSeekController(HttpClient httpClient, IConfiguration configuration, ILogger<DeepSeekController> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        // POST: api/DeepSeek/chat
        [HttpPost("chat")]
        public async Task<ActionResult<DeepSeekResponse>> Chat([FromBody] DeepSeekRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    return BadRequest(new { error = "Message cannot be empty" });
                }

                var apiKey = _configuration["DeepSeek:ApiKey"];
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return StatusCode(500, new { error = "DeepSeek API key not configured" });
                }

                var deepSeekRequest = new
                {
                    model = request.Model ?? "deepseek-chat",
                    messages = new[]
                    {
                        new
                        {
                            role = "system",
                            content = request.SystemPrompt ?? "You are a helpful assistant that provides accurate and concise responses."
                        },
                        new
                        {
                            role = "user",
                            content = request.Message
                        }
                    },
                    max_tokens = request.MaxTokens ?? 1000,
                    temperature = request.Temperature ?? 0.7,
                    top_p = request.TopP ?? 0.9,
                    stream = false
                };

                var jsonContent = JsonSerializer.Serialize(deepSeekRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var response = await _httpClient.PostAsync("https://api.deepseek.com/chat/completions", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"DeepSeek API error: {response.StatusCode} - {errorContent}");

                    return StatusCode(500, new
                    {
                        error = "Failed to get response from DeepSeek API",
                        details = errorContent,
                        statusCode = response.StatusCode
                    });
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<DeepSeekApiResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (apiResponse?.Choices == null || !apiResponse.Choices.Any())
                {
                    return StatusCode(500, new { error = "No response received from DeepSeek API" });
                }

                var result = new DeepSeekResponse
                {
                    Message = apiResponse.Choices[0].Message.Content,
                    Model = apiResponse.Model,
                    Usage = new UsageInfo
                    {
                        PromptTokens = apiResponse.Usage?.PromptTokens ?? 0,
                        CompletionTokens = apiResponse.Usage?.CompletionTokens ?? 0,
                        TotalTokens = apiResponse.Usage?.TotalTokens ?? 0
                    },
                    CreatedAt = DateTime.UtcNow
                };

                return Ok(result);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed while calling DeepSeek API");
                return StatusCode(500, new { error = "Network error occurred", details = ex.Message });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse DeepSeek API response");
                return StatusCode(500, new { error = "Invalid response format from DeepSeek API", details = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while processing DeepSeek request");
                return StatusCode(500, new { error = "An unexpected error occurred", details = ex.Message });
            }
        }

        // POST: api/DeepSeek/analyze
        [HttpPost("analyze")]
        public async Task<ActionResult<DeepSeekResponse>> AnalyzeText([FromBody] TextAnalysisRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Text))
                {
                    return BadRequest(new { error = "Text cannot be empty" });
                }

                var systemPrompt = request.AnalysisType.ToLower() switch
                {
                    "sentiment" => "You are a sentiment analysis expert. Analyze the sentiment of the given text and provide a detailed explanation. Classify it as positive, negative, or neutral, and explain your reasoning.",
                    "summary" => "You are a text summarization expert. Provide a concise and accurate summary of the given text, capturing the key points and main ideas.",
                    "keywords" => "You are a keyword extraction expert. Extract the most important keywords and key phrases from the given text. Present them in order of importance.",
                    "translation" => $"You are a professional translator. Translate the given text to {request.TargetLanguage ?? "English"}. Provide an accurate and natural translation.",
                    "grammar" => "You are a grammar and writing expert. Check the given text for grammar, spelling, and style issues. Provide corrections and suggestions for improvement.",
                    _ => "You are a text analysis expert. Analyze the given text and provide insights about its content, structure, and meaning."
                };

                var analysisRequest = new DeepSeekRequest
                {
                    Message = request.Text,
                    SystemPrompt = systemPrompt,
                    Model = "deepseek-chat",
                    MaxTokens = 1500,
                    Temperature = 0.3
                };

                return await Chat(analysisRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while analyzing text");
                return StatusCode(500, new { error = "An error occurred during text analysis", details = ex.Message });
            }
        }

        // POST: api/DeepSeek/code-review
        [HttpPost("code-review")]
        [Authorize] // Sadece authenticated kullanıcılar
        public async Task<ActionResult<DeepSeekResponse>> ReviewCode([FromBody] CodeReviewRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Code))
                {
                    return BadRequest(new { error = "Code cannot be empty" });
                }

                var systemPrompt = $@"You are an expert code reviewer specializing in {request.Language ?? "general programming"}. 
                Review the provided code and analyze it for:
                1. Code quality and best practices
                2. Potential bugs and issues
                3. Performance optimizations
                4. Security vulnerabilities
                5. Code readability and maintainability
                6. Suggested improvements

                Provide specific, actionable feedback with examples where possible.";

                var reviewRequest = new DeepSeekRequest
                {
                    Message = $"Please review this {request.Language ?? "code"}:\n\n```{request.Language ?? "text"}\n{request.Code}\n```",
                    SystemPrompt = systemPrompt,
                    Model = "deepseek-coder",
                    MaxTokens = 2000,
                    Temperature = 0.2
                };

                return await Chat(reviewRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while reviewing code");
                return StatusCode(500, new { error = "An error occurred during code review", details = ex.Message });
            }
        }

        // GET: api/DeepSeek/models
        [HttpGet("models")]
        public ActionResult<IEnumerable<string>> GetAvailableModels()
        {
            var models = new[]
            {
                "deepseek-chat",
                "deepseek-coder"
            };

            return Ok(models);
        }


        [HttpPost("simple-chat")]
        public async Task<ActionResult<SimpleChatResponse>> SimpleChat([FromBody] SimpleChatRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    return BadRequest(new { error = "Message cannot be empty" });
                }

                var apiKey = _configuration["DeepSeek:ApiKey"];
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return StatusCode(500, new { error = "DeepSeek API key not configured" });
                }

                // E-ticaret bağlamında yardımcı sistem promptu
                var systemPrompt = @"You are a helpful e-commerce assistant for an online store. 
        You help customers find products, provide recommendations, and answer shopping-related questions.
        Be friendly, helpful, and try to suggest relevant products when appropriate.
        If someone asks about a specific need (like hair cutting), recommend suitable product categories.
        Keep responses concise but informative.";

                var deepSeekRequest = new
                {
                    model = "deepseek-chat",
                    messages = new[]
                    {
                new
                {
                    role = "system",
                    content = systemPrompt
                },
                new
                {
                    role = "user",
                    content = request.Message
                }
            },
                    max_tokens = 500,
                    temperature = 0.7,
                    top_p = 0.9,
                    stream = false
                };

                var jsonContent = JsonSerializer.Serialize(deepSeekRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var response = await _httpClient.PostAsync("https://api.deepseek.com/chat/completions", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"DeepSeek API error: {response.StatusCode} - {errorContent}");

                    return StatusCode(500, new
                    {
                        error = "Failed to get response from DeepSeek API",
                        details = errorContent,
                        statusCode = response.StatusCode
                    });
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<DeepSeekApiResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (apiResponse?.Choices == null || !apiResponse.Choices.Any())
                {
                    return StatusCode(500, new { error = "No response received from DeepSeek API" });
                }

                var result = new SimpleChatResponse
                {
                    Response = apiResponse.Choices[0].Message.Content,
                    Timestamp = DateTime.UtcNow
                };

                return Ok(result);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed while calling DeepSeek API");
                return StatusCode(500, new { error = "Network error occurred", details = ex.Message });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse DeepSeek API response");
                return StatusCode(500, new { error = "Invalid response format from DeepSeek API", details = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while processing DeepSeek request");
                return StatusCode(500, new { error = "An unexpected error occurred", details = ex.Message });
            }
        }


        [HttpGet("check-api-status")]
        public async Task<ActionResult<object>> CheckApiStatus()
        {
            try
            {
                var apiKey = _configuration["DeepSeek:ApiKey"];

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return Ok(new
                    {
                        status = "error",
                        message = "API key not configured",
                        suggestion = "Please add a valid DeepSeek API key to appsettings.json",
                        timestamp = DateTime.UtcNow
                    });
                }

                if (apiKey == "your-deepseek-api-key-here" || apiKey.Contains("your-"))
                {
                    return Ok(new
                    {
                        status = "error",
                        message = "Default API key detected",
                        suggestion = "Please replace with your actual DeepSeek API key",
                        timestamp = DateTime.UtcNow
                    });
                }

                // Basit API test çağrısı
                var testRequest = new
                {
                    model = "deepseek-chat",
                    messages = new[]
                    {
                new
                {
                    role = "user",
                    content = "Hello"
                }
            },
                    max_tokens = 10
                };

                var jsonContent = JsonSerializer.Serialize(testRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var response = await _httpClient.PostAsync("https://api.deepseek.com/chat/completions", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                switch (response.StatusCode)
                {
                    case System.Net.HttpStatusCode.OK:
                        return Ok(new
                        {
                            status = "success",
                            message = "API key is valid and working",
                            timestamp = DateTime.UtcNow
                        });

                    case System.Net.HttpStatusCode.Unauthorized:
                        return Ok(new
                        {
                            status = "error",
                            message = "Invalid API key",
                            suggestion = "Please check your DeepSeek API key",
                            details = responseContent,
                            timestamp = DateTime.UtcNow
                        });

                    case System.Net.HttpStatusCode.PaymentRequired:
                        return Ok(new
                        {
                            status = "error",
                            message = "Insufficient balance",
                            suggestion = "Please add credits to your DeepSeek account at https://platform.deepseek.com",
                            details = responseContent,
                            timestamp = DateTime.UtcNow
                        });

                    case System.Net.HttpStatusCode.TooManyRequests:
                        return Ok(new
                        {
                            status = "error",
                            message = "Rate limit exceeded",
                            suggestion = "Please wait before making more requests",
                            details = responseContent,
                            timestamp = DateTime.UtcNow
                        });

                    default:
                        return Ok(new
                        {
                            status = "error",
                            message = $"API returned {response.StatusCode}",
                            details = responseContent,
                            timestamp = DateTime.UtcNow
                        });
                }
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    status = "error",
                    message = "Failed to check API status",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        // GET: api/DeepSeek/health
        [HttpGet("health")]
        public async Task<ActionResult<object>> CheckHealth()
        {
            try
            {
                var apiKey = _configuration["DeepSeek:ApiKey"];
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return Ok(new
                    {
                        status = "error",
                        message = "DeepSeek API key not configured",
                        timestamp = DateTime.UtcNow
                    });
                }

                // Simple test request
                var testRequest = new DeepSeekRequest
                {
                    Message = "Hello, can you respond with 'API is working'?",
                    MaxTokens = 50,
                    Temperature = 0.1
                };

                var result = await Chat(testRequest);

                if (result.Result is OkObjectResult)
                {
                    return Ok(new
                    {
                        status = "healthy",
                        message = "DeepSeek API is accessible",
                        timestamp = DateTime.UtcNow
                    });
                }
                else
                {
                    return Ok(new
                    {
                        status = "error",
                        message = "DeepSeek API test failed",
                        timestamp = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    status = "error",
                    message = "DeepSeek API health check failed",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }
    }


    public class SimpleChatRequest
    {
        public string Message { get; set; }
    }

    public class SimpleChatResponse
    {
        public string Response { get; set; }
        public DateTime Timestamp { get; set; }
    }
    // DTO Models
    public class DeepSeekRequest
    {
        public string Message { get; set; }
        public string? SystemPrompt { get; set; }
        public string? Model { get; set; }
        public int? MaxTokens { get; set; }
        public double? Temperature { get; set; }
        public double? TopP { get; set; }
    }

    public class DeepSeekResponse
    {
        public string Message { get; set; }
        public string Model { get; set; }
        public UsageInfo Usage { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class TextAnalysisRequest
    {
        public string Text { get; set; }
        public string AnalysisType { get; set; } // sentiment, summary, keywords, translation, grammar
        public string? TargetLanguage { get; set; } // for translation
    }

    public class CodeReviewRequest
    {
        public string Code { get; set; }
        public string? Language { get; set; }
    }

    public class UsageInfo
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }

    // Internal API Response Models
    internal class DeepSeekApiResponse
    {
        public string Id { get; set; }
        public string Object { get; set; }
        public long Created { get; set; }
        public string Model { get; set; }
        public Choice[] Choices { get; set; }
        public Usage Usage { get; set; }
    }

    internal class Choice
    {
        public int Index { get; set; }
        public Message Message { get; set; }
        public string FinishReason { get; set; }
    }

    internal class Message
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }

    internal class Usage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }
}
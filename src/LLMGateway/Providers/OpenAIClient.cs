using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace PairAdmin.LLMGateway.Providers;

/// <summary>
/// HTTP client for OpenAI API
/// </summary>
public class OpenAIClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private string? _apiKey;
    private string? _organizationId;
    private string _baseUrl;

    /// <summary>
    /// Default base URL
    /// </summary>
    private const string DefaultBaseUrl = "https://api.openai.com/v1";

    /// <summary>
    /// Completions endpoint
    /// </summary>
    private const string CompletionsEndpoint = "/chat/completions";

    /// <summary>
    /// Initializes a new instance of OpenAIClient
    /// </summary>
    public OpenAIClient(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _baseUrl = DefaultBaseUrl;

        _logger.LogInformation("OpenAIClient initialized");
    }

    /// <summary>
    /// Configures the client with API key
    /// </summary>
    /// <param name="apiKey">OpenAI API key</param>
    /// <param name="organizationId">Optional organization ID</param>
    /// <param name="baseUrl">Optional custom base URL</param>
    public void Configure(string apiKey, string? organizationId = null, string? baseUrl = null)
    {
        _apiKey = apiKey;
        _organizationId = organizationId;

        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            _baseUrl = baseUrl;
        }

        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            if (!string.IsNullOrWhiteSpace(_organizationId))
            {
                _httpClient.DefaultRequestHeaders.Add("OpenAI-Organization", _organizationId);
            }
        }
        else
        {
            _httpClient.DefaultRequestHeaders.Clear();
        }

        _logger.LogInformation("OpenAIClient configured");
    }

    /// <summary>
    /// Tests connection to OpenAI API
    /// </summary>
    /// <returns>True if connection is successful</returns>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                _logger.LogWarning("Cannot test connection: no API key configured");
                return false;
            }

            var testRequest = new OpenAICompletionRequest
            {
                Model = "gpt-3.5-turbo",
                Messages = new List<OpenAIMessage>
                {
                    new OpenAIMessage { Role = "user", Content = "test" }
                },
                MaxTokens = 5,
                Stream = false
            };

            var response = await SendRequestAsync(testRequest);

            return response != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed");
            return false;
        }
    }

    /// <summary>
    /// Sends a completion request to OpenAI API
    /// </summary>
    /// <param name="request">Completion request</param>
    /// <returns>OpenAI response or null on error</returns>
    public async Task<OpenAICompletionResponse?> SendRequestAsync(OpenAICompletionRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new LLMGatewayException("API key is not configured");
            }

            var jsonRequest = JsonSerializer.Serialize(request);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var url = _baseUrl + CompletionsEndpoint;
            _logger.LogTrace($"Sending request to {url}");

            var response = await _httpClient.PostAsync(url, content);

            var responseString = await response.Content.ReadAsStringAsync();

            _logger.LogTrace($"Response status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                return HandleErrorResponse(response.StatusCode, responseString);
            }

            var jsonResponse = JsonSerializer.Deserialize<OpenAICompletionResponse>(responseString);

            if (jsonResponse?.Usage != null)
            {
                _logger.LogTrace($"Tokens used: {jsonResponse.Usage.TotalTokens}");
            }

            return jsonResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send request");
            throw new LLMGatewayException("Failed to send request to OpenAI", ex);
        }
    }

    /// <summary>
    /// Sends a streaming completion request to OpenAI API
    /// </summary>
    /// <param name="request">Completion request</param>
    /// <returns>Async enumerable of stream chunks</returns>
    public async IAsyncEnumerable<OpenAIStreamChunk> SendStreamingRequestAsync(
        OpenAICompletionRequest request)
    {
        request.Stream = true;

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new LLMGatewayException("API key is not configured");
        }

        var chunks = new List<OpenAIStreamChunk>();
        Exception? capturedException = null;
        bool shouldBreak = false;

        try
        {
            var jsonRequest = JsonSerializer.Serialize(request);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var url = _baseUrl + CompletionsEndpoint;
            _logger.LogTrace($"Sending streaming request to {url}");

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                HandleErrorResponse(response.StatusCode, responseString);
                shouldBreak = true;
            }
            else
            {
                await foreach (var chunk in ParseStreamAsync(response))
                {
                    chunks.Add(chunk);
                }

                _logger.LogTrace("Streaming completed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send streaming request");
            capturedException = new LLMGatewayException("Failed to send streaming request to OpenAI", ex);
        }

        // Yield outside try-catch
        foreach (var chunk in chunks)
        {
            yield return chunk;
        }

        if (capturedException != null)
        {
            throw capturedException;
        }
    }

    /// <summary>
    /// Parses Server-Sent Events (SSE) stream
    /// </summary>
    private async IAsyncEnumerable<OpenAIStreamChunk> ParseStreamAsync(HttpResponseMessage response)
    {
        var chunks = new List<OpenAIStreamChunk>();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.StartsWith("data: "))
            {
                var data = line.Substring(6);

                if (data == "[DONE]")
                {
                    chunks.Add(new OpenAIStreamChunk { IsDone = true });
                    break;
                }

                try
                {
                    var chunk = JsonSerializer.Deserialize<OpenAIStreamChunk>(data);
                    if (chunk != null)
                    {
                        chunks.Add(chunk);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, $"Failed to parse stream chunk: {data}");
                }
            }
        }

        // Yield outside the using blocks
        foreach (var chunk in chunks)
        {
            yield return chunk;
        }
    }

    /// <summary>
    /// Handles error response from OpenAI API
    /// </summary>
    private OpenAICompletionResponse? HandleErrorResponse(System.Net.HttpStatusCode statusCode, string responseString)
    {
        var errorResponse = JsonSerializer.Deserialize<OpenAIErrorResponse>(responseString);
        var errorMessage = errorResponse?.Error?.Message ?? "Unknown error";

        _logger.LogError($"OpenAI API error ({(int)statusCode}): {errorMessage}");

        var exception = statusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => new LLMAuthenticationException("openai", errorMessage),
            System.Net.HttpStatusCode.TooManyRequests => new LLMRateLimitException("openai", errorMessage),
            System.Net.HttpStatusCode.BadRequest => new LLMInvalidRequestException("openai", errorMessage),
            _ => new LLMProviderException("openai", errorMessage)
        };

        exception.HttpStatusCode = (int)statusCode;
        exception.ErrorCode = errorResponse?.Error?.Code;

        throw exception;
    }

    /// <summary>
    /// Disposes of resources
    /// </summary>
    public void Dispose()
    {
        _httpClient.Dispose();
        _logger.LogInformation("OpenAIClient disposed");
    }
}

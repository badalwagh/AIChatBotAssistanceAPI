using Newtonsoft.Json;
using System.Text;

namespace AIChatBotAPI.Service
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _model;

        public GeminiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Gemini:ApiKey"]
                ?? throw new InvalidOperationException("Gemini API key not configured.");
            _model = configuration["Gemini:Model"] ?? "gemini-1.5-flash";
        }

        public async Task<string> GetResponse(string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
                return "Please enter a message.";

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = "\nUser: " + userMessage }
                        }
                    }
                }
            };

            var json = JsonConvert.SerializeObject(requestBody);

            int maxRetries = 3;
            int[] delaysMs = { 1000, 2000, 4000 }; // 1s, 2s, 4s

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync(url, content);
                    var result = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        dynamic? data = JsonConvert.DeserializeObject(result);
                        string? reply = data?.candidates?[0]?.content?.parts?[0]?.text;
                        return reply ?? "Sorry, I couldn't understand.";
                    }

                    int statusCode = (int)response.StatusCode;
                    bool shouldRetry = (statusCode == 503 || statusCode == 429)
                                        && attempt < maxRetries - 1;

                    if (shouldRetry)
                    {
                        Console.WriteLine($"Gemini {statusCode} on attempt {attempt + 1}, retrying in {delaysMs[attempt]}ms...");
                        await Task.Delay(delaysMs[attempt]);
                        continue;
                    }

                    Console.WriteLine($"Gemini API error {response.StatusCode}: {result}");

                    if (statusCode == 503)
                        return "The AI service is busy right now. Please try again in a moment.";
                    if (statusCode == 429)
                        return "Too many requests. Please wait a moment and try again.";

                    return "Sorry, the AI service returned an error. Please try again.";
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception on attempt {attempt + 1}: {ex.Message}");
                    if (attempt == maxRetries - 1)
                        return "Something went wrong while contacting the AI service.";
                    await Task.Delay(delaysMs[attempt]);
                }
            }

            return "The AI service is unavailable. Please try again later.";
        }

        private string GetSystemPrompt()
        {
            return @"
                You are a helpful assistant for Malta Education services.
                Application Process:
                1. Login
                2. Select service
                3. Fill details
                4. Upload documents
                5. Select hours
                6. Submit
                Explain simply.
            ";
        }
    }
}
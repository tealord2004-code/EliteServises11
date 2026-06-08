using System.Text.Json;
using System.Text.Json.Serialization;

namespace EliteJobs.App.Services
{
    public class RecaptchaService
    {
        private readonly HttpClient _httpClient;
        private readonly string _secretKey;

        public RecaptchaService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _secretKey = Environment.GetEnvironmentVariable("RECAPTCHA_SECRET_KEY")
                ?? configuration["Recaptcha:SecretKey"]
                ?? "";
        }

        public async Task<bool> VerifyAsync(string token)
        {
            if (string.IsNullOrEmpty(_secretKey) || string.IsNullOrEmpty(token))
            {
                Console.WriteLine($"reCAPTCHA: Skipping - key empty: {string.IsNullOrEmpty(_secretKey)}, token empty: {string.IsNullOrEmpty(token)}");
                return true;
            }

            try
            {
                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["secret"] = _secretKey,
                    ["response"] = token
                });

                var response = await _httpClient.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);
                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"reCAPTCHA raw: {json}");

                var result = JsonSerializer.Deserialize<RecaptchaResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result == null)
                {
                    Console.WriteLine("reCAPTCHA: Deserialization returned null");
                    return false;
                }

                Console.WriteLine($"reCAPTCHA: Success={result.Success}, Score={result.Score}, Action={result.Action}");
                bool passed = result.Success && result.Score >= 0.3;
                Console.WriteLine($"reCAPTCHA: Passed={passed}");
                return passed;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"reCAPTCHA error: {ex.Message}");
                return true;
            }
        }
    }

    public class RecaptchaResponse
    {
        public bool Success { get; set; }
        public double Score { get; set; }
        public string Action { get; set; } = string.Empty;
    }
}
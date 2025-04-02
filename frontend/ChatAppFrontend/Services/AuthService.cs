using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ChatAppFrontend.Models;
using ChatAppFrontend.Services;

namespace ChatAppFrontend
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "http://localhost:8080/api/auth/";

        public AuthService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            var loginData = new { username, password };

            var content = new StringContent(
                JsonSerializer.Serialize(loginData),
                Encoding.UTF8,
                "application/json");

            try
            {
                var response = await _httpClient.PostAsync($"{BaseUrl}login", content);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var loginResponse = JsonSerializer.Deserialize<LoginResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (loginResponse != null)
                    {
                        SessionManager.Token = loginResponse.Token;
                        SessionManager.Username = loginResponse.Username;
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur login : {ex.Message}");
                return false;
            }
        }


        public async Task<bool> RegisterAsync(string username, string email, string password)
        {
            var registerData = new { username, email, password };

            var content = new StringContent(
                JsonSerializer.Serialize(registerData),
                Encoding.UTF8,
                "application/json");

            try
            {
                var response = await _httpClient.PostAsync($"{BaseUrl}register", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur register : {ex.Message}");
                return false;
            }
        }
    }
}

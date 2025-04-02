using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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
            var loginData = new
            {
                username,
                password
            };

            var content = new StringContent(
                JsonSerializer.Serialize(loginData),
                Encoding.UTF8,
                "application/json");

            try
            {
                var response = await _httpClient.PostAsync($"{BaseUrl}login", content);
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Erreur de requête HTTP (login) : {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur inattendue (login) : {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RegisterAsync(string username, string email, string password)
        {
            var registerData = new
            {
                username,
                email,
                password
            };

            var content = new StringContent(
                JsonSerializer.Serialize(registerData),
                Encoding.UTF8,
                "application/json");

            try
            {
                var response = await _httpClient.PostAsync($"{BaseUrl}register", content);
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Erreur de requête HTTP (register) : {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur inattendue (register) : {ex.Message}");
                return false;
            }
        }
    }
}

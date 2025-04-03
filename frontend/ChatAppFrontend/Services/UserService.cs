using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using ChatAppFrontend.ViewModel;

namespace ChatAppFrontend.ViewModel
{
    public class User
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
    }
}

namespace ChatAppFrontend.Services
{
    public class UserService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "http://localhost:8080/api/users";

        public UserService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<List<User>> GetUsersAsync()
        {
            try
            {
                var token = SessionManager.Token;
                if (string.IsNullOrEmpty(token))
                    return new List<User>();

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.GetAsync(BaseUrl);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var users = JsonSerializer.Deserialize<List<User>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return users ?? new List<User>();
                }
                
                return new List<User>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur de récupération des utilisateurs : {ex.Message}");
                return new List<User>();
            }
        }
    }
} 
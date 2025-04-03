using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ChatAppFrontend.Models;
using ChatAppFrontend.Services;

namespace ChatAppFrontend.Services
{
    public class ChannelService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "http://localhost:8080/api/channels";

        public ChannelService()
        {
            _httpClient = new HttpClient();
        }

        private void AddAuthorizationHeader()
        {
            if (!string.IsNullOrEmpty(SessionManager.Token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SessionManager.Token);
            }
        }

        public async Task<List<Room>> GetPublicChannelsAsync()
        {
            AddAuthorizationHeader();
            try
            {
                var response = await _httpClient.GetAsync(BaseUrl);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var channels = JsonSerializer.Deserialize<List<Room>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return channels;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur GetPublicChannelsAsync : {ex.Message}");
            }

            return new List<Room>();
        }

        public async Task<Room> CreateChannelAsync(string name, string description)
        {
            AddAuthorizationHeader();
            var payload = new { name, description };
            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            try
            {
                var response = await _httpClient.PostAsync(BaseUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var channel = JsonSerializer.Deserialize<Room>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return channel;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur CreateChannelAsync : {ex.Message}");
            }

            return null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ChatAppFrontend.ViewModel;

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

        public async Task<List<Channel>> GetPublicChannelsAsync()
        {
            try
            {
                var token = SessionManager.Token;
                if (string.IsNullOrEmpty(token))
                    return new List<Channel>();

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.GetAsync(BaseUrl);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var channels = JsonSerializer.Deserialize<List<Channel>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return channels ?? new List<Channel>();
                }
                
                return new List<Channel>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur de récupération des salons : {ex.Message}");
                return new List<Channel>();
            }
        }
        
        public async Task<Channel> CreateChannelAsync(string name, string description)
        {
            try
            {
                var token = SessionManager.Token;
                if (string.IsNullOrEmpty(token))
                    return null;

                var channelData = new
                {
                    name = name,
                    description = description,
                    creatorUsername = SessionManager.Username
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(channelData),
                    Encoding.UTF8,
                    "application/json");

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.PostAsync(BaseUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var channel = JsonSerializer.Deserialize<Channel>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return channel;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur de création de salon : {ex.Message}");
                return null;
            }
        }
        
        public async Task<bool> DeleteChannelAsync(int channelId)
        {
            try
            {
                var token = SessionManager.Token;
                if (string.IsNullOrEmpty(token))
                    return false;

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.DeleteAsync($"{BaseUrl}/{channelId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur de suppression de salon : {ex.Message}");
                return false;
            }
        }
    }
}

using ChatAppFrontend.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ChatAppFrontend.Services
{
    public class MessageService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "http://localhost:8080/api/messages"; // Exemple

        public MessageService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<List<Message>> GetMessagesForRoomAsync(int roomId)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/room/{roomId}");

                // 🔐 Ajout du token JWT dans le header
                if (!string.IsNullOrEmpty(SessionManager.Token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", SessionManager.Token);
                }

                Console.WriteLine($"[DEBUG] Appel GET : {request.RequestUri}");

                var response = await _httpClient.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"[DEBUG] StatusCode = {(int)response.StatusCode}");
                Console.WriteLine($"[DEBUG] Réponse brute : {json}");

                if (response.IsSuccessStatusCode)
                {
                    var messages = JsonSerializer.Deserialize<List<Message>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return messages ?? new List<Message>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur GetMessagesForRoomAsync : {ex.Message}");
            }

            return new List<Message>();
        }

        public async Task<bool> SendMessageAsync(int roomId, string content)
{
    try
    {
        var payload = new
        {
            RoomId = roomId,
            Content = content
        };

        var jsonPayload = JsonSerializer.Serialize(payload);
        Console.WriteLine($"[DEBUG] Message JSON envoyé : {jsonPayload}");

        var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl)
        {
            Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrEmpty(SessionManager.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", SessionManager.Token);
        }

        var response = await _httpClient.SendAsync(request);
        Console.WriteLine($"[DEBUG] Envoi message - StatusCode : {(int)response.StatusCode}");

        return response.IsSuccessStatusCode;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erreur SendMessageAsync : {ex.Message}");
        return false;
    }
}

    }
}

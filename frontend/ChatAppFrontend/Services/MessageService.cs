using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ChatAppFrontend.ViewModel;
using System.Threading;
using System.Net.WebSockets;
using StompNet;

namespace ChatAppFrontend.Services
{
    public class MessageService
    {
        private readonly HttpClient _httpClient;
        private string BaseUrl;
        private const int MaxRetries = 3;
        private StompClient _stompClient;
        private bool _isStompConnected = false;

        // Événement pour notifier quand un nouveau message est reçu
        public event EventHandler<Message>? MessageReceived;

        public MessageService()
        {
            try
            {
                _httpClient = new HttpClient();
                _httpClient.Timeout = TimeSpan.FromSeconds(10); 
                BaseUrl = "http://localhost:8080/api/messages";
                
                // Initialiser le client STOMP
                _stompClient = new StompClient();
                InitializeStompConnection();
                
                // Afficher des informations de débogage sur l'API
                Task.Run(async () => await TestAPIEndpoints());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la création du service: {ex.Message}");
                _httpClient = new HttpClient();
                BaseUrl = "http://localhost:8080/api/messages";
            }
        }
        
        // Surcharge pour les tests ou l'injection de dépendances
        public MessageService(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            BaseUrl = "http://localhost:8080/api/messages";
            
            // Initialiser le client STOMP
            _stompClient = new StompClient();
            InitializeStompConnection();
            
            // Afficher des informations de débogage sur l'API
            Task.Run(async () => await TestAPIEndpoints());
        }
        
        private async void InitializeStompConnection()
        {
            try
            {
                // Ajouter des headers supplémentaires si nécessaire
                Dictionary<string, string> headers = new Dictionary<string, string>();
                
                // Ajouter le token d'authentification si disponible
                if (!string.IsNullOrEmpty(SessionManager.Token))
                {
                    headers.Add("Authorization", $"Bearer {SessionManager.Token}");
                }
                
                // L'URL WebSocket AVEC le suffixe /websocket requis par SockJS
                string wsUrl = "ws://localhost:8080/ws/websocket";
                Console.WriteLine($"Tentative de connexion STOMP à {wsUrl} avec protocole v10.stomp");
                
                try
                {
                    await _stompClient.ConnectAsync(wsUrl, null, null, headers);
                    _isStompConnected = true;
                    
                    // S'abonner aux messages privés - Correspond au format utilisé dans le backend
                    await _stompClient.SubscribeAsync("/user/queue/messages", HandlePrivateMessage);
                    
                    // S'abonner aux messages publics
                    await _stompClient.SubscribeAsync("/topic/public", HandlePublicMessage);
                    
                    Console.WriteLine("Client STOMP connecté et abonné avec succès!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Échec de la connexion STOMP à {wsUrl}: {ex.Message}");
                    // Supprimer la logique de fallback car elle essayait la mauvaise URL
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Cause: {ex.InnerException.Message}");
                    }
                    _isStompConnected = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'initialisation du client STOMP: {ex.Message}");
                if (ex.InnerException != null) 
                {
                    Console.WriteLine($"Cause: {ex.InnerException.Message}");
                }
                _isStompConnected = false;
            }
        }
        
        private void HandlePrivateMessage(string messageJson)
        {
            Console.WriteLine($"Message privé reçu: {messageJson}");
            
            try
            {
                // Désérialiser le message JSON reçu
                var message = JsonSerializer.Deserialize<Message>(messageJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                if (message != null)
                {
                    // Notifier que le message a été reçu
                    MessageReceived?.Invoke(this, message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors du traitement du message privé: {ex.Message}");
            }
        }
        
        private void HandlePublicMessage(string messageJson)
        {
            Console.WriteLine($"Message public reçu: {messageJson}");
            
            try
            {
                // Désérialiser le message JSON reçu
                var message = JsonSerializer.Deserialize<Message>(messageJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                if (message != null)
                {
                    // Notifier que le message a été reçu
                    MessageReceived?.Invoke(this, message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors du traitement du message public: {ex.Message}");
            }
        }
        
        public async Task<bool> SendMessageAsync(int? channelId, string content, string? recipientUsername = null)
        {
            // Validations de base pour éviter des erreurs
            if (string.IsNullOrEmpty(content))
                return false;
                
            // Débuggage
            Console.WriteLine($"SendMessageAsync - ChannelId: {channelId}, Content: {content}, Recipient: {recipientUsername}");
            
            // Vérification simple du type de message
            string messageType = "public";
            if (channelId.HasValue)
                messageType = "channel";
            else if (!string.IsNullOrEmpty(recipientUsername))
                messageType = "private";
                
            Console.WriteLine($"Type de message: {messageType}");
            
            // Essayer d'envoyer via STOMP d'abord
            if (_isStompConnected)
            {
                try
                {
                    // Format exact attendu par le backend (ChatMessagePayload dans ChatController.java)
                    var chatMessage = new
                    {
                        content = content,
                        recipientUsername = recipientUsername,  // null pour les messages publics ou de canal
                        channelId = channelId                   // null pour les messages privés ou publics
                    };
                    
                    var messageJson = JsonSerializer.Serialize(chatMessage);
                    
                    // Destination STOMP standard définie dans le backend
                    Console.WriteLine($"Envoi STOMP à /app/chat.sendMessage: {messageJson}");
                    await _stompClient.SendAsync("/app/chat.sendMessage", messageJson);
                    Console.WriteLine("Message envoyé avec succès via STOMP");
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors de l'envoi via STOMP: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Cause: {ex.InnerException.Message}");
                    }
                }
            }
            else
            {
                Console.WriteLine("Échec de l'envoi: le client STOMP n'est pas connecté, et il semble que ce serveur nécessite STOMP pour envoyer des messages.");
                return false;
            }
            
            // Si l'envoi STOMP a échoué, essayer les méthodes HTTP REST
            Console.WriteLine("Tentative de fallback vers les méthodes HTTP REST (bien que le serveur semble nécessiter STOMP)");
            
            // Essayer les méthodes HTTP REST comme fallback
            for (int retry = 0; retry < MaxRetries; retry++)
            {
                try
                {
                    // Nettoyer tous les headers potentiellement problématiques
                    _httpClient.DefaultRequestHeaders.Clear();
                    
                    // Ajouter l'en-tête d'autorisation
                    if (!string.IsNullOrEmpty(SessionManager.Token))
                    {
                        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SessionManager.Token);
                    }
                    
                    // Créer un payload adapté au type de message
                    object payload;
                    string endpoint;
                    
                    if (messageType == "private")
                    {
                        // Endpoint REST pour messages privés - UNIQUEMENT les endpoints REST, pas STOMP
                        endpoint = $"http://localhost:8080/api/messages/private/{recipientUsername}";
                        payload = new { content = content };
                        
                        var success = await SendRequestToEndpoint(endpoint, payload);
                        if (success) return true;
                        
                        // Si l'endpoint ci-dessus ne fonctionne pas, essayer les autres endpoints REST
                        endpoint = $"http://localhost:8080/api/messages/user/{recipientUsername}";
                        success = await SendRequestToEndpoint(endpoint, payload);
                        if (success) return true;
                        
                        endpoint = $"{BaseUrl}/private/{recipientUsername}";
                        success = await SendRequestToEndpoint(endpoint, payload);
                        if (success) return true;
                        
                        // Essayer avec un format alternatif
                        endpoint = $"http://localhost:8080/api/messages/direct-message";
                        var altPayload = new { content = content, recipient = recipientUsername };
                        success = await SendRequestToEndpoint(endpoint, altPayload);
                        
                        if (!success) {
                            Console.WriteLine("Échec de l'envoi par HTTP REST. L'envoi de messages privés nécessite probablement le WebSocket STOMP.");
                        }
                        
                        return success;
                    }
                    else if (messageType == "channel")
                    {
                        // Endpoint REST pour messages de canal
                        endpoint = $"http://localhost:8080/api/messages/channel/{channelId}";
                        payload = new { content = content };
                        
                        var success = await SendRequestToEndpoint(endpoint, payload);
                        if (success) return true;
                        
                        // Autres endpoints REST possibles
                        endpoint = $"{BaseUrl}/channel/{channelId}";
                        success = await SendRequestToEndpoint(endpoint, payload);
                        if (success) return true;
                        
                        endpoint = $"http://localhost:8080/api/channels/{channelId}/messages";
                        success = await SendRequestToEndpoint(endpoint, payload);
                        
                        if (!success) {
                            Console.WriteLine("Échec de l'envoi par HTTP REST. L'envoi de messages au canal nécessite probablement le WebSocket STOMP.");
                        }
                        
                        return success;
                    }
                    else
                    {
                        // Endpoint REST pour messages publics
                        endpoint = "http://localhost:8080/api/messages/public";
                        payload = new { content = content };
                        
                        var success = await SendRequestToEndpoint(endpoint, payload);
                        if (success) return true;
                        
                        // Autre format possible
                        endpoint = $"{BaseUrl}/public";
                        success = await SendRequestToEndpoint(endpoint, payload);
                        
                        if (!success) {
                            Console.WriteLine("Échec de l'envoi par HTTP REST. L'envoi de messages publics nécessite probablement le WebSocket STOMP.");
                        }
                        
                        return success;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception lors de l'envoi (tentative {retry+1}/{MaxRetries}): {ex.Message}");
                    if (retry < MaxRetries - 1)
                    {
                        await Task.Delay(1000);
                    }
                }
            }
            
            return false;
        }
        
        private async Task<bool> TrySendToChannel(int channelId, string content)
        {
            Console.WriteLine($"Tentative d'envoi vers le canal {channelId}");
            
            // Endpoint principal WebSocket utilisé par le frontend JS
            var endpoint = "http://localhost:8080/app/chat.sendMessage";
            var chatPayload = new { content = content, channelId = channelId, recipientUsername = (string)null };
            var success = await SendRequestToEndpoint(endpoint, chatPayload);
            if (success) return true;
            
            // Essai 2: Endpoint /channel/{id} du backend
            endpoint = $"http://localhost:8080/api/messages/channel/{channelId}";
            var payload = new { content = content };
            success = await SendRequestToEndpoint(endpoint, payload);
            if (success) return true;
            
            // Essai 3: Format spécifique au canal
            endpoint = $"{BaseUrl}/channel/{channelId}";
            success = await SendRequestToEndpoint(endpoint, payload);
            if (success) return true;
            
            // Essai 4: Endpoint /channels/{id}/messages
            endpoint = $"http://localhost:8080/api/channels/{channelId}/messages";
            success = await SendRequestToEndpoint(endpoint, payload);
            
            return success;
        }
        
        private async Task<bool> TrySendPrivateMessage(string recipient, string content)
        {
            Console.WriteLine($"Tentative d'envoi de message privé à {recipient}");
            
            // Créer quelques variantes de payload à tester
            var payloads = new List<object>
            {
                // Format simple
                new { content = content },
                
                // Format avec type explicite
                new { content = content, type = "PRIVATE" },
                
                // Format avec destinataire explicite
                new { content = content, recipientUsername = recipient },
                
                // Format avec tous les champs
                new { 
                    content = content, 
                    recipientUsername = recipient, 
                    senderUsername = SessionManager.Username, 
                    type = "PRIVATE" 
                }
            };
            
            // Essai systématique de plusieurs endpoints et formats de payload
            var endpoints = new List<string>
            {
                // Endpoints REST probables
                $"http://localhost:8080/api/messages/user/{recipient}",
                $"http://localhost:8080/api/messages/private/{recipient}",
                $"{BaseUrl}/private/{recipient}",
                $"{BaseUrl}/user/{recipient}",
                $"http://localhost:8080/api/chat/private/{recipient}",
                
                // Endpoint avec format différent du corps
                $"http://localhost:8080/api/messages/direct-message",
                $"http://localhost:8080/api/users/{recipient}/messages"
            };
            
            // Tester chaque endpoint avec chaque format de payload
            foreach (var endpoint in endpoints)
            {
                foreach (var payload in payloads)
                {
                    var success = await SendRequestToEndpoint(endpoint, payload);
                    if (success)
                    {
                        Console.WriteLine($"✅ Succès avec endpoint: {endpoint} et payload: {JsonSerializer.Serialize(payload)}");
                        return true;
                    }
                }
            }
            
            Console.WriteLine("❌ Échec de toutes les tentatives d'envoi de message privé");
            return false;
        }
        
        private async Task<bool> TrySendPublicMessage(string content)
        {
            Console.WriteLine("Tentative d'envoi de message public");
            
            // Endpoint principal WebSocket utilisé par le frontend JS
            var endpoint = "http://localhost:8080/app/chat.sendMessage";
            var chatPayload = new { content = content, channelId = (int?)null, recipientUsername = (string)null };
            var success = await SendRequestToEndpoint(endpoint, chatPayload);
            if (success) return true;
            
            // Essai 2: Endpoint /public du backend
            endpoint = $"http://localhost:8080/api/messages/public";
            var payload = new { content = content };
            success = await SendRequestToEndpoint(endpoint, payload);
            if (success) return true;
            
            // Essai 3: Autre format
            endpoint = $"{BaseUrl}/public";
            success = await SendRequestToEndpoint(endpoint, payload);
            
            return success;
        }
        
        private async Task<bool> TrySendGenericMessage(int? channelId, string? recipientUsername, string content)
        {
            Console.WriteLine("Tentative d'envoi avec le format générique");
            
            // Endpoint WebSocket utilisé par le frontend JS
            var endpoint = "http://localhost:8080/app/chat.sendMessage";
            var payload = new 
            { 
                content = content,
                channelId = channelId,
                recipientUsername = recipientUsername,
                senderUsername = SessionManager.Username
            };
            var success = await SendRequestToEndpoint(endpoint, payload);
            if (success) return true;
            
            // Backup: Endpoint /send générique REST
            endpoint = $"{BaseUrl}/send";
            success = await SendRequestToEndpoint(endpoint, payload);
            
            return success;
        }
        
        private async Task<bool> SendRequestToEndpoint(string endpoint, object payload)
        {
            try
            {
                string json = JsonSerializer.Serialize(payload);
                Console.WriteLine($"Envoi à {endpoint} avec payload: {json}");
                
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync(endpoint, httpContent);
                Console.WriteLine($"Réponse de {endpoint}: {response.StatusCode}");
                
                if (!response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Erreur depuis {endpoint}: {responseContent}");
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception lors de l'envoi à {endpoint}: {ex.Message}");
                return false;
            }
        }
        
        public async Task<bool> DeleteMessageAsync(int channelId, int messageId)
        {
            for (int retry = 0; retry < MaxRetries; retry++)
            {
                try
                {
                    AddAuthorizationHeader();
                    // Utiliser le même endpoint que le frontend
                    var response = await _httpClient.DeleteAsync($"http://localhost:8080/api/messages/channel/{channelId}/messages/{messageId}");
                    if (!response.IsSuccessStatusCode)
                    {
                        // Essayer avec l'ancien format
                        response = await _httpClient.DeleteAsync($"{BaseUrl}/channel/{channelId}/messages/{messageId}");
                    }
                    return response.IsSuccessStatusCode;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur DeleteMessageAsync (tentative {retry+1}/{MaxRetries}): {ex.Message}");
                    if (retry == MaxRetries - 1) break;
                    await Task.Delay(1000);
                }
            }
            
            return false;
        }

        private async Task TestAPIEndpoints()
        {
            try
            {
                Console.WriteLine("Test des endpoints de l'API:");
                
                // Nettoyer tous les headers potentiellement problématiques
                _httpClient.DefaultRequestHeaders.Clear();
                
                // Ajouter l'en-tête d'autorisation si disponible
                if (!string.IsNullOrEmpty(SessionManager.Token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SessionManager.Token);
                }
                
                // Liste des endpoints à tester
                var endpointsToTest = new List<string>
                {
                    // API de base
                    "http://localhost:8080/api",
                    
                    // Endpoints messages utilisés par le frontend
                    "http://localhost:8080/api/messages",
                    "http://localhost:8080/api/messages/public",
                    
                    // Endpoints utilisateurs
                    "http://localhost:8080/api/users",
                    
                    // Endpoints salons
                    "http://localhost:8080/api/channels",
                    
                    // Authentication
                    "http://localhost:8080/api/auth/me"
                };
                
                foreach (var endpoint in endpointsToTest)
                {
                    try
                    {
                        Console.WriteLine($"Test de l'endpoint: {endpoint}");
                        var response = await _httpClient.GetAsync(endpoint);
                        Console.WriteLine($"Réponse: {response.StatusCode}");
                        
                        if (response.IsSuccessStatusCode)
                        {
                            var content = await response.Content.ReadAsStringAsync();
                            Console.WriteLine($"Contenu: {content.Substring(0, Math.Min(content.Length, 100))}...");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erreur lors du test de {endpoint}: {ex.Message}");
                    }
                }
                
                // Tester l'utilisateur actuel
                try
                {
                    Console.WriteLine("Informations sur l'utilisateur actuel:");
                    Console.WriteLine($"Username: {SessionManager.Username}");
                    Console.WriteLine($"Token présent: {!string.IsNullOrEmpty(SessionManager.Token)}");
                    if (!string.IsNullOrEmpty(SessionManager.Token))
                    {
                        Console.WriteLine($"Token (premiers caractères): {SessionManager.Token.Substring(0, Math.Min(SessionManager.Token.Length, 20))}...");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors de la récupération des informations utilisateur: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur globale lors du test des endpoints: {ex.Message}");
            }
        }

        private void AddAuthorizationHeader()
        {
            try
            {
                // Nettoyer d'abord tous les headers d'authentification existants
                if (_httpClient.DefaultRequestHeaders.Contains("Authorization"))
                {
                    _httpClient.DefaultRequestHeaders.Remove("Authorization");
                }
                
                var token = SessionManager.Token;
                if (string.IsNullOrEmpty(token))
                {
                    Console.WriteLine("Aucun token d'authentification disponible");
                    return;
                }

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                Console.WriteLine("En-tête d'authentification ajouté: Bearer " + token.Substring(0, Math.Min(token.Length, 10)) + "...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'ajout de l'en-tête d'authentification: {ex.Message}");
            }
        }
        
        public async Task<List<Message>> GetMessagesForRoomAsync(int roomId)
        {
            for (int retry = 0; retry < MaxRetries; retry++)
            {
                try
                {
                    AddAuthorizationHeader();
                    // Utiliser le même endpoint que le frontend
                    var response = await _httpClient.GetAsync($"http://localhost:8080/api/messages/channel/{roomId}");
                    if (!response.IsSuccessStatusCode)
                    {
                        // Fallback au format ancien
                        response = await _httpClient.GetAsync($"{BaseUrl}/channel/{roomId}");
                    }
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var messages = JsonSerializer.Deserialize<List<Message>>(json, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        return messages ?? new List<Message>();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur GetMessagesForRoomAsync (tentative {retry+1}/{MaxRetries}): {ex.Message}");
                    if (retry == MaxRetries - 1) break;
                    await Task.Delay(1000); // Attendre avant de réessayer
                }
            }

            return new List<Message>();
        }
        
        public async Task<List<Message>> GetMessagesForUserAsync(string username)
        {
            for (int retry = 0; retry < MaxRetries; retry++)
            {
                try
                {
                    AddAuthorizationHeader();
                    // Utiliser le même endpoint que le frontend
                    var response = await _httpClient.GetAsync($"http://localhost:8080/api/messages/user/{username}");
                    if (!response.IsSuccessStatusCode)
                    {
                        // Fallback au format ancien
                        response = await _httpClient.GetAsync($"{BaseUrl}/user/{username}");
                    }
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var messages = JsonSerializer.Deserialize<List<Message>>(json, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        return messages ?? new List<Message>();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur GetMessagesForUserAsync (tentative {retry+1}/{MaxRetries}): {ex.Message}");
                    if (retry == MaxRetries - 1) break;
                    await Task.Delay(1000);
                }
            }

            return new List<Message>();
        }

        public async Task<List<Message>> GetPublicMessagesAsync()
        {
            for (int retry = 0; retry < MaxRetries; retry++)
            {
                try
                {
                    AddAuthorizationHeader();
                    // Utiliser le même endpoint que le frontend
                    var response = await _httpClient.GetAsync($"http://localhost:8080/api/messages/public");
                    if (!response.IsSuccessStatusCode)
                    {
                        // Fallback au format ancien
                        response = await _httpClient.GetAsync($"{BaseUrl}/public");
                    }
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var messages = JsonSerializer.Deserialize<List<Message>>(json, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        return messages ?? new List<Message>();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur GetPublicMessagesAsync (tentative {retry+1}/{MaxRetries}): {ex.Message}");
                    if (retry == MaxRetries - 1) break;
                    await Task.Delay(1000);
                }
            }

            return new List<Message>();
        }

        /// <summary>
        /// Détecte l'URL et la configuration correctes du serveur WebSocket
        /// </summary>
        public async Task DetectServerConfiguration()
        {
            Console.WriteLine("Détection de la configuration du serveur WebSocket...");
            
            // Liste des URL WebSocket potentielles
            var wsUrls = new List<string>
            {
                "ws://localhost:8080/ws",
                "ws://localhost:8080/websocket",
                "ws://localhost:8080/ws/websocket",
                "ws://localhost:8080/stomp",
                "ws://localhost:8080/socket",
                "ws://localhost:8080/messaging",
                "ws://localhost:8080/app"
            };
            
            // Liste des URL API potentielles
            var apiUrls = new List<string>
            {
                "http://localhost:8080/api",
                "http://localhost:8080",
                "http://localhost:8080/api/v1"
            };
            
            // Tester les endpoints API pour trouver la base correcte
            Console.WriteLine("Test des URLs API de base:");
            foreach (var apiUrl in apiUrls)
            {
                try
                {
                    // Nettoyer les headers
                    _httpClient.DefaultRequestHeaders.Clear();
                    if (!string.IsNullOrEmpty(SessionManager.Token))
                    {
                        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SessionManager.Token);
                    }
                    
                    var response = await _httpClient.GetAsync($"{apiUrl}/health");
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"✅ URL API valide: {apiUrl}/health");
                    }
                    else
                    {
                        Console.WriteLine($"❌ URL API invalide: {apiUrl}/health - {response.StatusCode}");
                    }
                    
                    // Tester aussi l'endpoint utilisateurs qui semble fonctionner
                    response = await _httpClient.GetAsync($"{apiUrl}/users");
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"✅ URL API valide: {apiUrl}/users");
                        var content = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"   Contenu: {content.Substring(0, Math.Min(content.Length, 100))}...");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Erreur lors du test de {apiUrl}: {ex.Message}");
                }
            }
            
            // Essayer de trouver l'URL WebSocket correcte
            foreach (var wsUrl in wsUrls)
            {
                try
                {
                    var webSocket = new ClientWebSocket();
                    // Utiliser correctement AddSubProtocol au lieu de SetRequestHeader
                    webSocket.Options.AddSubProtocol("v10.stomp");
                    Console.WriteLine("Ajout du sous-protocole: v10.stomp");
                    
                    if (!string.IsNullOrEmpty(SessionManager.Token))
                    {
                        webSocket.Options.SetRequestHeader("Authorization", $"Bearer {SessionManager.Token}");
                    }
                    
                    Console.WriteLine($"Test de l'URL WebSocket: {wsUrl} avec protocole v10.stomp");
                    
                    // Timeout court pour ne pas bloquer trop longtemps
                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                    try
                    {
                        await webSocket.ConnectAsync(new Uri(wsUrl), cts.Token);
                        if (webSocket.State == WebSocketState.Open)
                        {
                            Console.WriteLine($"✅ URL WebSocket valide: {wsUrl}");
                            
                            // Essayer d'envoyer une trame STOMP CONNECT
                            var stompConnect = $"CONNECT\naccept-version:1.0,1.1,1.2\nhost:localhost:8080\n\n\0";
                            var bytes = Encoding.UTF8.GetBytes(stompConnect);
                            await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cts.Token);
                            
                            // Attendre une réponse
                            var buffer = new byte[4096];
                            try 
                            {
                                var response = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                                var message = Encoding.UTF8.GetString(buffer, 0, response.Count);
                                Console.WriteLine($"✅ Réponse du serveur: {message.Replace("\0", "<NULL>")}");
                                
                                // Si on arrive jusqu'ici, c'est probablement la bonne URL
                                Console.WriteLine($"✅ URL WebSocket et STOMP confirmée: {wsUrl}");
                                
                                // Fermer proprement la connexion
                                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test terminé", CancellationToken.None);
                                break;
                            }
                            catch (OperationCanceledException)
                            {
                                Console.WriteLine("⚠️ Timeout en attente de réponse STOMP");
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Console.WriteLine($"⚠️ Timeout lors de la connexion à {wsUrl}");
                    }
                    catch (WebSocketException wsEx)
                    {
                        Console.WriteLine($"❌ Erreur WebSocket avec {wsUrl}: {wsEx.Message}");
                    }
                    
                    // Fermer le WebSocket si nécessaire
                    if (webSocket.State == WebSocketState.Open)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test terminé", CancellationToken.None);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Erreur lors du test de {wsUrl}: {ex.Message}");
                }
            }
            
            Console.WriteLine("Détection terminée. Reportez-vous aux logs pour identifier la configuration correcte.");
        }

        public async Task SubscribeToChannel(int channelId)
        {
            if (!_isStompConnected)
            {
                Console.WriteLine($"Impossible de s'abonner au canal {channelId} - STOMP non connecté");
                return;
            }
            
            try
            {
                string destination = $"/topic/channel.{channelId}";
                await _stompClient.SubscribeAsync(destination, HandleChannelMessage);
                Console.WriteLine($"Abonné au canal {channelId} via {destination}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'abonnement au canal {channelId}: {ex.Message}");
            }
        }
        
        private void HandleChannelMessage(string messageJson)
        {
            Console.WriteLine($"Message de canal reçu: {messageJson}");
            
            try
            {
                // Désérialiser le message JSON reçu
                var message = JsonSerializer.Deserialize<Message>(messageJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                if (message != null)
                {
                    // Notifier que le message a été reçu
                    MessageReceived?.Invoke(this, message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors du traitement du message de canal: {ex.Message}");
            }
        }
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StompNet
{
    /// <summary>
    /// Client STOMP simplifié pour la communication WebSocket
    /// </summary>
    public class StompClient
    {
        private ClientWebSocket _webSocket;
        private bool _isConnected = false;
        private readonly Dictionary<string, Action<string>> _subscriptions = new Dictionary<string, Action<string>>();
        private CancellationTokenSource _receiveCts;

        /// <summary>
        /// Constructeur du StompClient
        /// </summary>
        public StompClient()
        {
            _webSocket = new ClientWebSocket();
            _receiveCts = new CancellationTokenSource();
        }

        /// <summary>
        /// Se connecte au serveur STOMP via WebSocket
        /// </summary>
        /// <param name="url">URL du serveur WebSocket</param>
        /// <param name="login">Identifiant (optionnel)</param>
        /// <param name="passcode">Mot de passe (optionnel)</param>
        /// <param name="headers">Headers additionnels</param>
        /// <returns>Task représentant l'opération asynchrone</returns>
        public async Task ConnectAsync(string url, string? login = null, string? passcode = null, Dictionary<string, string>? headers = null)
        {
            try
            {
                // Initialiser WebSocket
                _webSocket = new ClientWebSocket();
                
                // Définir explicitement le protocole STOMP
                // Ne pas utiliser SetRequestHeader pour le protocole WebSocket
                _webSocket.Options.AddSubProtocol("v10.stomp");
                Console.WriteLine("Ajout du sous-protocole: v10.stomp");
                
                // Ajouter les headers d'authentification
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        Console.WriteLine($"Ajout header WebSocket: {header.Key}={header.Value}");
                        _webSocket.Options.SetRequestHeader(header.Key, header.Value);
                    }
                }

                // Configuration supplémentaire pour le client WebSocket
                _webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
                
                // Se connecter au WebSocket
                Console.WriteLine($"Tentative de connexion WebSocket à {url}");
                await _webSocket.ConnectAsync(new Uri(url), CancellationToken.None);
                Console.WriteLine($"WebSocket connecté avec succès à {url}, état: {_webSocket.State}");
                
                // Créer les headers STOMP
                var stompHeaders = new Dictionary<string, string>
                {
                    { "accept-version", "1.0,1.1,1.2" },
                    { "host", new Uri(url).Host },
                    { "heart-beat", "10000,10000" }
                };
                
                // Ajouter login/passcode si spécifiés
                if (!string.IsNullOrEmpty(login))
                    stompHeaders.Add("login", login);
                
                if (!string.IsNullOrEmpty(passcode))
                    stompHeaders.Add("passcode", passcode);
                
                // Ajouter les headers personnalisés
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        // Ne pas dupliquer les headers déjà présents dans les options WebSocket
                        if (!stompHeaders.ContainsKey(header.Key.ToLower()))
                        {
                            stompHeaders[header.Key] = header.Value;
                        }
                    }
                }
                
                // Envoyer le frame CONNECT
                Console.WriteLine("Envoi de la trame STOMP CONNECT avec les headers:");
                foreach (var header in stompHeaders)
                {
                    Console.WriteLine($"  {header.Key}={header.Value}");
                }
                
                await SendFrameAsync("CONNECT", stompHeaders, null);
                Console.WriteLine("Trame CONNECT envoyée, attente de la réponse...");
                
                // Attendre une réponse CONNECTED
                var timeoutToken = new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token;
                var response = await ReadFrameWithTimeoutAsync(timeoutToken);
                
                if (response.Command != "CONNECTED")
                {
                    throw new Exception($"Réponse inattendue du serveur STOMP: {response.Command}");
                }
                
                Console.WriteLine("Réponse CONNECTED reçue avec les headers:");
                foreach (var header in response.Headers)
                {
                    Console.WriteLine($"  {header.Key}={header.Value}");
                }
                
                _isConnected = true;
                
                // Démarrer la tâche de réception des messages
                _receiveCts = new CancellationTokenSource();
                Task.Run(() => ReceiveMessagesLoop(), _receiveCts.Token);
                
                Console.WriteLine("Connexion STOMP établie avec succès");
            }
            catch (WebSocketException wsEx)
            {
                Console.WriteLine($"Erreur WebSocket lors de la connexion STOMP: {wsEx.Message}");
                Console.WriteLine($"WebSocketError: {wsEx.WebSocketErrorCode}");
                _isConnected = false;
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la connexion STOMP: {ex.Message}");
                _isConnected = false;
                throw;
            }
        }

        /// <summary>
        /// S'abonne à une destination STOMP
        /// </summary>
        /// <param name="destination">Destination à laquelle s'abonner</param>
        /// <param name="messageHandler">Gestionnaire de messages</param>
        /// <returns>Task représentant l'opération asynchrone</returns>
        public async Task SubscribeAsync(string destination, Action<string> messageHandler)
        {
            if (!_isConnected || _webSocket.State != WebSocketState.Open)
                throw new InvalidOperationException("Client STOMP non connecté");
            
            var subscriptionId = Guid.NewGuid().ToString();
            
            var headers = new Dictionary<string, string>
            {
                { "id", subscriptionId },
                { "destination", destination }
            };
            
            await SendFrameAsync("SUBSCRIBE", headers, null);
            
            // Enregistrer le gestionnaire pour cette destination
            _subscriptions[destination] = messageHandler;
            
            Console.WriteLine($"Abonné à {destination} avec l'ID {subscriptionId}");
        }

        /// <summary>
        /// Envoie un message à une destination STOMP
        /// </summary>
        /// <param name="destination">Destination du message</param>
        /// <param name="message">Contenu du message</param>
        /// <returns>Task représentant l'opération asynchrone</returns>
        public async Task SendAsync(string destination, string message)
        {
            if (!_isConnected || _webSocket.State != WebSocketState.Open)
                throw new InvalidOperationException("Client STOMP non connecté");
            
            var headers = new Dictionary<string, string>
            {
                { "destination", destination },
                { "content-type", "application/json" },
                { "content-length", Encoding.UTF8.GetByteCount(message).ToString() }
            };
            
            await SendFrameAsync("SEND", headers, message);
            
            Console.WriteLine($"Message envoyé à {destination}: {message}");
        }

        /// <summary>
        /// Se déconnecte du serveur STOMP
        /// </summary>
        public async Task DisconnectAsync()
        {
            if (_isConnected && _webSocket.State == WebSocketState.Open)
            {
                try
                {
                    // Envoyer le frame DISCONNECT
                    await SendFrameAsync("DISCONNECT", new Dictionary<string, string>(), null);
                    
                    // Arrêter la boucle de réception
                    _receiveCts.Cancel();
                    
                    // Fermer le WebSocket
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Déconnexion normale", CancellationToken.None);
                    
                    _isConnected = false;
                    Console.WriteLine("Déconnecté du serveur STOMP");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors de la déconnexion STOMP: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Boucle de réception des messages
        /// </summary>
        private async Task ReceiveMessagesLoop()
        {
            try
            {
                while (_isConnected && _webSocket.State == WebSocketState.Open && !_receiveCts.Token.IsCancellationRequested)
                {
                    var frame = await ReadFrameAsync();
                    
                    if (frame.Command == "MESSAGE")
                    {
                        // Extraire la destination
                        if (frame.Headers.TryGetValue("destination", out var destination) && _subscriptions.TryGetValue(destination, out var handler))
                        {
                            // Appeler le gestionnaire avec le contenu du message
                            handler?.Invoke(frame.Body);
                        }
                        else
                        {
                            // Si pas de gestionnaire spécifique, afficher le message
                            Console.WriteLine($"Message reçu de {destination}: {frame.Body}");
                        }
                    }
                    else if (frame.Command == "ERROR")
                    {
                        Console.WriteLine($"Erreur STOMP: {frame.Body}");
                    }
                    
                    // Petite pause pour éviter de consommer trop de CPU
                    await Task.Delay(10);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur dans la boucle de réception: {ex.Message}");
                _isConnected = false;
            }
        }

        /// <summary>
        /// Envoie un frame STOMP
        /// </summary>
        private async Task SendFrameAsync(string command, Dictionary<string, string> headers, string body)
        {
            var builder = new StringBuilder();
            
            // Ajouter la commande
            builder.Append(command).Append('\n');
            
            // Ajouter les headers
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    builder.Append(header.Key).Append(':').Append(header.Value).Append('\n');
                }
            }
            
            // Ligne vide pour marquer la fin des headers
            builder.Append('\n');
            
            // Ajouter le corps si présent
            if (!string.IsNullOrEmpty(body))
            {
                builder.Append(body);
            }
            
            // Terminer le frame avec NULL byte
            builder.Append('\0');
            
            // Convertir en bytes et envoyer
            var frameBytes = Encoding.UTF8.GetBytes(builder.ToString());
            await _webSocket.SendAsync(new ArraySegment<byte>(frameBytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        /// <summary>
        /// Lit un frame STOMP
        /// </summary>
        private async Task<StompFrame> ReadFrameAsync()
        {
            var buffer = new byte[8192];
            var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            
            if (result.MessageType == WebSocketMessageType.Close)
            {
                _isConnected = false;
                return new StompFrame { Command = "CLOSE", Headers = new Dictionary<string, string>(), Body = null };
            }
            
            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            return ParseFrame(message);
        }

        /// <summary>
        /// Lit un frame STOMP avec timeout
        /// </summary>
        private async Task<StompFrame> ReadFrameWithTimeoutAsync(CancellationToken cancellationToken)
        {
            try
            {
                var buffer = new byte[8192];
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _isConnected = false;
                    return new StompFrame { Command = "CLOSE", Headers = new Dictionary<string, string>(), Body = null };
                }
                
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"Trame brute reçue: {message.Replace("\0", "<NULL>")}");
                return ParseFrame(message);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Timeout lors de la lecture de la trame STOMP");
                return new StompFrame { Command = "TIMEOUT", Headers = new Dictionary<string, string>(), Body = null };
            }
        }

        /// <summary>
        /// Parse un message STOMP en frame
        /// </summary>
        private StompFrame ParseFrame(string frameData)
        {
            var frame = new StompFrame
            {
                Headers = new Dictionary<string, string>()
            };
            
            // Supprimer le NULL byte à la fin si présent
            if (frameData.EndsWith('\0'))
            {
                frameData = frameData.Substring(0, frameData.Length - 1);
            }
            
            var parts = frameData.Split(new[] { '\n' }, 2);
            
            if (parts.Length > 0)
            {
                frame.Command = parts[0].Trim();
                
                if (parts.Length > 1)
                {
                    var headerBodyParts = parts[1].Split(new[] { "\n\n" }, 2, StringSplitOptions.None);
                    
                    // Traiter les headers
                    var headerLines = headerBodyParts[0].Split('\n');
                    foreach (var line in headerLines)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                            continue;
                            
                        var headerParts = line.Split(new[] { ':' }, 2);
                        if (headerParts.Length == 2)
                        {
                            frame.Headers[headerParts[0].Trim()] = headerParts[1].Trim();
                        }
                    }
                    
                    // Traiter le corps si présent
                    if (headerBodyParts.Length > 1)
                    {
                        frame.Body = headerBodyParts[1];
                    }
                }
            }
            
            return frame;
        }

        /// <summary>
        /// Classe représentant un frame STOMP
        /// </summary>
        private class StompFrame
        {
            public string Command { get; set; }
            public Dictionary<string, string> Headers { get; set; }
            public string Body { get; set; }
        }
    }
} 
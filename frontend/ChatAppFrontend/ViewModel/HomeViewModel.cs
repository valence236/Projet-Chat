using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ChatAppFrontend.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChatAppFrontend.ViewModel
{
    public enum ConversationType
    {
        Public,
        User,
        Channel
    }

    public class Conversation
    {
        public ConversationType Type { get; set; }
        public string? Id { get; set; }
        public string? Name { get; set; }
    }

    public partial class HomeViewModel : ObservableObject
    {
        private readonly ChannelService _channelService;
        private readonly MessageService _messageService;
        private readonly UserService _userService;

        [ObservableProperty]
        private ObservableCollection<Room>? rooms;

        [ObservableProperty]
        private ObservableCollection<Channel>? channels;

        [ObservableProperty] 
        private ObservableCollection<User>? users;

        [ObservableProperty]
        private string? nomNouveauSalon;

        [ObservableProperty]
        private string? descriptionNouveauSalon;

        [ObservableProperty]
        private Room? salonSelectionne;

        [ObservableProperty]
        private Channel? channelSelectionne;

        [ObservableProperty]
        private User? userSelectionne;

        [ObservableProperty]
        private Conversation? currentConversation;

        [ObservableProperty]
        private string? messageAEnvoyer;

        [ObservableProperty]
        private ObservableCollection<Message> messages = new();

        [ObservableProperty]
        private string? currentUsername;

        [ObservableProperty]
        private string? statusMessage;

        public ICommand CreerSalonCommand { get; }
        public ICommand SupprimerSalonCommand { get; }
        public ICommand EnvoyerMessageCommand { get; private set; }
        public ICommand SelectChannelCommand { get; private set; }
        public ICommand SelectUserCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand DeleteMessageCommand { get; }
        public ICommand TestConfigurationCommand { get; private set; }

        public HomeViewModel()
        {
            try
            {
                _messageService = new MessageService();
                _userService = new UserService();
                _channelService = new ChannelService();

                // S'abonner à l'événement MessageReceived pour recevoir les messages en temps réel
                _messageService.MessageReceived += OnMessageReceived;

                CreerSalonCommand = new RelayCommand(CreerSalon);
                SupprimerSalonCommand = new RelayCommand<Channel>(SupprimerSalon);
                EnvoyerMessageCommand = new AsyncRelayCommand(EnvoyerMessageAsync);
                SelectChannelCommand = new RelayCommand<Channel>(async (channel) => await SelectChannel(channel));
                SelectUserCommand = new RelayCommand<User>(user => SelectConversation(ConversationType.User, user?.Username, $"Conversation avec {user?.Username}"));
                LogoutCommand = new RelayCommand(Logout);
                DeleteMessageCommand = new RelayCommand<Message>(DeleteMessage);

                Channels = new ObservableCollection<Channel>();
                Users = new ObservableCollection<User>();

                CurrentUsername = SessionManager.Username ?? "Utilisateur";
                
                // Définir le chat public comme conversation par défaut
                CurrentConversation = new Conversation 
                { 
                    Type = ConversationType.Public, 
                    Id = null, 
                    Name = "Chat Public" 
                };

                // Utiliser Dispatcher pour charger les données après l'initialisation de la vue
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        // Charger les données initiales
                        ChargerSalonsDepuisApi();
                        ChargerUtilisateursDepuisApi();
                        ChargerMessagesSelonConversation();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erreur lors du chargement des données: {ex.Message}");
                    }
                }, System.Windows.Threading.DispatcherPriority.Background);

                InitializeCommands();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur dans le constructeur de HomeViewModel: {ex.Message}");
                // Initialiser au minimum les commandes et collections
                Channels = new ObservableCollection<Channel>();
                Users = new ObservableCollection<User>();
                CurrentConversation = new Conversation { Type = ConversationType.Public, Name = "Chat Public" };
            }
        }

        private void InitializeCommands()
        {
            // Commande de test de la configuration
            TestConfigurationCommand = new RelayCommand(async () => 
            {
                StatusMessage = "Test de la configuration du serveur en cours...";
                await Task.Run(async () => 
                {
                    try 
                    {
                        await _messageService.DetectServerConfiguration();
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            StatusMessage = "Test terminé, consultez la console pour les résultats.";
                        });
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            StatusMessage = $"Erreur lors du test: {ex.Message}";
                        });
                    }
                });
            });
        }

        private async void ChargerSalonsDepuisApi()
        {
            var salonsApi = await _channelService.GetPublicChannelsAsync();
            Channels?.Clear();
            foreach (var salon in salonsApi)
            {
                Channels?.Add(salon);
            }
        }

        private async void ChargerUtilisateursDepuisApi()
        {
            var usersApi = await _userService.GetUsersAsync();
            Users?.Clear();
            foreach (var user in usersApi)
            {
                Users?.Add(user);
            }
        }

        private async void CreerSalon()
        {
            if (!string.IsNullOrWhiteSpace(NomNouveauSalon))
            {
                var description = DescriptionNouveauSalon ?? ""; // Valeur par défaut vide si null
                var salonCree = await _channelService.CreateChannelAsync(NomNouveauSalon, description);

                if (salonCree != null)
                {
                    Channels?.Add(salonCree);
                    SelectConversation(ConversationType.Channel, salonCree.Id.ToString(), $"#{salonCree.Name}");
                }

                NomNouveauSalon = string.Empty;
                DescriptionNouveauSalon = string.Empty;
            }
        }

        private async void SupprimerSalon(Channel? salon)
        {
            if (salon != null)
            {
                bool success = await _channelService.DeleteChannelAsync(salon.Id);
                if (success)
                {
                    Channels?.Remove(salon);
                    
                    // Si la conversation courante était ce salon, revenir au chat public
                    if (CurrentConversation?.Type == ConversationType.Channel && 
                        CurrentConversation?.Id == salon.Id.ToString())
                    {
                        SelectConversation(ConversationType.Public, null, "Chat Public");
                    }
                }
            }
        }

        private async Task EnvoyerMessageAsync()
        {
            // Débuggage pour voir si la méthode est appelée
            Console.WriteLine("Tentative d'envoi de message");
            
            if (string.IsNullOrWhiteSpace(MessageAEnvoyer))
                return;

            string contenu = MessageAEnvoyer.Trim();
            bool success = false;
            
            try
            {
                Console.WriteLine($"Conversation type: {CurrentConversation?.Type}, ID: {CurrentConversation?.Id}");

                if (CurrentConversation?.Type == ConversationType.Channel && !string.IsNullOrEmpty(CurrentConversation.Id))
                {
                    int channelId = int.Parse(CurrentConversation.Id);
                    Console.WriteLine($"Envoi vers le salon {channelId}");
                    success = await _messageService.SendMessageAsync(channelId, contenu, null);
                }
                else if (CurrentConversation?.Type == ConversationType.User && !string.IsNullOrEmpty(CurrentConversation.Id))
                {
                    Console.WriteLine($"Envoi vers l'utilisateur {CurrentConversation.Id}");
                    success = await _messageService.SendMessageAsync(null, contenu, CurrentConversation.Id);
                }
                else // Public
                {
                    Console.WriteLine("Envoi message public");
                    success = await _messageService.SendMessageAsync(null, contenu, null);
                }

                Console.WriteLine($"Résultat de l'envoi: {success}");

                if (success)
                {
                    // Ajouter le message localement pour une UX plus fluide
                    AjouterMessageDansUI(CurrentUsername, contenu, DateTime.Now);
                    MessageAEnvoyer = string.Empty;
                }
                else
                {
                    Console.WriteLine("Échec de l'envoi du message");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception lors de l'envoi: {ex.Message}");
            }
        }

        private void AjouterMessageDansUI(string? sender, string contenu, DateTime timestamp)
        {
            // Vérifier et limiter la taille du contenu pour éviter des problèmes de rendu
            string contenuSanitized = SanitizeMessageContent(contenu);
            
            Messages.Add(new Message
            {
                Sender = SanitizeText(sender),
                Content = contenuSanitized,
                Timestamp = timestamp
            });
        }
        
        // Méthode pour assainir le texte des messages
        private string SanitizeMessageContent(string? content)
        {
            if (string.IsNullOrEmpty(content))
                return "[Message vide]";
            
            // Limiter la longueur du message
            if (content.Length > 500)
                content = content.Substring(0, 500) + "...";
            
            // Supprimer les caractères qui peuvent causer des problèmes
            content = content.Replace('\0', ' '); // Remplacer les caractères nuls
            
            return content;
        }
        
        // Méthode pour assainir les noms d'utilisateurs
        private string SanitizeText(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return "Anonyme";
            
            // Limiter la longueur
            if (text.Length > 50)
                text = text.Substring(0, 50) + "...";
            
            // Supprimer les caractères problématiques
            text = text.Replace('\0', ' ');
            
            return text;
        }

        public void SelectConversation(ConversationType type, string? id, string name)
        {
            CurrentConversation = new Conversation
            {
                Type = type,
                Id = id,
                Name = name
            };
            
            ChargerMessagesSelonConversation();
        }

        private async void ChargerMessagesSelonConversation()
        {
            Messages.Clear();
            
            List<Message> loadedMessages = new List<Message>();
            
            if (CurrentConversation?.Type == ConversationType.Channel && !string.IsNullOrEmpty(CurrentConversation.Id))
            {
                int channelId = int.Parse(CurrentConversation.Id);
                loadedMessages = await _messageService.GetMessagesForRoomAsync(channelId);
            }
            else if (CurrentConversation?.Type == ConversationType.User && !string.IsNullOrEmpty(CurrentConversation.Id))
            {
                string username = CurrentConversation.Id;
                loadedMessages = await _messageService.GetMessagesForUserAsync(username);
            }
            else // Public
            {
                loadedMessages = await _messageService.GetPublicMessagesAsync();
            }
            
            foreach (var message in loadedMessages)
            {
                Messages.Add(message);
            }
        }
        
        private async void DeleteMessage(Message? message)
        {
            if (message != null && CurrentConversation?.Type == ConversationType.Channel && !string.IsNullOrEmpty(CurrentConversation.Id))
            {
                int channelId = int.Parse(CurrentConversation.Id);
                bool success = await _messageService.DeleteMessageAsync(channelId, message.Id);
                if (success)
                {
                    Messages.Remove(message);
                }
            }
        }
        
        private void Logout()
        {
            SessionManager.Logout();
            NavigationService.NavigateToLogin();
        }

        private async Task SelectChannel(Channel channel)
        {
            if (channel == null)
                return;
                
            // Utiliser la méthode SelectConversation existante
            SelectConversation(ConversationType.Channel, channel.Id.ToString(), $"#{channel.Name}");
            
            // S'abonner au canal via STOMP
            await _messageService.SubscribeToChannel(channel.Id);
        }

        private void OnMessageReceived(object? sender, Message message)
        {
            // Ajouter le message reçu à la liste des messages
            if (message != null)
            {
                // Vérifier si le message appartient à la conversation actuelle
                bool shouldAdd = false;
                
                if (CurrentConversation?.Type == ConversationType.Public && message.IsPublic)
                {
                    shouldAdd = true;
                }
                else if (CurrentConversation?.Type == ConversationType.User && 
                        (message.SenderUsername == CurrentConversation.Id || 
                         message.RecipientUsername == CurrentConversation.Id))
                {
                    shouldAdd = true;
                }
                else if (CurrentConversation?.Type == ConversationType.Channel && 
                         message.ChannelId.HasValue && 
                         message.ChannelId.Value == int.Parse(CurrentConversation.Id))
                {
                    shouldAdd = true;
                }
                
                if (shouldAdd)
                {
                    // Exécuter sur le thread UI
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Messages.Add(message);
                        // Trier les messages par date si nécessaire
                        var sortedMessages = new ObservableCollection<Message>(Messages.OrderBy(m => m.Timestamp));
                        Messages = sortedMessages;
                    });
                }
            }
        }
    }
}

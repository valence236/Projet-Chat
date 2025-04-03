using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;
using ChatAppFrontend.Models;
using ChatAppFrontend.Services;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace ChatAppFrontend.ViewsModel
{
    public partial class HomeViewModel : ObservableObject
    {
        private readonly ChannelService _channelService = new();
        private readonly MessageService _messageService = new();

        [ObservableProperty]
        private ObservableCollection<Room> rooms;

        [ObservableProperty]
        private string nomNouveauSalon;

        [ObservableProperty]
        private string descriptionNouveauSalon;

        [ObservableProperty]
        private Room salonSelectionne;

        [ObservableProperty]
        private string messageAEnvoyer;

        [ObservableProperty]
        private ObservableCollection<Message> messages = new();

        [ObservableProperty]
        private string currentUsername;

        public ICommand CreerSalonCommand { get; }
        public ICommand SupprimerSalonCommand { get; }
        public ICommand EnvoyerMessageCommand { get; }

        public HomeViewModel()
        {
            CreerSalonCommand = new RelayCommand(CreerSalon);
            SupprimerSalonCommand = new RelayCommand<Room>(SupprimerSalon);
            EnvoyerMessageCommand = new AsyncRelayCommand(EnvoyerMessageAsync);

            Rooms = new ObservableCollection<Room>();
            ChargerSalonsDepuisApi();
        }

        private async void ChargerSalonsDepuisApi()
        {
            var salonsApi = await _channelService.GetPublicChannelsAsync();
            foreach (var salon in salonsApi)
            {
                if (!Rooms.Any(r => r.Id == salon.Id))
                {
                    Rooms.Add(salon);
                }
            }
        }

        private async void CreerSalon()
        {
            if (!string.IsNullOrWhiteSpace(NomNouveauSalon))
            {
                var salonCree = await _channelService.CreateChannelAsync(NomNouveauSalon, DescriptionNouveauSalon);

                if (salonCree != null)
                {
                    Rooms.Add(salonCree);
                }
                else
                {
                    var newRoom = new Room
                    {
                        Id = Rooms.Count + 1,
                        Nom = NomNouveauSalon,
                        Description = DescriptionNouveauSalon,
                    };
                    Rooms.Add(newRoom);
                }

                NomNouveauSalon = string.Empty;
                DescriptionNouveauSalon = string.Empty;
            }
        }

        private void SupprimerSalon(Room salon)
        {
            if (salon != null && Rooms.Contains(salon))
            {
                Rooms.Remove(salon);
            }
        }

        private async Task EnvoyerMessageAsync()
        {
            Console.WriteLine($"[DEBUG] Envoi du message dans le salon ID = {SalonSelectionne?.Id}");

            if (SalonSelectionne == null || string.IsNullOrWhiteSpace(MessageAEnvoyer))
                return;

            string contenu = MessageAEnvoyer.Trim();
            int roomId = SalonSelectionne.Id;

            bool success = await _messageService.SendMessageAsync(roomId, contenu);

            if (success)
            {
                AjouterMessageDansUI(CurrentUsername, contenu, DateTime.Now);
                MessageAEnvoyer = string.Empty;
            }
            else
            {
                Console.WriteLine("[DEBUG] Envoi échoué, message non affiché");
            }
        }

        private void AjouterMessageDansUI(string sender, string content, DateTime timestamp)
        {
            Messages.Add(new Message
            {
                Sender = sender,
                Content = content,
                Timestamp = timestamp
            });
        }

        public async Task SelectionnerSalonAsync(Room room)
        {
            SalonSelectionne = room;
            await ChargerMessagesPourSalon(room.Id);
        }

        private async Task ChargerMessagesPourSalon(int roomId)
        {
            Console.WriteLine($"[DEBUG] Chargement des messages du salon ID = {roomId}");

            Messages.Clear();
            var messages = await _messageService.GetMessagesForRoomAsync(roomId);

            foreach (var msg in messages)
            {
                Console.WriteLine($"[DEBUG] Message de {msg.Sender} : \"{msg.Content}\" à {msg.Timestamp}");
                Messages.Add(msg);
            }

            Console.WriteLine($"[DEBUG] Total messages chargés : {Messages.Count}");
        }
    }
}

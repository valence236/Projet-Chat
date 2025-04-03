using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;
using ChatAppFrontend.Models;
using ChatAppFrontend.Services;
using System.Threading.Tasks;
using System.Linq;

namespace ChatAppFrontend.ViewsModel
{
    public partial class HomeViewModel : ObservableObject
    {
        private readonly ChannelService _channelService = new();

        [ObservableProperty]
        private ObservableCollection<Room> rooms;

        [ObservableProperty]
        private string nomNouveauSalon;

        [ObservableProperty]
        private string descriptionNouveauSalon; // ✅ ajout pour champ description

        public ICommand CreerSalonCommand { get; }
        public ICommand SupprimerSalonCommand { get; }

        public HomeViewModel()
        {
            CreerSalonCommand = new RelayCommand(CreerSalon);
            SupprimerSalonCommand = new RelayCommand<Room>(SupprimerSalon);

            Rooms = new ObservableCollection<Room>();

            ChargerSalonsDepuisApi();
        }

        private async void ChargerSalonsDepuisApi()
        {
            var salonsApi = await _channelService.GetPublicChannelsAsync(); // ✅ nom corrigé ici
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
                // Appel API avec nom + description saisis
                var salonCree = await _channelService.CreateChannelAsync(NomNouveauSalon, DescriptionNouveauSalon);

                if (salonCree != null)
                {
                    Rooms.Add(salonCree);
                }
                else
                {
                    // Fallback local si API KO
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
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;
using ChatAppFrontend.Models;

namespace ChatAppFrontend.ViewsModel
{
    public partial class HomeViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<Room> rooms;

        [ObservableProperty]
        private string nomNouveauSalon;

        public ICommand CreerSalonCommand { get; }
        public ICommand SupprimerSalonCommand { get; }

        public HomeViewModel()
        {
            CreerSalonCommand = new RelayCommand(CreerSalon);
            SupprimerSalonCommand = new RelayCommand<Room>(SupprimerSalon);

            Rooms = new ObservableCollection<Room>
            {
                new Room { Id = 1, Nom = "Accueil", Description = "Accueil", Nombre = 10 },
                new Room { Id = 2, Nom = "Quiz Salon", Description = "C'est Quiz Chat", Nombre = 6 },
                new Room { Id = 3, Nom = "Salon Adulte", Description = "+18 seulement", Nombre = 5 },
                new Room { Id = 4, Nom = "Salon Teste", Description = "Ceci est uniquement un chat testuel", Nombre = 9 }
            };
        }

        private void CreerSalon()
        {
            if (!string.IsNullOrWhiteSpace(NomNouveauSalon))
            {
                var newRoom = new Room
                {
                    Id = Rooms.Count + 1,
                    Nom = NomNouveauSalon,
                    Description = "Salon créé manuellement",
                    Nombre = 0
                };

                Rooms.Add(newRoom);
                NomNouveauSalon = string.Empty;
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

namespace ChatAppFrontend.Models
{
    public class Room
    {
        public int Id { get; set; }
        public string Nom { get; set; }
        public string Description { get; set; }
        public int Nombre { get; set; } // 👈 ici on passe à int
    }
}
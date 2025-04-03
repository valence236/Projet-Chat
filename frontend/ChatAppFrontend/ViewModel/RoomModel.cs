using System.Text.Json.Serialization;

namespace ChatAppFrontend.Models
{
    public class Room
    {
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Nom { get; set; }

        public string Description { get; set; }

    }
}

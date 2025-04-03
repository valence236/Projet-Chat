using System;
using System.Text.Json.Serialization;

namespace ChatAppFrontend.ViewModel
{
    public class Room
    {
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Nom { get; set; }

        public string? Description { get; set; }

        public string? CreatorUsername { get; set; }
    }

    public class Channel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? CreatorUsername { get; set; }
    }
}

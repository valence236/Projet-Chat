using System;

namespace ChatAppFrontend.ViewModel
{
    public class Message
    {
        public int Id { get; set; }
        public string? Sender { get; set; }
        public string? Content { get; set; }
        public DateTime Timestamp { get; set; }
        public string? RecipientUsername { get; set; }
        public int? ChannelId { get; set; }
    }
}
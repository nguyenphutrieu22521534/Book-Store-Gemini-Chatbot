namespace BookMart.Models
{
    public class ChatMessage
    {
        public string Sender { get; set; } // "user" or "bot"
        public string Text { get; set; }
    }
}

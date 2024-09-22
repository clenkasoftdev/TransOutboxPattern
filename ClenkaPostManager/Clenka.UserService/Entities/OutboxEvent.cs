namespace Clenka.UserService.Entities
{
    public class OutboxEvent
    {
        public int ID { get; set; }
        public string Event { get; set; }
        public string Data { get; set; }

        public DateTime DateOccured { get; set; }
        public DateTime? ProcessedDate { get; set;}
    }
}

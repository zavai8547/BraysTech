using System.ComponentModel.DataAnnotations;

namespace SplashCityCarwash.Models
{
    public class WashQueue
    {
        [Key]
        public int QueueID { get; set; }
        public int TransactionID { get; set; }
        public string? AssignedWasherID { get; set; }
        public WashStatus Status { get; set; } = WashStatus.Waiting;
        public int QueuePosition { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public Transaction Transaction { get; set; } = null!;
        public AppUser? AssignedWasher { get; set; }
    }
}
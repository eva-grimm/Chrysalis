using Microsoft.Build.Evaluation;
using NuGet.Protocol.Plugins;
using System.ComponentModel.DataAnnotations;

namespace Chrysalis.Models
{
    public class Notification
    {
        private DateTime _created;

        // keys
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int TicketId { get; set; }
        [Required] public string? SenderId { get; set; }
        [Required] public string? RecipientId { get; set; }

        // properties
        [Required] public string? Title { get; set; }
        [Required] public string? Message { get; set; }
        public DateTime Created
        {
            get => _created.ToLocalTime();
            set => _created = value.ToUniversalTime();
        }
        public int NotificationTypeId { get; set; }
        public bool HasBeenViewed { get; set; }

        // nav properties
        public virtual NotificationType? NotificationType { get; set; }
        public virtual Ticket? Ticket { get; set; }
        public virtual Project? Project { get; set; }
        public virtual BTUser? Sender { get; set; }
        public virtual BTUser? Recipient { get; set; }
    }
}
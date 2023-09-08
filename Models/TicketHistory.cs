using Microsoft.AspNetCore.Http.HttpResults;
using System.ComponentModel.DataAnnotations;

namespace Chrysalis.Models
{
    public class TicketHistory
    {
        private DateTime _created;

        // keys
        public int Id { get; set; }
        public int TicketId { get; set; }

        // properties
        public string? PropertyName { get; set; }
        public string? Description { get; set; }
        public DateTime Created
        {
            get => _created.ToLocalTime();
            set => _created = value.ToUniversalTime();
        }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        [Required] public string? UserId { get; set; }

        // nav properties
        public virtual Ticket? Ticket { get; set; }
        public virtual BTUser? User { get; set; }
    }
}
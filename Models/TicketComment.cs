using Microsoft.AspNetCore.Http.HttpResults;
using System.ComponentModel.DataAnnotations;

namespace Chrysalis.Models
{
    public class TicketComment
    {
        private DateTime _created;

        // keys
        public int Id { get; set; }
        public int TicketId { get; set; }
        public string? UserId { get; set; }

        // properties
        [Required] public string? Comment { get; set; }
        public DateTime Created
        {
            get => _created.ToLocalTime();
            set => _created = value.ToUniversalTime();
        }

        // nav properties
        public virtual Ticket? Ticket { get; set; }
        public virtual BTUser? User { get; set; }
    }
}
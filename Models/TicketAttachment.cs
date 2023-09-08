using Microsoft.AspNetCore.Http.HttpResults;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Chrysalis.Models
{
    public class TicketAttachment
    {
        private DateTime _created;

        // keys
        public int Id { get; set; }
        public int TicketId { get; set; }
        [Required] public string? BTUserId { get; set; }

        // properties
        public string? Description { get; set; }
        public DateTime Created
        {
            get => _created.ToLocalTime();
            set => _created = value.ToUniversalTime();
        }

        // file properties
        [NotMapped] public IFormFile? FormFile { get; set; }
        public byte[]? FileData { get; set; }
        public string? FileType { get; set; }

        // nav properties
        public virtual Ticket? Ticket { get; set; }
        public virtual BTUser? BTUser { get; set; }
    }
}
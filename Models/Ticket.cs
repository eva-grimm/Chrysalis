using Microsoft.AspNetCore.Http.HttpResults;
using System.ComponentModel.DataAnnotations;

namespace Chrysalis.Models
{
    public class Ticket
    {
        private DateTime _created;
        private DateTime? _updated;

        // keys
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int TicketTypeId { get; set; }
        public int TicketStatusId { get; set; }
        public int TicketPriorityId { get; set; }
        public string? DeveloperUserId { get; set; }
        [Required] public string? SubmitterUserId { get; set; }

        // properties
        [Required] public string? Title { get; set; }
        [Required] public string? Description { get; set; }
        public DateTime Created
        {
            get => _created.ToLocalTime();
            set => _created = value.ToUniversalTime();
        }
        public DateTime? Updated
        {
            get => _updated?.ToLocalTime();
            set => _updated = value.HasValue ? value.Value.ToUniversalTime() : null;
        }
        public bool Archived { get; set; }
        public bool ArchivedByProject { get; set; }

        // nav properties
        public virtual Project? Project { get; set; }
        public virtual TicketPriority? TicketPriority { get; set; }
        public virtual TicketType? TicketType { get; set; }
        public virtual TicketStatus? TicketStatus { get; set; }
        public virtual BTUser? DeveloperUser { get; set; }
        public virtual BTUser? SubmitterUser { get; set; }
        public virtual ICollection<TicketComment> Comments { get; set; } = new HashSet<TicketComment>();
        public virtual ICollection<TicketAttachment> Attachments { get; set; } = new HashSet<TicketAttachment>();
        public virtual ICollection<TicketHistory> History { get; set; } = new HashSet<TicketHistory>();
    }
}
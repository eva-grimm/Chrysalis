using Microsoft.AspNetCore.Http.HttpResults;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Chrysalis.Models
{
    public class Project
    {
        private DateTime _created;
        private DateTime _startDate;
        private DateTime _endDate;

        // keys
        public int Id { get; set; }
        public int CompanyId { get; set; }

        // properties
        [Required] public string? Name { get; set; }
        [Required] public string? Description { get; set; }
        public DateTime Created
        {
            get => _created.ToLocalTime();
            set => _created = value.ToUniversalTime();
        }
        public DateTime StartDate
        {
            get => _startDate.ToLocalTime();
            set => _startDate = value.ToUniversalTime();
        }
        public DateTime EndDate
        {
            get => _endDate.ToLocalTime();
            set => _endDate = value.ToUniversalTime();
        }
        public int ProjectPriorityId { get; set; }
        public bool Archived { get; set; }

        // project image
        [NotMapped] public IFormFile? ImageFile { get; set; }
        public byte[]? ImageData { get; set; }
        public string? ImageType { get; set; }

        // nav properties
        public virtual Company? Company { get; set; }
        public virtual ProjectPriority? ProjectPriority { get; set; }
        public virtual ICollection<BTUser> Members { get; set; } = new HashSet<BTUser>();
        public virtual ICollection<Ticket> Tickets { get; set; } = new HashSet<Ticket>();
    }
}
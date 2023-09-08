using Microsoft.Build.Evaluation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Chrysalis.Models
{
    public class Company
    {
        // key
        public int Id { get; set; }

        // properties
        [Required] public string? Name { get; set; }
        public string? Description { get; set; }

        // profile image
        [NotMapped] public IFormFile? ImageFile { get; set; }
        public byte[]? ImageData { get; set; }
        public string? ImageType { get; set; }

        // nav properties
        public virtual ICollection<Project> Projects { get; set; } = new HashSet<Project>();
        public virtual ICollection<BTUser> Members { get; set; } = new HashSet<BTUser>();
        public virtual ICollection<Invite> Invites { get; set; } = new HashSet<Invite>();
    }
}
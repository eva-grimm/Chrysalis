using Microsoft.AspNetCore.Identity;
using Microsoft.Build.Evaluation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Chrysalis.Models
{
    public class BTUser : IdentityUser
    {
        // key
        public int CompanyId { get; set; }

        // properties
        [Required, Display(Name = "First Name")]
        [StringLength(50, ErrorMessage = "The {0} must be at least {2} and max {1} characters long", MinimumLength = 2)]
        public string? FirstName { get; set; }

        [Required, Display(Name = "Last Name")]
        [StringLength(50, ErrorMessage = "The {0} must be at least {2} and max {1} characters long", MinimumLength = 2)]
        public string? LastName { get; set; }

        [NotMapped] public string? FullName { get { return $"{FirstName} {LastName}"; } }

        // profile image
        [NotMapped] public IFormFile? ImageFile { get; set; }
        public byte[]? ImageData { get; set; }
        public string? ImageType { get; set; }

        // nav properties
        public virtual Company? Company { get; set; }
        public virtual ICollection<Project> Projects { get; set; } = new HashSet<Project>();
    }
}
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Chrysalis.Models.ViewModels
{
    public class ManageUserRolesViewModel
    {
        public BTUser? BTUser { get; set; }
        public MultiSelectList? Roles { get; set; }
        public IEnumerable<string>? SelectedRoles { get; set; }
    }
}
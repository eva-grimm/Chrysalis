using Chrysalis.Enums;
using Chrysalis.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace Chrysalis.Services.Interfaces
{
    public interface IRoleService
    {
        public Task<bool> AddUserToRoleAsync(BTUser? user, string? roleName);
        public Task<List<IdentityRole>> GetRolesAsync();
        public Task<IEnumerable<string>> GetUserRolesAsync(BTUser? user);
        public Task<List<BTUser>> GetUsersInRoleAsync(string? roleName, int? companyId);
        public Task<bool> IsUserInRoleAsync(BTUser? member, string? roleName);
        public Task<bool> RemoveUserFromRoleAsync(BTUser? user, string? roleName);
        public Task<bool> RemoveUserFromRolesAsync(BTUser? user, IEnumerable<string>? roleNames);
        public Task<MultiSelectList> GetUserRoleSelectListAsync(BTUser? user);
    }
}
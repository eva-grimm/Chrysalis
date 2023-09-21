using Chrysalis.Data;
using Chrysalis.Enums;
using Chrysalis.Models;
using Chrysalis.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Chrysalis.Services
{
    public class RoleService : IRoleService
    {
        private readonly UserManager<BTUser> _userMananger;
        private readonly ApplicationDbContext _context;

        public RoleService(UserManager<BTUser> userMananger,
            ApplicationDbContext context)
        {
            _userMananger = userMananger;
            _context = context;
        }

        public async Task<bool> AddUserToRoleAsync(BTUser? user, string? roleName)
        {
            try
            {
                if (user != null && !string.IsNullOrEmpty(roleName))
                {
                    bool result = (await _userMananger.AddToRoleAsync(user, roleName)).Succeeded;
                    return result;
                }
                return false;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<IdentityRole>> GetRolesAsync()
        {
            try
            {
                List<IdentityRole> result = new();
                result = await _context.Roles.ToListAsync();

                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IEnumerable<string>> GetUserRolesAsync(BTUser? user)
        {
            try
            {
                if (user != null)
                {
                    IEnumerable<string> result = await _userMananger.GetRolesAsync(user);
                    return result;
                }
                return Enumerable.Empty<string>();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<BTUser>> GetUsersInRoleAsync(string? roleName, int? companyId)
        {
            try
            {
                List<BTUser> result = new();
                List<BTUser> users = new();

                if (!string.IsNullOrEmpty(roleName))
                {
                    users = (await _userMananger.GetUsersInRoleAsync(roleName)).ToList();
                    result = users.Where(u => u.CompanyId == companyId).ToList();
                }
                return result;
                //return null!;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> IsUserInRoleAsync(BTUser? member, string? roleName)
        {
            try
            {
                if (member != null && !string.IsNullOrEmpty(roleName)) return await _userMananger.IsInRoleAsync(member, roleName);
                else return false;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> RemoveUserFromRoleAsync(BTUser? user, string? roleName)
        {
            try
            {
                if (user != null && !string.IsNullOrEmpty(roleName)) return (await _userMananger.RemoveFromRoleAsync(user, roleName)).Succeeded;
                else return false;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> RemoveUserFromRolesAsync(BTUser? user, IEnumerable<string>? roleNames)
        {
            try
            {
                if (user != null && roleNames != null) return (await _userMananger.RemoveFromRolesAsync(user, roleNames)).Succeeded;
                else return false;
            }
            catch (Exception)
            {
                throw;
            }
        }
    
        public async Task<MultiSelectList> GetUserRoleSelectListAsync(BTUser? user)
        {
            if (user == null) return new MultiSelectList(await GetRolesAsync(), "Name", "Name");
            IEnumerable<string> currentRoles = await GetUserRolesAsync(user);
            return new MultiSelectList(await GetRolesAsync(), "Name", "Name", currentRoles);
        }
    }
}
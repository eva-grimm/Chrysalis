﻿using Chrysalis.Data;
using Chrysalis.Models;
using Chrysalis.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Chrysalis.Services
{
    public class BTRolesService : IBTRolesService
    {
        private readonly UserManager<BTUser> _userMananger;
        private readonly ApplicationDbContext _context;

        public BTRolesService(UserManager<BTUser> userMananger,
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

        public async Task<IEnumerable<string>?> GetUserRolesAsync(BTUser? user)
        {
            try
            {
                if (user != null)
                {
                    IEnumerable<string> result = await _userMananger.GetRolesAsync(user);
                    return result;
                }
                return null;
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
                if (member != null && !string.IsNullOrEmpty(roleName))
                {
                    bool result = await _userMananger.IsInRoleAsync(member, roleName);
                    return result;
                }
                return false;
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
                if (user != null && !string.IsNullOrEmpty(roleName))
                {
                    bool result = (await _userMananger.RemoveFromRoleAsync(user, roleName)).Succeeded;
                    return result;
                }
                return false;
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
                if (user != null && roleNames != null)
                {
                    bool result = (await _userMananger.RemoveFromRolesAsync(user, roleNames)).Succeeded;
                    return result;
                }
                return false;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
using Chrysalis.Data;
using Chrysalis.Enums;
using Chrysalis.Models;
using Chrysalis.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Chrysalis.Services
{
    public class CompanyService : ICompanyService
    {
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<IdentityRole> _roleManager;

        public CompanyService(ApplicationDbContext context,
            RoleManager<IdentityRole> roleManager) 
        {
            _context = context;
            _roleManager = roleManager;
        }

        public bool CompanyExists(int companyId)
        {
            return (_context.Companies?.Any(e => e.Id == companyId)).GetValueOrDefault();
        }

        public async Task<bool> UpdateCompanyAsync(Company? company)
        {
            if (company == null) return false;

            try
            {
                _context.Update(company);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Returns the Company whose ID matches companyId
        /// </summary>
        /// <param name="companyId">ID of Company to Retrieve</param>
        /// <returns>Matching Company or null if no matches</returns>
        public async Task<Company> GetCompanyByIdAsync(int? companyId)
        {
            Company? company = await _context.Companies
                .Include(c => c.Projects)
                    .ThenInclude(p => p.ProjectPriority)
                .Include(c => c.Projects)
                    .ThenInclude(p => p.Tickets)
                        .ThenInclude(t => t.TicketPriority)
                .Include(c => c.Projects)
                    .ThenInclude(p => p.Tickets)
                        .ThenInclude(t => t.TicketStatus)
                .Include(c => c.Projects)
                    .ThenInclude(p => p.Tickets)
                        .ThenInclude(t => t.TicketType)
                .Include(c => c.Members)
                .FirstOrDefaultAsync(c => c.Id == companyId);

            return company ?? new Company();
        }

        public async Task<BTUser> GetCompanyUserByIdAsync(string? userId)
        {
            if (string.IsNullOrEmpty(userId)) return new BTUser();

            return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId) ?? new BTUser();
        }

        /// <summary>
        /// Returns all employees of the current user's company.
        /// </summary>
        /// <param name="companyId">Current User's CompanyID</param>
        public async Task<List<BTUser>> GetCompanyUsersAsync(int? companyId)
        {
            return await _context.Users
                .Where(u => u.CompanyId == companyId)
                .ToListAsync();
        }
        
        /// <summary>
        /// Returns all invites for the current user's company.
        /// </summary>
        /// <param name="companyId">Current User's CompanyID</param>
        public async Task<IEnumerable<Invite>> GetCompanyInvitesAsync(int? companyId)
        {
            return await _context.Invites
                .Where(i => i.CompanyId == companyId)
                .ToListAsync();
        }
    }
}
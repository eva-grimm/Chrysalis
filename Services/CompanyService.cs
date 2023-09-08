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

        /// <summary>
        /// Returns the Company whose ID matches companyId
        /// </summary>
        /// <param name="companyId">ID of Company to Retrieve</param>
        /// <returns>Matching Company or null if no matches</returns>
        public async Task<Company> GetCompanyById(int? companyId)
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

        /// <summary>
        /// Returns all employees of the current user's company.
        /// </summary>
        /// <param name="companyId">Current User's CompanyID</param>
        public async Task<IEnumerable<BTUser>> GetAllCompanyUsersAsync(int? companyId)
        {
            return await _context.Users
                .Where(bt => bt.CompanyId == companyId)
                .ToListAsync();
        }

        /// <summary>
        /// Returns all projects for the current user's company.
        /// </summary>
        /// <param name="companyId">Current User's CompanyID</param>
        public async Task<IEnumerable<Project>> GetAllCompanyProjectsAsync(int? companyId)
        {
            return await _context.Projects
                .Where(p => p.CompanyId == companyId)
                .ToListAsync();
        }
    }
}

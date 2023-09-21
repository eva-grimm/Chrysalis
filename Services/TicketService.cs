using Chrysalis.Data;
using Chrysalis.Enums;
using Chrysalis.Models;
using Chrysalis.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Chrysalis.Services
{
    public class TicketService : ITicketService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICompanyService _companyService;
        private readonly IRoleService _roleService;

        public TicketService(ApplicationDbContext context, 
            ICompanyService companyService, 
            IRoleService roleService)
        {
            _context = context;
            _companyService = companyService;
            _roleService = roleService;
        }

        public bool TicketExists(int id)
        {
            return _context.Tickets.Any(t => t.Id == id);
        }

        public async Task<bool> AddTicketAsync(Ticket? ticket)
        {
            if (ticket == null) return false;

            try
            {
                _context.Add(ticket);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> UpdateTicketAsync(Ticket? ticket)
        {
            if (ticket == null) return false;

            try
            {
                _context.Update(ticket);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
                throw;
            }
        }

        public async Task<Ticket?> GetTicketByIdAsync(int? ticketId, int? companyId)
        {
            if (ticketId == null) return new Ticket();

            try
            {
                Ticket? ticket = await _context.Tickets
                    .Include(t => t.Project)
                        .ThenInclude(p => p.Members)
                    .Include(t => t.TicketType)
                    .Include(t => t.TicketPriority)
                    .Include(t => t.TicketStatus)
                    .Include(t => t.Comments)
                        .ThenInclude(c => c.User)
                    .Include(t => t.Attachments)
                        .ThenInclude(a => a.User)
                    .Include(t => t.History)
                        .ThenInclude(h => h.User)
                    .Include(t => t.DeveloperUser)
                    .Include(t => t.SubmitterUser)
                    .FirstOrDefaultAsync(t => t.Id == ticketId
                        && t.Project!.CompanyId == companyId);
                return ticket;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<Ticket?> GetTicketByIdAsNoTrackingAsync(int? ticketId, int? companyId)
        {
            if (ticketId == null) return new Ticket();

            try
            {
                Ticket? ticket = await _context.Tickets
                    .Include(t => t.Project)
                        .ThenInclude(p => p.Members)
                    .Include(t => t.TicketType)
                    .Include(t => t.TicketPriority)
                    .Include(t => t.TicketStatus)
                    .Include(t => t.Comments)
                        .ThenInclude(c => c.User)
                    .Include(t => t.Attachments)
                        .ThenInclude(a => a.User)
                    .Include(t => t.History)
                        .ThenInclude(h => h.User)
                    .Include(t => t.DeveloperUser)
                    .Include(t => t.SubmitterUser)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == ticketId
                        && t.Project!.CompanyId == companyId);
                return ticket;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IEnumerable<Ticket>> GetTicketsAsync(int? companyId)
        {
            try
            {
                IEnumerable<Project> projects = await _context.Projects
                    .Where(p => p.CompanyId == companyId)
                    .Include(p => p.Tickets)
                        .ThenInclude(t => t.TicketPriority)
                    .Include(p => p.Tickets)
                        .ThenInclude(t => t.TicketStatus)
                    .Include(p => p.Tickets)
                        .ThenInclude(t => t.TicketType)
                        //TO-DO: put back?
                    //.Include(p => p.Tickets)
                    //    .ThenInclude(t => t.Comments)
                    //        .ThenInclude(c => c.User)
                    //.Include(p => p.Tickets)
                    //    .ThenInclude(t => t.Attachments)
                    //        .ThenInclude(c => c.User)
                    .ToListAsync();

                return projects.SelectMany(p => p.Tickets);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IEnumerable<Ticket>> GetTicketsByUserIdAsync(string? userId, int? companyId)
        {
            BTUser? user = await _companyService.GetCompanyUserByIdAsync(userId, companyId);

            try
            {
                // Project managers get the active tickets of their projects
                if (await _roleService.IsUserInRoleAsync(user, nameof(BTRoles.ProjectManager)))
                {
                    IEnumerable<Project> projects = await _context.Projects
                        .Where(p => p.CompanyId == companyId
                            && !p.Archived
                            && p.Members.Any(m => m.Id.Equals(userId)))
                    .Include(p => p.Tickets)
                        .ThenInclude(t => t.TicketPriority)
                    .Include(p => p.Tickets)
                        .ThenInclude(t => t.TicketStatus)
                    .Include(p => p.Tickets)
                        .ThenInclude(t => t.TicketType)
                        .ToListAsync();
                    return projects.SelectMany(p => p.Tickets)
                        .Where(t => !t.Archived
                            && !t.ArchivedByProject);
                }

                IEnumerable<Ticket> tickets = await GetTicketsAsync(companyId);
                // Admins get all active tickets of the company
                if (await _roleService.IsUserInRoleAsync(user, nameof(BTRoles.Admin)))
                    return tickets.Where(t => !t.Archived
                        && !t.ArchivedByProject);
                // developers get tickets where they're the assigned developer
                else if (await _roleService.IsUserInRoleAsync(user, nameof(BTRoles.Developer)))
                    return tickets.Where(t => t.DeveloperUserId == userId
                        && !t.Archived
                        && !t.ArchivedByProject);
                // submitters get tickets they submitted
                else
                    return tickets.Where(t => t.SubmitterUserId == userId
                        && !t.Archived
                        && !t.ArchivedByProject);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IEnumerable<Ticket>> GetActiveTicketsAsync(int? companyId)
        {
            IEnumerable<Project> projects = await _context.Projects
                    .Where(p => p.CompanyId == companyId)
                    .Include(p => p.Tickets)
                        .ThenInclude(t => t.TicketPriority)
                    .Include(p => p.Tickets)
                        .ThenInclude(t => t.TicketStatus)
                    .Include(p => p.Tickets)
                        .ThenInclude(t => t.TicketType)
                    .Include(p => p.Tickets)
                        .ThenInclude(t => t.Comments)
                            .ThenInclude(c => c.User)
                    .Include(p => p.Tickets)
                        .ThenInclude(t => t.Attachments)
                            .ThenInclude(c => c.User)
                    .ToListAsync();

            return projects.SelectMany(p => p.Tickets)
                .Where(t => !t.Archived
                    && !t.ArchivedByProject);
        }

        public async Task<IEnumerable<Ticket>> GetUnassignedActiveTicketsAsync(int? companyId)
        {
            IEnumerable<Project> projects = await _context.Projects
                    .Where(p => p.CompanyId == companyId)
                    .Include(p => p.Tickets)
                        .ThenInclude(t => t.TicketPriority)
                    .Include(p => p.Tickets)
                        .ThenInclude(t => t.TicketStatus)
                    .Include(p => p.Tickets)
                        .ThenInclude(t => t.TicketType)
                    .Include(p => p.Tickets)
                        .ThenInclude(t => t.Comments)
                            .ThenInclude(c => c.User)
                    .Include(p => p.Tickets)
                        .ThenInclude(t => t.Attachments)
                            .ThenInclude(c => c.User)
                    .ToListAsync();

            return projects.SelectMany(p => p.Tickets)
                .Where(t => !t.Archived
                    && !t.ArchivedByProject
                    && t.DeveloperUserId == null);
        }

        public async Task<IEnumerable<Ticket>> GetArchivedTicketsAsync(int? companyId)
        {
            IEnumerable<Project> projects = await _context.Projects
                    .Where(p => p.CompanyId == companyId)
                    .Include(p => p.Tickets)
                        .ThenInclude(t => t.TicketPriority)
                    .Include(p => p.Tickets)
                        .ThenInclude(t => t.TicketStatus)
                    .Include(p => p.Tickets)
                        .ThenInclude(t => t.TicketType)
                    .Include(p => p.Tickets)
                        .ThenInclude(t => t.Comments)
                            .ThenInclude(c => c.User)
                    .Include(p => p.Tickets)
                        .ThenInclude(t => t.Attachments)
                            .ThenInclude(c => c.User)
                    .ToListAsync();

            return projects.SelectMany(p => p.Tickets)
                .Where(t => t.Archived
                    || t.ArchivedByProject);
        }

        public async Task<BTUser?> GetDeveloperAsync(int? ticketId)
        {
            try
            {
                Ticket? ticket = await _context.Tickets
                .Include(t => t.DeveloperUser)
                    .FirstOrDefaultAsync(t => t.Id == ticketId);

                return ticket!.DeveloperUser ?? null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> AddTicketAttachmentAsync(TicketAttachment ticketAttachment)
        {
            try
            {
                await _context.AddAsync(ticketAttachment);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> UpdateTicketAttachmentAsync(TicketAttachment ticketAttachment)
        {
            try
            {
                _context.Update(ticketAttachment);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> DeleteTicketAttachmentAsync(TicketAttachment ticketAttachment)
        {
            try
            {
                _context.Remove(ticketAttachment);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<TicketAttachment?> GetTicketAttachmentByIdAsync(int ticketAttachmentId)
        {
            try
            {
                TicketAttachment? ticketAttachment = await _context.TicketAttachments
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Id == ticketAttachmentId);
                return ticketAttachment ?? new TicketAttachment();
            }
            catch (Exception)
            {
                return new TicketAttachment();
            }
        }
    }
}
using Chrysalis.Data;
using Chrysalis.Enums;
using Chrysalis.Models;
using Chrysalis.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Chrysalis.Services
{
    public class TicketService : ITicketService
    {
        private readonly ApplicationDbContext _context;

        public TicketService(ApplicationDbContext context)
        {
            _context = context;
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
                    .Include(t => t.TicketType)
                    .Include(t => t.TicketPriority)
                    .Include(t => t.TicketStatus)
                    .Include(t => t.Comments)
                        .ThenInclude(c => c.User)
                    .Include(t => t.Attachments)
                        .ThenInclude(a => a.User)
                    .Include(t => t.History)
                        .ThenInclude(h => h.User)
                    .FirstOrDefaultAsync(t => t.Id == ticketId);
                return ticket != null && ticket.Project!.CompanyId == companyId ? ticket : null;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<Ticket?> GetTicketByIdAsNoTrackingAsync(int? ticketId, int? companyId)
        {
            if (ticketId == null) return new Ticket();

            try
            {
                Ticket? ticket = await _context.Tickets
                    .Include(t => t.Project)
                    .Include(t => t.TicketType)
                    .Include(t => t.TicketPriority)
                    .Include(t => t.TicketStatus)
                    .Include(t => t.Comments)
                        .ThenInclude(c => c.User)
                    .Include(t => t.Attachments)
                        .ThenInclude(a => a.User)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == ticketId);
                return ticket != null && ticket.Project!.CompanyId == companyId ? ticket : null;
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
                    .Include(p => p.Tickets)
                        .ThenInclude(t => t.Comments)
                            .ThenInclude(c => c.User)
                    .Include(p => p.Tickets)
                        .ThenInclude(t => t.Attachments)
                            .ThenInclude(c => c.User)
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
            try
            {
                IEnumerable<Ticket> tickets = await GetTicketsAsync(companyId);

                return tickets.Where(t => t.DeveloperUserId == userId);
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

        public async Task<IEnumerable<TicketPriority>> GetTicketPrioritiesAsync()
        {
            return await _context.TicketPriorities.ToListAsync();
        }

        public async Task<IEnumerable<TicketStatus>> GetTicketStatusesAsync()
        {
            return await _context.TicketStatuses.ToListAsync();
        }

        public async Task<int?> GetTicketStatusByIdAsync(BTTicketStatuses status)
        {
            string? statusName = status.ToString();

            TicketStatus? ticketStatus = await _context.TicketStatuses
                .FirstOrDefaultAsync(ts => ts.Name!.Equals(statusName));

            return ticketStatus!.Id;
        }

        public async Task<IEnumerable<TicketType>> GetTicketTypesAsync()
        {
            return await _context.TicketTypes.ToListAsync();
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
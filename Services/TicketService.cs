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

        public TicketService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Provides a bool indicating whether a ticket exists that
        /// matches the provided ID.
        /// </summary>
        /// <param name="id">Potential ID of a Ticket</param>
        /// <returns>#</returns>
        public bool TicketExists(int id)
        {
            return _context.Tickets.Any(t => t.Id == id);
        }

        /// <summary>
        /// Adds provided ticket to the database.
        /// </summary>
        /// <param name="ticket">Ticket to be added</param>
        public async Task AddTicketAsync(Ticket? ticket)
        {
            if (ticket == null) return;

            try
            {
                _context.Add(ticket);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Updates the database with the provided ticket.
        /// </summary>
        /// <param name="ticket">Ticket to be updated</param>
        public async Task UpdateTicketAsync(Ticket? ticket)
        {
            if (ticket == null) return;

            try
            {
                _context.Update(ticket);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Retrieves the Ticket that matches the provided ticketID, but only if
        /// the user belongs to the company for that ticket.
        /// </summary>
        /// <param name="ticketId">ID of the Ticket to be retrieved</param>
        /// <param name="companyId">Current User's CompanyID</param>
        /// <returns>Returns matching ticket or null</returns>
        public async Task<Ticket?> GetSingleCompanyTicketAsync(int? ticketId, int? companyId)
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
                    .FirstOrDefaultAsync(t => t.Id == ticketId);
                return ticket != null && ticket.Project!.CompanyId == companyId ? ticket : null;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Gets all tickets for the company the current user belongs to.
        /// </summary>
        /// <param name="companyId">Current User's CompanyID</param>
        public async Task<IEnumerable<Ticket>> GetAllCompanyTicketsAsync(int? companyId)
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
                    .ToListAsync();

                return projects.SelectMany(p => p.Tickets);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Returns all TicketPriorities.
        /// </summary>
        public async Task<IEnumerable<TicketPriority>> GetAllTicketPriorities()
        {
            return await _context.TicketPriorities.ToListAsync();
        }

        /// <summary>
        /// Returns all TicketStatuses.
        /// </summary>
        public async Task<IEnumerable<TicketStatus>> GetAllTicketStatuses()
        {
            return await _context.TicketStatuses.ToListAsync();
        }

        public async Task<int?> GetTicketStatusIdAsync(BTTicketStatuses status)
        {
            string? statusName = status.ToString();

            TicketStatus? ticketStatus = await _context.TicketStatuses
                .FirstOrDefaultAsync(ts => ts.Name!.Equals(statusName));

            return ticketStatus!.Id;
        }

        /// <summary>
        /// Returns all TicketTypes.
        /// </summary>
        public async Task<IEnumerable<TicketType>> GetAllTicketTypes()
        {
            return await _context.TicketTypes.ToListAsync();
        }
    }
}
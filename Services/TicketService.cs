﻿using Chrysalis.Data;
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
        /// Adds provided Ticket to the database.
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

        public async Task<Ticket?> GetTicketAsync(int? ticketId, int? companyId)
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
                    .FirstOrDefaultAsync(t => t.Id == ticketId);
                return ticket != null && ticket.Project!.CompanyId == companyId ? ticket : null;
            }
            catch (Exception)
            {
                throw;
            }
        }
        
        public async Task<Ticket?> GetTicketAsNoTrackingAsync(int? ticketId, int? companyId)
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

		public async Task<IEnumerable<Ticket>> GetAllUserTicketsAsync(string? userId, int? companyId)
        {
			try
			{
                IEnumerable<Ticket> tickets = await GetAllCompanyTicketsAsync(companyId);

				return tickets.Where(t => t.DeveloperUserId == userId);
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

        public async Task AddTicketAttachmentAsync(TicketAttachment ticketAttachment)
        {
            try
            {
                await _context.AddAsync(ticketAttachment);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {

                throw;
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
                throw;
            }
        }
    }
}
﻿using Chrysalis.Data;
using Chrysalis.Models;
using Chrysalis.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Chrysalis.Services
{
    public class TicketHistoryService : ITicketHistoryService
    {
        private readonly ApplicationDbContext _context;

        public TicketHistoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> AddHistoryAsync(Ticket? oldTicket, Ticket? newTicket, string? userId)
        {
            try
            {
                // if brand new ticket
                if (oldTicket == null && newTicket != null)
                {
                    TicketHistory history = new()
                    {
                        TicketId = newTicket.Id,
                        PropertyName = string.Empty,
                        OldValue = string.Empty,
                        NewValue = string.Empty,
                        Created = DataUtility.GetPostGresDate(DateTime.Now),
                        UserId = userId,
                        Description = "New Ticket Created"
                    };

                    try
                    {
                        await _context.TicketHistories.AddAsync(history);
                        await _context.SaveChangesAsync();
                        return true;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
                // if ticket already exists
                else if (oldTicket != null && newTicket != null)
                {
                    // Check Ticket Title
                    if (oldTicket.Title != newTicket.Title)
                    {
                        TicketHistory history = new()
                        {
                            TicketId = newTicket.Id,
                            PropertyName = "Title",
                            OldValue = oldTicket.Title,
                            NewValue = newTicket.Title,
                            Created = DataUtility.GetPostGresDate(DateTime.Now),
                            UserId = userId,
                            Description = $"Changed ticket title from '{oldTicket.Title}' to '{newTicket.Title}'"
                        };

                        await _context.TicketHistories.AddAsync(history);
                    }

                    // Check Ticket Description
                    if (oldTicket.Description != newTicket.Description)
                    {
                        TicketHistory history = new()
                        {
                            TicketId = newTicket.Id,
                            PropertyName = "Description",
                            OldValue = oldTicket.Description,
                            NewValue = newTicket.Description,
                            Created = DataUtility.GetPostGresDate(DateTime.Now),
                            UserId = userId,
                            Description = $"Changed ticket description from '{oldTicket.Description}' to '{newTicket.Description}'"
                        };

                        await _context.TicketHistories.AddAsync(history);
                    }

                    // Check Ticket Priority
                    if (oldTicket.TicketPriorityId != newTicket.TicketPriorityId)
                    {
                        TicketHistory history = new()
                        {
                            TicketId = newTicket.Id,
                            PropertyName = "TicketPriority",
                            OldValue = oldTicket.TicketPriority?.Name,
                            NewValue = newTicket.TicketPriority?.Name,
                            Created = DataUtility.GetPostGresDate(DateTime.Now),
                            UserId = userId,
                            Description = $"Changed ticket priority from '{oldTicket.TicketPriority?.Name}' to '{newTicket.TicketPriority?.Name}'"
                        };

                        await _context.TicketHistories.AddAsync(history);
                    }

                    // Check Ticket Status
                    if (oldTicket.TicketStatusId != newTicket.TicketStatusId)
                    {
                        TicketHistory history = new()
                        {
                            TicketId = newTicket.Id,
                            PropertyName = "TicketStatus",
                            OldValue = oldTicket.TicketStatus?.Name,
                            NewValue = newTicket.TicketStatus?.Name,
                            Created = DataUtility.GetPostGresDate(DateTime.Now),
                            UserId = userId,
                            Description = $"Changed ticket status from '{oldTicket.TicketStatus?.Name}' to '{newTicket.TicketStatus?.Name}'"
                        };

                        await _context.TicketHistories.AddAsync(history);
                    }

                    // Check Ticket Type
                    if (oldTicket.TicketTypeId != newTicket.TicketTypeId)
                    {
                        TicketHistory history = new()
                        {
                            TicketId = newTicket.Id,
                            PropertyName = "TicketType",
                            OldValue = oldTicket.TicketType?.Name,
                            NewValue = newTicket.TicketType?.Name,
                            Created = DataUtility.GetPostGresDate(DateTime.Now),
                            UserId = userId,
                            Description = $"Changed ticket type from '{oldTicket.TicketType?.Name}' to '{newTicket.TicketType?.Name}'"
                        };

                        await _context.TicketHistories.AddAsync(history);
                    }

                    // Check Ticket Developer
                    if (oldTicket.DeveloperUserId != newTicket.DeveloperUserId)
                    {
                        TicketHistory history = new()
                        {
                            TicketId = newTicket.Id,
                            PropertyName = "DeveloperUser",
                            OldValue = oldTicket.DeveloperUser?.FullName ?? "Not Assigned",
                            NewValue = newTicket.DeveloperUser?.FullName,
                            Created = DataUtility.GetPostGresDate(DateTime.Now),
                            UserId = userId,
                            Description = $"Changed ticket developer from '{oldTicket.DeveloperUser?.FullName ?? "Not Assigned"}' to '{newTicket.DeveloperUser?.FullName}'"
                        };

                        await _context.TicketHistories.AddAsync(history);
                    }

                    try
                    {
                        await _context.SaveChangesAsync();
                        return true;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
                else return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> AddHistoryAsync(int? ticketId, string? model, string? userId)
        {
            try
            {
                Ticket? ticket = await _context.Tickets.FindAsync(ticketId);
                string description = model!.ToLower().Replace("ticket", "");
                description = $"New {description} added to ticket: {ticket?.Title}";

                if (ticket != null)
                {
                    TicketHistory history = new()
                    {
                        TicketId = ticket.Id,
                        PropertyName = model,
                        OldValue = string.Empty,
                        NewValue = string.Empty,
                        Created = DataUtility.GetPostGresDate(DateTime.Now),
                        UserId = userId,
                        Description = description
                    };

                    await _context.TicketHistories.AddAsync(history);
                    await _context.SaveChangesAsync();
                    return true;
                }
                else return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<List<TicketHistory>> GetCompanyTicketHistoriesAsync(int? companyId)
        {
            if (companyId == null) return new List<TicketHistory>();

            IEnumerable<Project> companyProjects = await _context.Projects
                .Where(p => p.CompanyId == companyId)
                .Include(p => p.Tickets)
                    .ThenInclude(t => t.History)
                .ToListAsync();

            List<TicketHistory> histories = companyProjects
                .SelectMany(p => p.Tickets)
                .SelectMany(t => t.History)
                .OrderByDescending(h => h.Created)
                .ToList();

            return histories;
        }

        public async Task<List<TicketHistory>> GetProjectTicketHistoriesAsync(int? projectId, int? companyId)
        {
            if (projectId == null || companyId == null) return new List<TicketHistory>();

            IEnumerable<Ticket> projectTickets = await _context.Tickets
                .Where(t => t.ProjectId == projectId && t.Project!.CompanyId == companyId)
                .Include(t => t.History)
                    .ThenInclude(h => h.User)
                .OrderBy(h => h.Created)
                .ToListAsync();

            List<TicketHistory> histories = projectTickets
                .SelectMany(t => t.History)
                .OrderByDescending(h => h.Created)
                .ToList();

            return histories;
        }
    }
}
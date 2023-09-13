using Chrysalis.Data;
using Chrysalis.Enums;
using Chrysalis.Models;
using Chrysalis.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Chrysalis.Services
{
    public class NotificiationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificiationService(ApplicationDbContext context) 
        { 
            _context = context;
        }

        /// <summary>
        /// Adds provided Notification to the database.
        /// </summary>
        /// <param name="ticket">Notification to be added</param>
        public async Task AddNotificationAsync(Notification? notification)
        {
            if (notification == null) return;

            try
            {
                _context.Add(notification);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Retrieves the specified user's notifications
        /// </summary>
        /// <param name="userId">User whose notifications are wanted</param>
        public async Task<List<Notification>> GetNotificationsByUserIdAsync(string? userId)
        {
            if (string.IsNullOrEmpty(userId)) return new List<Notification>();

            try
            {
                List<Notification> notifications = await _context.Notifications
                    .Where(n => n.RecipientId == userId)
                    .Include(n => n.NotificationType)
                    .Include(n => n.Ticket)
                    .Include(n => n.Project)
                    .Include(n => n.Sender)
                    .ToListAsync();

                return notifications;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public Task<bool> NewDeveloperNotificationAsync(int? ticketId, string? developerId, string? senderId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> NewTicketNotificationAsync(int? ticketId, string? senderId)
        {
            throw new NotImplementedException();
        }

        public Task NotificationsByRoleAsync(int? companyId, Notification? notification, BTRoles role)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SendEmailNotificationAsync(Notification? notification, string? emailSubject)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SendEmailNotificationByRoleAsync(int? companyId, Notification? notification, BTRoles role)
        {
            throw new NotImplementedException();
        }

        public Task<bool> TicketUpdateNotificationAsync(int? ticketId, string? developerId, string? senderId = null)
        {
            throw new NotImplementedException();
        }
    }
}

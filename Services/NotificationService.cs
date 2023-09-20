using Chrysalis.Data;
using Chrysalis.Enums;
using Chrysalis.Models;
using Chrysalis.Services.Interfaces;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.Design;
using System.Data;

namespace Chrysalis.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailService;
        private readonly IRoleService _rolesService;
        private readonly IProjectService _projectService;
        private readonly UserManager<BTUser> _userManager;

        public NotificationService(ApplicationDbContext context,
            IEmailSender emailService,
            IRoleService rolesService,
            UserManager<BTUser> userManager,
            IProjectService projectService)
        {
            _context = context;
            _emailService = emailService;
            _rolesService = rolesService;
            _userManager = userManager;
            _projectService = projectService;
        }

        public async Task<bool> AddNotificationAsync(Notification? notification)
        {
            if (notification == null) return false;

            try
            {
                _context.Add(notification);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> MarkNotificationReadAsync(int? notificationId, string? userId)
        {
            if (notificationId == null || string.IsNullOrEmpty(userId)) return false;

            Notification? notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId);
            if (notification == null || !notification.RecipientId!.Equals(userId)) return false;

            notification.HasBeenViewed = true;
            _context.Notifications.Update(notification);
            await _context.SaveChangesAsync();
            return true;
        }
        
        public async Task<bool> MarkNotificationUnreadAsync(int? notificationId, string? userId)
        {
            if (notificationId == null || string.IsNullOrEmpty(userId)) return false;

            Notification? notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId);
            if (notification == null || !notification.RecipientId!.Equals(userId)) return false;

            notification.HasBeenViewed = false;
            _context.Notifications.Update(notification);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(string? userId)
        {
            if (string.IsNullOrEmpty(userId)) return new List<Notification>();

            try
            {
                List<Notification> notifications = await _context.Notifications
                    .Where(n => n.RecipientId == userId)
                    .Include(n => n.Recipient)
                    .Include(n => n.Sender)
                    .Include(n => n.NotificationType)
                    .Include(n => n.Ticket)
                    .Include(n => n.Project)
                    .ToListAsync();

                return notifications;
            }
            catch (Exception)
            {
                return new List<Notification>();
            }
        }

        public async Task<List<Notification>> GetUnreadUserNotificationsAsync(string? userId)
        {
            if (string.IsNullOrEmpty(userId)) return new List<Notification>();

            try
            {
                List<Notification> notifications = await _context.Notifications
                    .Where(n => n.RecipientId == userId && !n.HasBeenViewed)
                    .Include(n => n.Recipient)
                    .Include(n => n.Sender)
                    .ToListAsync();

                return notifications;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> NotificationsByRoleAsync(int? companyId, Notification? notification, BTRoles role)
        {
            if (notification == null) return false;

            try
            {
                IEnumerable<string> memberIds = (await _rolesService
                    .GetUsersInRoleAsync(nameof(role), companyId))!
                    .Select(u => u.Id);

                // create copy of notification for each member of role
                foreach (string memberId in memberIds)
                {
                    notification.Id = 0;
                    notification.RecipientId = memberId;

                    await AddNotificationAsync(notification);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> NotifyDeveloperAsync(int? ticketId, string? developerId, string? senderId)
        {
            if (ticketId == null
                || string.IsNullOrEmpty(developerId)
                || string.IsNullOrEmpty(senderId))
                return false;

            try
            {
                BTUser? user = await _userManager.FindByIdAsync(senderId);
                Ticket? ticket = await _context.Tickets.FindAsync(ticketId);

                Notification? notification = new()
                {
                    TicketId = ticket!.Id,
                    Title = "Ticket Assigned to You",
                    Message = $"Ticket: {ticket.Title} was assigned to you by {user?.FullName}",
                    Created = DataUtility.GetPostGresDate(DateTime.Now),
                    SenderId = senderId,
                    RecipientId = developerId,
                    NotificationType = new NotificationType()
                    {
                        Name = BTNotificationType.Ticket.ToString()
                    }
                };

                await AddNotificationAsync(notification);
                await SendEmailNotificationAsync(notification, "Ticket Assigned to You");

                return true;
            }
            catch (Exception)
            {
                return false;
                throw;
            }
        }

        public async Task<bool> NewTicketNotificationAsync(int? ticketId, string? senderId)
        {
            if (ticketId == null || string.IsNullOrEmpty(senderId)) return false;

            BTUser? user = await _userManager.FindByIdAsync(senderId);
            Ticket? ticket = await _context.Tickets.FindAsync(ticketId);
            BTUser? projectManager = await _projectService.GetProjectManagerAsync(ticket?.ProjectId);

            if (ticket == null || user == null) return false;

            try
            {
                Notification? notification = new()
                {
                    TicketId = ticket.Id,
                    Title = "New Ticket Created",
                    Message = $"New Ticket: {ticket.Title} was created by {user.FullName} ",
                    Created = DataUtility.GetPostGresDate(DateTime.Now),
                    SenderId = senderId,
                    RecipientId = projectManager?.Id,
                    NotificationType = new NotificationType()
                    {
                        Name = BTNotificationType.Ticket.ToString()
                    }
                };

                if (projectManager != null)
                {
                    await AddNotificationAsync(notification);
                    await SendEmailNotificationAsync(notification, "New Ticket Added");
                }
                // if project has no PM assigned yet, alert all company Admins
                else
                {
                    await NotificationsByRoleAsync(user.CompanyId, notification, BTRoles.Admin);
                    await SendEmailNotificationByRoleAsync(user.CompanyId, notification, BTRoles.Admin);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
                throw;
            }
        }

        public async Task<bool> TicketUpdateNotificationAsync(int? ticketId, string? developerId, string? senderId = null)
        {
            if (ticketId == null || string.IsNullOrEmpty(senderId)) return false;

            BTUser? user = await _userManager.FindByIdAsync(senderId);
            Ticket? ticket = await _context.Tickets
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.Id == ticketId);
            BTUser? projectManager = await _projectService
                .GetProjectManagerAsync(ticket?.ProjectId);

            if (ticket == null || user == null) return false;

            try
            {
                Notification? notification = new()
                {
                    TicketId = ticketId.Value,
                    Title = "Ticket Updated",
                    Message = $"Ticket: {ticket?.Title} was updated by {user?.FullName}",
                    Created = DataUtility.GetPostGresDate(DateTime.Now),
                    SenderId = senderId,
                    RecipientId = projectManager?.Id,
                    NotificationType = new NotificationType()
                    {
                        Name = BTNotificationType.Ticket.ToString()
                    }
                };

                if (projectManager != null)
                {
                    await AddNotificationAsync(notification);
                    await SendEmailNotificationAsync(notification, "Ticket Updated");
                }
                else
                {
                    // TO-DO: need clarification
                    await NotificationsByRoleAsync(user!.CompanyId, notification, BTRoles.Admin);
                    await SendEmailNotificationByRoleAsync(user!.CompanyId, notification, BTRoles.Admin);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
                throw;
            }
        }

        public async Task<bool> SendEmailNotificationAsync(Notification? notification, string? emailSubject)
        {
            if (notification == null || emailSubject == null) return false;

            try
            {
                // get email for recipient
                BTUser? user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == notification.RecipientId);
                string? userEmail = user?.Email;

                if (userEmail != null)
                {
                    await _emailService.SendEmailAsync(userEmail, emailSubject, notification.Message!);
                    return true;
                }
                else return false;
            }
            catch (Exception)
            {
                return false;
                throw;
            }
        }

        public async Task<bool> SendEmailNotificationByRoleAsync(int? companyId, Notification? notification, BTRoles role)
        {
            if (companyId == null || notification == null) return false;

            try
            {
                IEnumerable<string?> memberEmails = (await _rolesService
                    .GetUsersInRoleAsync(nameof(role), companyId))!
                    .Select(u => u.Email);

                foreach (string? email in memberEmails)
                {
                    if (string.IsNullOrEmpty(email)) continue;
                    await _emailService.SendEmailAsync(email, notification.Title!, notification.Message!);
                }
                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
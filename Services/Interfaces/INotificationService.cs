using Chrysalis.Enums;
using Chrysalis.Models;

namespace Chrysalis.Services.Interfaces
{
    public interface INotificationService
    {
        public Task<bool> AddNotificationAsync(Notification? notification);
        public Task<List<Notification>> GetUserNotificationsAsync(string? userId);
        public Task<List<Notification>> GetUnreadUserNotificationsAsync(string? userId);
        public Task<bool> NotificationsByRoleAsync(int? companyId, Notification? notification, BTRoles role);
        public Task<bool> NotifyDeveloperAsync(int? ticketId, string? developerId, string? senderId);
        public Task<bool> NewTicketNotificationAsync(int? ticketId, string? senderId);
        public Task<bool> TicketUpdateNotificationAsync(int? ticketId, string? developerId, string? senderId = null);
        public Task<bool> SendEmailNotificationByRoleAsync(int? companyId, Notification? notification, BTRoles role);
        public Task<bool> SendEmailNotificationAsync(Notification? notification, string? emailSubject);
    }
}
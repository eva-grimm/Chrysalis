using Chrysalis.Enums;
using Chrysalis.Models;

namespace Chrysalis.Services.Interfaces
{
    public interface INotificationService
    {
        public Task AddNotificationAsync(Notification? notification);
        public Task<List<Notification>> GetNotificationsByUserIdAsync(string? userId);
        public Task NotificationsByRoleAsync(int? companyId, Notification? notification, BTRoles role);
        public Task<bool> NotifyDeveloperAsync(int? ticketId, string? developerId, string? senderId);
        public Task<bool> NewTicketNotificationAsync(int? ticketId, string? senderId);
        public Task<bool> TicketUpdateNotificationAsync(int? ticketId, string? developerId, string? senderId = null);
        public Task<bool> SendEmailNotificationByRoleAsync(int? companyId, Notification? notification, BTRoles role);
        public Task<bool> SendEmailNotificationAsync(Notification? notification, string? emailSubject);
    }
}
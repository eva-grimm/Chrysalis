using Chrysalis.Enums;
using Chrysalis.Models;
using Microsoft.EntityFrameworkCore;

namespace Chrysalis.Services.Interfaces
{
    public interface ILookupService
    {
        public Task<NotificationType> GetNotificationByEnumAsync(BTNotificationTypes type);
        public Task<ProjectPriority> GetProjectPriorityByEnumAsync(BTProjectPriorities priority);
        public Task<IEnumerable<TicketPriority>> GetTicketPrioritiesAsync();
        public Task<TicketPriority> GetTicketPriorityByEnumAsync(BTTicketPriorities priority);
        public Task<IEnumerable<TicketStatus>> GetTicketStatusesAsync();
        public Task<TicketStatus> GetTicketStatusByEnumAsync(BTTicketStatuses status);
        public Task<IEnumerable<TicketType>> GetTicketTypesAsync();
        public Task<TicketType> GetTicketTypeByEnumAsync(BTTicketTypes type);
    }
}
using Chrysalis.Data;
using Chrysalis.Enums;
using Chrysalis.Models;
using Chrysalis.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Chrysalis.Services
{
    public class LookupService : ILookupService
    {
        private readonly ApplicationDbContext _context;

        public LookupService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<NotificationType> GetNotificationByEnumAsync(BTNotificationTypes type)
        {
            return await _context.NotificationTypes
                .FirstAsync(nt => nt.Name == Enum.GetName(type));
        }

        public async Task<ProjectPriority> GetProjectPriorityByEnumAsync(BTProjectPriorities priority)
        {
            return await _context.ProjectPriorities
                .FirstAsync(pp => pp.Name == Enum.GetName(priority));
        }

        public async Task<IEnumerable<TicketPriority>> GetTicketPrioritiesAsync()
        {
            return await _context.TicketPriorities
                .OrderBy(t => t.Id)
                .ToListAsync();
        }

        public async Task<TicketPriority> GetTicketPriorityByEnumAsync(BTTicketPriorities priority)
        {
            return await _context.TicketPriorities
                .FirstAsync(tp => tp.Name == Enum.GetName(priority));
        }

        public async Task<IEnumerable<TicketStatus>> GetTicketStatusesAsync()
        {
            return await _context.TicketStatuses
                .OrderBy(ts => ts.Id)
                .ToListAsync();
        }

        public async Task<TicketStatus> GetTicketStatusByEnumAsync(BTTicketStatuses status)
        {
            return await _context.TicketStatuses
                .FirstAsync(ts => ts.Name == Enum.GetName(status));
        }

        public async Task<IEnumerable<TicketType>> GetTicketTypesAsync()
        {
            return await _context.TicketTypes
                .OrderBy(t => t.Id)
                .ToListAsync();
        }

        public async Task<TicketType> GetTicketTypeByEnumAsync(BTTicketTypes type)
        {
            return await _context.TicketTypes
                .FirstAsync(tt => tt.Name == Enum.GetName(type));
        }
    }
}
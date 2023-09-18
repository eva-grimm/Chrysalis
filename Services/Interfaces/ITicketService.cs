using Chrysalis.Models;
using Chrysalis.Enums;

namespace Chrysalis.Services.Interfaces
{
    public interface ITicketService
    {
        public bool TicketExists(int id);
        public Task<bool> AddTicketAsync(Ticket? ticket);
        public Task<bool> UpdateTicketAsync(Ticket? ticket);
        public Task<Ticket?> GetTicketByIdAsync(int? ticketId, int? companyId);
        public Task<Ticket?> GetTicketByIdAsNoTrackingAsync(int? ticketId, int? companyId);
        public Task<IEnumerable<Ticket>> GetCompanyTicketsAsync(int? companyId);
        public Task<IEnumerable<Ticket>> GetUserTicketsAsync(string? userId, int? companyId);
        public Task<IEnumerable<TicketPriority>> GetTicketPrioritiesAsync();
        public Task<IEnumerable<TicketStatus>> GetTicketStatusesAsync();
        public Task<int?> GetTicketStatusByIdAsync(BTTicketStatuses status);
        public Task<IEnumerable<TicketType>> GetTicketTypesAsync();
        public Task AddTicketAttachmentAsync(TicketAttachment ticketAttachment);
        public Task<TicketAttachment?> GetTicketAttachmentByIdAsync(int ticketAttachmentId);
    }
}

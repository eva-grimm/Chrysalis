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
        public Task<IEnumerable<Ticket>> GetTicketsAsync(int? companyId);
        public Task<IEnumerable<Ticket>> GetTicketsByUserIdAsync(string? userId, int? companyId);
        public Task<IEnumerable<Ticket>> GetActiveTicketsAsync(int? companyId);
        public Task<IEnumerable<Ticket>> GetUnassignedActiveTicketsAsync(int? companyId);
        public Task<IEnumerable<Ticket>> GetArchivedTicketsAsync(int? companyId);
        public Task<BTUser?> GetDeveloperAsync(int? ticketId);
        public Task<bool> AddTicketAttachmentAsync(TicketAttachment ticketAttachment);
        public Task<bool> UpdateTicketAttachmentAsync(TicketAttachment ticketAttachment);
        public Task<bool> DeleteTicketAttachmentAsync(TicketAttachment ticketAttachment);
        public Task<TicketAttachment?> GetTicketAttachmentByIdAsync(int ticketAttachmentId);
    }
}

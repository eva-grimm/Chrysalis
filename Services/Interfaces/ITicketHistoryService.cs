using Chrysalis.Models;

namespace Chrysalis.Services.Interfaces
{
    public interface ITicketHistoryService
    {
        public Task<bool> AddHistoryAsync(Ticket? oldTicket, Ticket? newTicket, string? userId);
        public Task<bool> AddHistoryAsync(int? ticketId, string? model, string? userId);
        public Task<List<TicketHistory>> GetProjectTicketHistoriesAsync(int? projectId, int? companyId);
        public Task<List<TicketHistory>> GetCompanyTicketHistoriesAsync(int? companyId);
    }
}
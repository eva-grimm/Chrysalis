using Chrysalis.Models;

namespace Chrysalis.Services.Interfaces
{
    public interface ITicketHistoryService
    {
        Task AddHistoryAsync(Ticket? oldTicket, Ticket? newTicket, string? userId);
        Task AddHistoryAsync(int? ticketId, string? model, string? userId);
        Task<List<TicketHistory>> GetProjectTicketHistoriesAsync(int? projectId, int? companyId);
        Task<List<TicketHistory>> GetCompanyTicketHistoriesAsync(int? companyId);
    }
}

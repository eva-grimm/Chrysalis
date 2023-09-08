using Chrysalis.Models;
using Chrysalis.Enums;

namespace Chrysalis.Services.Interfaces
{
    public interface ITicketService
    {
        public bool TicketExists(int id);
        public Task AddTicketAsync(Ticket? ticket);
        public Task UpdateTicketAsync(Ticket? ticket);
        public Task<Ticket?> GetSingleCompanyTicketAsync(int? ticketId, int? companyId);
        public Task<IEnumerable<Ticket>> GetAllCompanyTicketsAsync(int? companyId);
        public Task<IEnumerable<TicketPriority>> GetAllTicketPriorities();
        public Task<IEnumerable<TicketStatus>> GetAllTicketStatuses();
        public Task<int?> GetTicketStatusIdAsync(BTTicketStatuses status);
        public Task<IEnumerable<TicketType>> GetAllTicketTypes();
    }
}

using Chrysalis.Data;
using Chrysalis.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Sockets;

namespace Chrysalis.Services.Interfaces
{
    public class InviteService : IInviteService
    {
        private readonly ApplicationDbContext _context;
        
        public InviteService (ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<bool> AcceptInviteAsync(Guid? token, string? userId)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Adds provided Invite to the database
        /// </summary>
        /// <param name="invite">Invite to add</param>
        public async Task AddNewInviteAsync(Invite? invite)
        {
            if (invite == null) return;

            try
            {
                _context.Add(invite);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public Task<bool> AnyInviteAsync(Guid? token, string? email, int? companyId)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Retrieves the Invite that matches the provided inviteID, but only if
        /// the user belongs to the company for that ticket.
        /// </summary>
        /// <param name="ticketId">ID of the Ticket to be retrieved</param>
        /// <param name="companyId">Current User's CompanyID</param>
        public async Task<Invite?> GetSingleInviteAsync(int? inviteId, int? companyId)
        {
            if (inviteId == null) return new Invite();

            Invite? invite = await _context.Invites
                .FirstOrDefaultAsync(i => i.CompanyId == companyId);

            return invite;
        }

        public Task<Invite?> GetInviteAsync(Guid? token, string? email, int? companyId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ValidateInviteCodeAsync(Guid? token)
        {
            throw new NotImplementedException();
        }
    }
}

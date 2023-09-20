using Chrysalis.Services.Interfaces;
using Chrysalis.Models;
using Chrysalis.Data;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;

namespace Chrysalis.Services
{
    public class InviteService : IInviteService
    {
        private readonly ApplicationDbContext _context;

        public InviteService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> AddNewInviteAsync(Invite? invite)
        {
            if (invite == null) return false;
            try
            {
                await _context.AddAsync(invite);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> AcceptInviteAsync(Guid? token, string? userId, int? companyId)
        {
            if (string.IsNullOrEmpty(userId)) return false;

            Invite? invite = await _context.Invites
                .FirstOrDefaultAsync(i => i.CompanyToken == token
                && i.CompanyId == companyId);
            if (invite == null) return false;

            try
            {
                invite.IsValid = false;
                invite.InviteeId = userId;
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> AnyInviteAsync(Guid? token, string? email, int? companyId)
        {
            try
            {
                bool result = await _context.Invites
                    .Where(i => i.CompanyId == companyId)
                    .AnyAsync(i => i.CompanyToken == token && i.InviteeEmail == email);
                return result;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<Invite?> GetInviteAsync(int? inviteId, int? companyId)
        {
            try
            {
                Invite? invite = await _context.Invites
                    .Where(i => i.CompanyId == companyId)
                    .Include(i => i.Company)
                    .Include(i => i.Project)
                    .Include(i => i.Invitor)
                    .FirstOrDefaultAsync(i => i.Id == inviteId);

                return invite;
            }
            catch (Exception)
            {
                return new Invite();
            }
        }

        public async Task<Invite?> GetInviteAsync(Guid? token, string? email, int? companyId)
        {
            try
            {
                Invite? invite = await _context.Invites
                    .Where(i => i.CompanyId == companyId)
                    .Include(i => i.Company)
                    .Include(i => i.Project)
                    .Include(i => i.Invitor)
                    .FirstOrDefaultAsync(i => i.CompanyToken == token
                        && i.InviteeEmail == email);
                return invite;
            }
            catch (Exception)
            {
                return new Invite();
            }
        }

        public async Task<IEnumerable<Invite>> GetValidInvitesAsync(int? companyId)
        {
            if (companyId == null) return Enumerable.Empty<Invite>();

            IEnumerable<Invite> invites = await _context.Invites
                .Where(i => i.CompanyId ==  companyId
                    && i.IsValid)
                .Include(i => i.Project)
                .Include(i => i.Invitor)
                .ToListAsync();

            return invites;
        }

        public async Task<bool> CancelInviteAsync(int? inviteId, int? companyId)
        {
            if (inviteId == null || companyId == null) return false;
            Invite? invite = await GetInviteAsync(inviteId, companyId);
            if (invite == null) return false;

            invite.IsValid = false;
            _context.Update(invite);
            await _context.SaveChangesAsync();
            return true;
        }
        
        public async Task<bool> RenewInviteAsync(int? inviteId, int? companyId)
        {
            if (inviteId == null || companyId == null) return false;
            Invite? invite = await GetInviteAsync(inviteId, companyId);
            if (invite == null) return false;

            invite.InviteDate = DateTime.Now;
            invite.IsValid = true;
            _context.Update(invite);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ValidateInviteCodeAsync(Guid? token)
        {
            Invite? invite = await _context.Invites.FirstOrDefaultAsync(i => i.CompanyToken == token);

            if (invite == null) return false;

            DateTime inviteDate = invite.InviteDate.ToLocalTime();
            bool validDate = (DateTime.Now - inviteDate).TotalDays <= 7;

            if (validDate) return invite.IsValid;
            else return false;
        }
    }
}
using Chrysalis.Models;

namespace Chrysalis.Services.Interfaces
{
    public interface IInviteService
    {
        public Task<bool> AddNewInviteAsync(Invite? invite);
        public Task<bool> AcceptInviteAsync(Guid? token, string? userId, int? companyId);
        public Task<bool> AnyInviteAsync(Guid? token, string? email, int? companyId);
        public Task<Invite?> GetInviteAsync(int? inviteId, int? companyId);
        public Task<Invite?> GetInviteAsync(Guid? token, string? email, int? companyId);
        public Task<IEnumerable<Invite>> GetValidInvitesAsync(int? companyId);
        public Task<bool> CancelInviteAsync(int? inviteId, int? companyId);
        public Task<bool> RenewInviteAsync(int? inviteId, int? companyId);
        public Task<bool> ValidateInviteCodeAsync(Guid? token);
    }
}
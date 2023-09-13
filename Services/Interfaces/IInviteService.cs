using Chrysalis.Models;

namespace Chrysalis.Services.Interfaces
{
    public interface IInviteService
    {
        public Task<bool> AcceptInviteAsync(Guid? token, string? userId);
        public Task AddNewInviteAsync(Invite? invite);
        public Task<bool> AnyInviteAsync(Guid? token, string? email, int? companyId);
        public Task<Invite?> GetSingleInviteAsync(int? inviteId, int? companyId);
        public Task<Invite?> GetInviteAsync(Guid? token, string? email, int? companyId);
        public Task<bool> ValidateInviteCodeAsync(Guid? token);
    }
}

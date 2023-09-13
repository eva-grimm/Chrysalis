using Chrysalis.Models;
using Microsoft.EntityFrameworkCore;

namespace Chrysalis.Services.Interfaces
{
    public interface ICompanyService
    {
        public bool CompanyExists(int companyId);
        public Task<Company> GetCompanyByIdAsync(int? companyId);
        public Task<BTUser> GetCompanyUserByIdAsync(string? userId);
        public Task<List<BTUser>> GetAllCompanyUsersAsync(int? companyId);
        public Task<IEnumerable<Project>> GetAllCompanyProjectsAsync(int? companyId);
        public Task<IEnumerable<Invite>> GetAllCompanyInvitesAsync(int? companyId);

    }
}

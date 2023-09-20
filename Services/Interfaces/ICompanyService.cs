using Chrysalis.Models;
using Microsoft.EntityFrameworkCore;

namespace Chrysalis.Services.Interfaces
{
    public interface ICompanyService
    {
        public bool CompanyExists(int companyId);
        public Task<bool> UpdateCompanyAsync(Company? company);
        public Task<Company> GetCompanyByIdAsync(int? companyId);
        public Task<BTUser> GetCompanyUserByIdAsync(string? userId, int? companyId);
        public Task<List<BTUser>> GetCompanyUsersAsync(int? companyId);
        public Task<IEnumerable<Invite>> GetCompanyInvitesAsync(int? companyId);
    }
}

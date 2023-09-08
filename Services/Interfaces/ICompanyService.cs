using Chrysalis.Models;

namespace Chrysalis.Services.Interfaces
{
    public interface ICompanyService
    {
        public Task<Company> GetCompanyById(int? id);
        public Task<IEnumerable<BTUser>> GetAllCompanyUsersAsync(int? companyId);
        public Task<IEnumerable<Project>> GetAllCompanyProjectsAsync(int? companyId);
        //public Task<IEnumerable<BTUser>> GetDeveloperUsers(int? companyId);
    }
}

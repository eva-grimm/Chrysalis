using Chrysalis.Models;

namespace Chrysalis.Services.Interfaces
{
    public interface IProjectService
    {
        public bool ProjectExists(int? id);
        public Task AddProjectAsync(Project? project);
        public Task UpdateProjectAsync(Project? project);
        public Task<Project?> GetSingleCompanyProjectAsync(int? projectId, int? companyId);
        public Task<IEnumerable<Ticket>> GetProjectTickets(int? projectId);
        public Task<IEnumerable<BTUser>> GetCompanyMembersNotOnProject(int? projectId, int? companyId);
        public Task<ProjectPriority?> GetSingleProjectPriorityAsync(int? projectPriorityId);
        public Task<IEnumerable<ProjectPriority>> GetAllProjectPrioritiesAsync();
    }
}
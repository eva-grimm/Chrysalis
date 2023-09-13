using Chrysalis.Models;

namespace Chrysalis.Services.Interfaces
{
    public interface IProjectService
    {
        public bool ProjectExists(int? id);
        public Task AddProjectAsync(Project? project);
        public Task UpdateProjectAsync(Project? project);
        public Task<Project?> GetSingleCompanyProjectAsync(int? projectId, int? companyId);
        public Task<Project?> GetProjectAsNoTrackingAsync(int? projectId, int? companyId);
        public Task<IEnumerable<BTUser>> GetCompanyMembersNotOnProject(int? projectId, int? companyId);
        public Task<ProjectPriority?> GetSingleProjectPriorityAsync(int? projectPriorityId);
        public Task<IEnumerable<ProjectPriority>> GetAllProjectPrioritiesAsync();
        public Task<BTUser?> GetProjectManagerAsync(int? projectId);
        public Task<bool> AddProjectManagerAsync(string? userId, int? projectId);
        public Task<bool> AddMemberToProjectAsync(BTUser? member, int? projectId);
        public Task<bool> RemoveProjectManagerAsync(int? projectId);
        public Task<bool> RemoveMemberFromProjectAsync(BTUser? member, int? projectId);
    }
}
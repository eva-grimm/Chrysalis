using Chrysalis.Models;

namespace Chrysalis.Services.Interfaces
{
    public interface IProjectService
    {
        public bool ProjectExists(int? id);
        public Task<bool> AddProjectAsync(Project? project);
        public Task<bool> UpdateProjectAsync(Project? project);
        public Task<IEnumerable<Project>> GetProjectsAsync(int? companyId);
        public Task<IEnumerable<Project>> GetProjectsByUserIdAsync(string? userId, int? companyId);
        public Task<IEnumerable<Project>> GetUnassignedActiveProjectsAsync(int? companyId);
        public Task<IEnumerable<Project>> GetActiveProjectsAsync(int? companyId);
        public Task<IEnumerable<Project>> GetArchivedProjectsAsync(int? companyId);
        public Task<Project?> GetProjectAsync(int? projectId, int? companyId);
        public Task<Project?> GetProjectAsNoTrackingAsync(int? projectId, int? companyId);
        public Task<IEnumerable<BTUser>> GetMembersNotOnProjectAsync(int? projectId, int? companyId);
        public Task<ProjectPriority?> GetProjectPriorityByIdAsync(int? projectPriorityId);
        public Task<IEnumerable<ProjectPriority>> GetProjectPrioritiesAsync();
        public Task<BTUser?> GetProjectManagerAsync(int? projectId);
        public Task<IEnumerable<BTUser>> GetProjectDevelopersAsync(int? projectId);
        public Task<bool> AddProjectManagerAsync(string? userId, int? projectId);
        public Task<bool> AddMemberToProjectAsync(BTUser? member, int? projectId);
        public Task<bool> RemoveProjectManagerAsync(int? projectId);
        public Task<bool> RemoveMemberFromProjectAsync(BTUser? member, int? projectId);
        public Task<bool> RemoveProjectMembersAsync(int? projectId, int? companyId);
        public Task<bool> ArchiveProjectAsync(int projectId, int companyId);
        public Task<bool> UnarchiveProjectAsync(int projectId, int companyId);
    }
}
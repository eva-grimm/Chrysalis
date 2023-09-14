using Chrysalis.Data;
using Chrysalis.Models;
using Chrysalis.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Chrysalis.Enums;

namespace Chrysalis.Services
{
    public class ProjectService : IProjectService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<BTUser> _userManager;
        private readonly IRolesService _roleService;

        public ProjectService(ApplicationDbContext context,
            UserManager<BTUser> userManager,
            IRolesService roleService)
        {
            _context = context;
            _userManager = userManager;
            _roleService = roleService;
        }

        /// <summary>
        /// Provides a bool indicating whether a project exists that
        /// matches the provided ID.
        /// </summary>
        /// <param name="id">Potential ID of a Project</param>
        /// <returns>#</returns>
        public bool ProjectExists(int? id)
        {
            return _context.Projects.Any(p => p.Id == id);
        }

        /// <summary>
        /// Adds provided project to the database.
        /// </summary>
        /// <param name="project">Project to be added</param>
        /// <returns>#</returns>
        public async Task AddProjectAsync(Project? project)
        {
            if (project == null) return;

            try
            {
                _context.Add(project);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> UpdateProjectAsync(Project? project)
        {
            if (project == null) return false;

            try
            {
                _context.Update(project);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
                throw;
            }
        }

        public async Task<IEnumerable<Project>> GetCompanyProjectsAsync(int? companyId)
        {
            return await _context.Projects
                                .Where(p => p.CompanyId == companyId)
                                .ToListAsync();
        }

        /// <summary>
        /// Returns the Project that matches the ID provide, but only
        /// if the current user belongs to the same company as the project.
        /// </summary>
        /// <param name="projectId">ID of the Project to be retrieved</param>
        /// <param name="companyId">Current User's CompanyID</param>
        /// <returns>Null if no matching Project found</returns>
        public async Task<Project?> GetSingleCompanyProjectAsync(int? projectId, int? companyId)
        {
            if (projectId == null) return new Project();

            try
            {
                Project? project = await _context.Projects
                    .Where(p => p.CompanyId == companyId)
                    .Include(p => p.Company)
                    .Include(p => p.Members)
                    .Include(p => p.Tickets)
                        .ThenInclude(t => t.Comments)
                            .ThenInclude(c => c.User)
                    .Include(p => p.Tickets)
                        .ThenInclude(t => t.Attachments)
                    .Include(p => p.Tickets)
                        .ThenInclude(t => t.History)
                    .Include(p => p.ProjectPriority)
                    .FirstOrDefaultAsync(p => p.Id == projectId);
                return project;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<Project?> GetProjectAsNoTrackingAsync(int? projectId, int? companyId)
        {
            if (projectId == null) return new Project();

            try
            {
                Project? project = await _context.Projects
                    .Where(p => p.CompanyId == companyId)
                    .Include(p => p.Company)
                    .Include(p => p.Members)
                    .Include(p => p.Tickets)
                        .ThenInclude(t => t.Comments)
                            .ThenInclude(c => c.User)
                    .Include(p => p.Tickets)
                        .ThenInclude(t => t.Attachments)
                    .Include(p => p.Tickets)
                        .ThenInclude(t => t.History)
                    .Include(p => p.ProjectPriority)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == projectId);
                return project;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IEnumerable<BTUser>> GetCompanyMembersNotOnProject(int? projectId, int? companyId)
        {
            if (projectId == null) return Enumerable.Empty<BTUser>();

            List<BTUser> companyUsers = await _context.Users
                .Where(bt => bt.CompanyId == companyId)
                .Include(bt => bt.Projects)
                .ToListAsync();

            //Project? project = await GetSingleCompanyProjectAsync(projectId, companyId);
            //if (project == null) return Enumerable.Empty<BTUser>();
            //ICollection<BTUser> projectUsers = project.Members;
            ICollection<BTUser> projectUsers = (await GetSingleCompanyProjectAsync(projectId, companyId))!.Members;

            return companyUsers.Except(projectUsers).ToList();
        }

        /// <summary>
        /// Gets the ProjectPriority that matches the ID provided.
        /// </summary>
        /// <param name="projectPriorityId">ID of the desired ProjectPriority</param>
        /// <returns>Null if no matching ProjectPriority found</returns>
        public async Task<ProjectPriority?> GetSingleProjectPriorityAsync(int? projectPriorityId)
        {
            if (projectPriorityId == null) return new ProjectPriority();

            try
            {
                ProjectPriority? projectPriority = await _context.ProjectPriorities
                    .FirstOrDefaultAsync(pp => pp.Id == projectPriorityId.Value);
                return projectPriority;
            }
            catch (Exception)
            {

                throw;
            }
        }

        /// <summary>
        /// Returns all ProjectPriorities
        /// </summary>
        public async Task<IEnumerable<ProjectPriority>> GetAllProjectPrioritiesAsync()
        {
            return await _context.ProjectPriorities.ToListAsync();
        }

        public async Task<BTUser?> GetProjectManagerAsync(int? projectId)
        {
            try
            {
                Project? project = await _context.Projects
                    .Include(p => p.Members)
                    .FirstOrDefaultAsync(p => p.Id == projectId);

                if (project != null)
                {
                    foreach (BTUser member in project.Members)
                    {
                        if (await _roleService.IsUserInRoleAsync(member, nameof(BTRoles.ProjectManager)))
                        {
                            return member;
                        }
                    }
                }

                return null;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> AddProjectManagerAsync(string? userId, int? projectId)
        {
            try
            {
                if (userId == null || projectId == null) return false;
                BTUser? currentPM = await GetProjectManagerAsync(projectId);
                BTUser? selectedPM = await _context.Users.FindAsync(userId);

                if (currentPM != null) await RemoveProjectManagerAsync(projectId);

                try
                {
                    return await AddMemberToProjectAsync(selectedPM!, projectId);
                }
                catch (Exception)
                {
                    throw;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> AddMemberToProjectAsync(BTUser? member, int? projectId)
        {
            try
            {
                if (member == null || projectId == null) return false;
                Project? project = await _context.Projects
                    .Include(p => p.Members)
                    .FirstOrDefaultAsync(p => p.Id == projectId);
                if (project == null) return false;

                bool IsOnProject = project.Members.Any(m => m.Id == member.Id);

                if (!IsOnProject)
                {
                    project.Members.Add(member);
                    await _context.SaveChangesAsync();
                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> RemoveProjectManagerAsync(int? projectId)
        {
            try
            {
                if (projectId == null) return false;
                Project? project = await _context.Projects
                    .Include(p => p.Members)
                    .FirstOrDefaultAsync(p => p.Id == projectId);
                if (project == null) return false;

                foreach (BTUser member in project.Members)
                {
                    if (await _roleService.IsUserInRoleAsync(member, nameof(BTRoles.ProjectManager)))
                    {
                        return await RemoveMemberFromProjectAsync(member, projectId);
                    }
                }

                return false;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> RemoveMemberFromProjectAsync(BTUser? member, int? projectId)
        {
            try
            {
                if (member == null || projectId == null) return false;
                Project? project = await _context.Projects
                    .Include(p => p.Members)
                    .FirstOrDefaultAsync(p => p.Id == projectId);
                if (project == null) return false;

                bool IsOnProject = project.Members.Any(m => m.Id == member.Id);

                if (IsOnProject)
                {
                    project.Members.Remove(member);
                    await _context.SaveChangesAsync();
                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> ArchiveProjectAsync(int? projectId, int? companyId)
        {
            Project? project = await GetSingleCompanyProjectAsync(projectId, companyId);
            if (project == null) return false;

            try
            {
                project.Archived = true;

                foreach (Ticket ticket in project.Tickets)
                {
                    ticket.ArchivedByProject = true;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
                throw;
            }
        }

        public async Task<bool> UnarchiveProjectAsync(int? projectId, int? companyId)
        {
            Project? project = await GetSingleCompanyProjectAsync(projectId, companyId);
            if (project == null) return false;

            try
            {
                project.Archived = false;

                foreach (Ticket ticket in project.Tickets)
                {
                    ticket.ArchivedByProject = false;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
                throw;
            }
        }
    }
}

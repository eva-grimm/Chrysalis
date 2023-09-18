using Chrysalis.Data;
using Chrysalis.Models;
using Chrysalis.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Chrysalis.Enums;
using System.Collections.Generic;

namespace Chrysalis.Services
{
    public class ProjectService : IProjectService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<BTUser> _userManager;
        private readonly IRoleService _roleService;

        public ProjectService(ApplicationDbContext context,
            UserManager<BTUser> userManager,
            IRoleService roleService)
        {
            _context = context;
            _userManager = userManager;
            _roleService = roleService;
        }

        public bool ProjectExists(int? id)
        {
            return _context.Projects.Any(p => p.Id == id);
        }

        public async Task<bool> AddProjectAsync(Project? project)
        {
            if (project == null) return true;

            try
            {
                _context.Add(project);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false; ;
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

        public async Task<IEnumerable<Project>> GetProjectsAsync(int? companyId)
        {
            return await _context.Projects
                .Where(p => p.CompanyId == companyId)
                .Include(p => p.Members)
                .Include(p => p.Tickets)
                .Include(p => p.ProjectPriority)
                .ToListAsync();
        }

        public async Task<IEnumerable<Project>> GetUnassignedProjectsAsync(int? companyId)
        {
            List<Project> projects = await _context.Projects
                                .Where(p => p.CompanyId == companyId)
                                .Include(p => p.Members)
                                .Include(p => p.Tickets)
                                .ToListAsync();

            List<Project> unassignedProjects = new();

            foreach (Project project in projects)
            {
                BTUser? pm = await GetProjectManagerAsync(project.Id);
                if (pm == null) unassignedProjects.Add(project);
            }

            return unassignedProjects;
        }

        public async Task<IEnumerable<Project>> GetArchivedProjectsAsync(int? companyId)
        {
            return await _context.Projects
                                .Where(p => p.CompanyId == companyId
                                && p.Archived)
                                .Include(p => p.Members)
                                .Include(p => p.Tickets)
                                .ToListAsync();
        }

        public async Task<Project?> GetProjectAsync(int? projectId, int? companyId)
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
                        .ThenInclude(t => t.TicketStatus)
                    .Include(p => p.Tickets)
                        .ThenInclude(t => t.TicketPriority)
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
                        .ThenInclude(t => t.TicketStatus)
                    .Include(p => p.Tickets)
                        .ThenInclude(t => t.TicketPriority)
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

        public async Task<IEnumerable<BTUser>> GetMembersNotOnProjectAsync(int? projectId, int? companyId)
        {
            if (projectId == null) return Enumerable.Empty<BTUser>();

            List<BTUser> companyUsers = await _context.Users
                .Where(bt => bt.CompanyId == companyId)
                .Include(bt => bt.Projects)
                .ToListAsync();

            //Project? project = await GetSingleCompanyProjectAsync(projectId, companyId);
            //if (project == null) return Enumerable.Empty<BTUser>();
            //ICollection<BTUser> projectUsers = project.Members;
            ICollection<BTUser> projectUsers = (await GetProjectAsync(projectId, companyId))!.Members;

            return companyUsers.Except(projectUsers).ToList();
        }

        public async Task<ProjectPriority?> GetProjectPriorityByIdAsync(int? projectPriorityId)
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

        public async Task<IEnumerable<ProjectPriority>> GetProjectPrioritiesAsync()
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

        public async Task<IEnumerable<BTUser>> GetProjectDevelopersAsync(int? projectId)
        {
            try
            {
                Project? project = await _context.Projects
                    .Include(p => p.Members)
                    .FirstOrDefaultAsync(p => p.Id == projectId);

                List<BTUser> projectDevelopers = new();

                if (project != null)
                {
                    foreach (BTUser member in project.Members)
                    {
                        if (await _roleService.IsUserInRoleAsync(member, nameof(BTRoles.Developer)))
                        {
                            projectDevelopers.Add(member);
                        }
                    }
                }
                return projectDevelopers;
            }
            catch (Exception)
            {
                return new List<BTUser>();
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

        public async Task<bool> RemoveProjectMembersAsync(int? projectId, int? companyId)
        {
            if (projectId == null) return false;

            try
            {
                Project? project = await GetProjectAsync(projectId, companyId);
                if (project == null) return false;
                project.Members.Clear();
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> ArchiveProjectAsync(int? projectId, int? companyId)
        {
            Project? project = await GetProjectAsync(projectId, companyId);
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
            Project? project = await GetProjectAsync(projectId, companyId);
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
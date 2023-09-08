using Chrysalis.Data;
using Chrysalis.Models;
using Chrysalis.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Chrysalis.Services
{
    public class ProjectService : IProjectService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<BTUser> _userManager;

        public ProjectService(ApplicationDbContext context,
            UserManager<BTUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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

        /// <summary>
        /// Updates the database with the provided project.
        /// </summary>
        /// <param name="project">Project to be updated</param>
        /// <returns>#</returns>
        public async Task UpdateProjectAsync(Project? project)
        {
            if (project == null) return;

            try
            {
                _context.Update(project);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                throw;
            }
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
                    .Include(p => p.ProjectPriority)
                    .FirstOrDefaultAsync(p => p.Id == projectId);
                return project;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Gets all tickets associated with the project matching the
        /// provided projectId.
        /// </summary>
        /// <param name="projectId">ID of the Project to retrieve Tickets from</param>
        /// <returns>Empty Enumerable if no matching Project found</returns>
        public async Task<IEnumerable<Ticket>> GetProjectTickets(int? projectId)
        {
            if (projectId == null) return Enumerable.Empty<Ticket>();

            return await _context.Tickets
                .Where(t => t.Project!.Id == projectId)
                .Include(t => t.TicketPriority)
                .Include(t => t.TicketType)
                .Include(t => t.TicketStatus)
                .Include(t => t.DeveloperUser)
                .Include(t => t.SubmitterUser)
                .ToListAsync();
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
    }
}

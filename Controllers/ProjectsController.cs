using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Chrysalis.Data;
using Chrysalis.Models;
using Chrysalis.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Chrysalis.Enums;
using Chrysalis.Models.ViewModels;

namespace Chrysalis.Controllers
{
    [Authorize]
    public class ProjectsController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<BTUser> _userManager;
        private readonly IProjectService _projectService;
        private readonly ICompanyService _companyService;
        private readonly IFileService _fileService;
        private readonly ITicketService _ticketService;
        private readonly IRoleService _roleService;

        public ProjectsController(ApplicationDbContext context,
            UserManager<BTUser> userManager,
            IProjectService projectService,
            ICompanyService companyService,
            IFileService fileService,
            ITicketService ticketService,
            IRoleService roleService)
        {
            _context = context;
            _userManager = userManager;
            _projectService = projectService;
            _companyService = companyService;
            _fileService = fileService;
            _ticketService = ticketService;
            _roleService = roleService;
        }

        // GET: Projects
        public async Task<IActionResult> Index()
        {
            BTUser? user = await _context.Users
                .Include(u => u.Projects)
                .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));
            IEnumerable<string> userRoles = await _roleService.GetUserRolesAsync(user);

            IEnumerable<Project> model = user!.Projects;

            if (userRoles.Any(r => r.Equals(nameof(BTRoles.Admin))))
            {
                model = await _projectService.GetCompanyProjectsAsync(_companyId);
            }

            return View(model);
        }

        // GET: Projects/Details/5
        public async Task<IActionResult> Details(int? id, string? swalMessage = null)
        {
            ViewBag.SwalMessage = swalMessage;

            if (id == null) return NotFound();

            Project? project = await _projectService
                .GetSingleCompanyProjectAsync(id, _companyId);
            if (project == null) return NotFound();

            return View(project);
        }

        // GET: Projects/Create
        [Authorize(Policy = nameof(BTPolicies.AdPm))]
        public async Task<IActionResult> Create()
        {
            ViewData["ProjectPriorities"] = new SelectList(await _projectService.GetAllProjectPrioritiesAsync(), "Id", "Name");
            return View();
        }

        // POST: Projects/Create
        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = nameof(BTPolicies.AdPm))]
        public async Task<IActionResult> Create([Bind("Name,Description,StartDate,EndDate,ProjectPriorityId,ImageFile")] Project project)
        {
            ModelState.Remove("CompanyId");
            ModelState.Remove("Archived");
            if (ModelState.IsValid)
            {
                BTUser? user = await _userManager.GetUserAsync(User);
                project.CompanyId = user!.CompanyId;

                project.Created = DateTime.Now;
                project.Archived = false;

                if (project.ImageFile != null)
                {
                    project.ImageData = await _fileService.ConvertFileToByteArrayAsync(project.ImageFile);
                    project.ImageType = project.ImageFile.ContentType;
                }

                await _projectService.AddProjectAsync(project);
                return RedirectToAction(nameof(Index));
            }
            ViewData["ProjectPriorityId"] = new SelectList(await _projectService.GetAllProjectPrioritiesAsync(), "Id", "Name", project.ProjectPriorityId);
            return View(project);
        }

        // GET: Projects/Edit/5
        [Authorize(Policy = nameof(BTPolicies.AdPm))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            Project? project = await _projectService
                .GetSingleCompanyProjectAsync(id, _companyId);
            if (project == null) return NotFound();

            ViewData["ProjectPriorityId"] = new SelectList(
                await _projectService.GetAllProjectPrioritiesAsync(),
                    "Id", "Name", project.ProjectPriorityId);
            return View(project);
        }

        // POST: Projects/Edit/5
        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = nameof(BTPolicies.AdPm))]
        public async Task<IActionResult> Edit(int id)
        {
            Project? project = await _projectService
                .GetSingleCompanyProjectAsync(id, _companyId);
            if (project == null) return NotFound();

            // security check
            // TO-DO: Reinstate!
            //if (!project.Members.Contains(user)) return Unauthorized();

            bool validUpdate = await TryUpdateModelAsync(
                project,
                string.Empty,
                p => p.ProjectPriorityId,
                p => p.Name,
                p => p.Description,
                p => p.StartDate,
                p => p.EndDate,
                p => p.Archived,
                p => p.ImageFile);

            if (validUpdate)
            {
                try
                {
                    Project? originalProject = await _projectService.GetProjectAsNoTrackingAsync(
                        id, _companyId);

                    if (project.ImageFile != null)
                    {
                        project.ImageData = await _fileService
                            .ConvertFileToByteArrayAsync(project.ImageFile);
                        project.ImageType = project.ImageFile.ContentType;
                    }

                    // handle project being archived/unarchived
                    if (project.Archived && !originalProject!.Archived)
                    {
                        foreach (Ticket? ticket in project.Tickets)
                        {
                            try
                            {
                                ticket.ArchivedByProject = true;
                                await _ticketService.UpdateTicketAsync(ticket);
                            }
                            catch (Exception)
                            {
                                throw;
                            }
                        }
                    }
                    else if (!project.Archived && originalProject!.Archived)
                    {
                        foreach (Ticket? ticket in project.Tickets)
                        {
                            try
                            {
                                ticket.ArchivedByProject = false;
                                await _ticketService.UpdateTicketAsync(ticket);
                            }
                            catch (Exception)
                            {
                                throw;
                            }
                        }
                    }

                    await _projectService.UpdateProjectAsync(project);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_projectService.ProjectExists(project.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            else
            {
                ViewData["ProjectPriorityId"] = new SelectList(await
                    _projectService.GetAllProjectPrioritiesAsync(),
                    "Id", "Name", project.ProjectPriorityId);
                return View(project);
            }
        }

        // GET: Projects/AssignPM/5
        public async Task<IActionResult> AssignPM(int? projectId)
        {
            if (projectId == null) return NotFound();
            Project? project = await _projectService.GetSingleCompanyProjectAsync(projectId, _companyId);
            if (project == null) return NotFound();

            IEnumerable<BTUser> projectManagers = await _roleService.GetUsersInRoleAsync(nameof(BTRoles.ProjectManager), _companyId);
            BTUser? currentPM = await _projectService.GetProjectManagerAsync(projectId);

            AssignPMViewModel viewModel = new()
            {
                ProjectId = project.Id,
                ProjectName = project.Name,
                PMList = new SelectList(projectManagers, "Id", "FullName", currentPM?.Id),
                PMId = currentPM?.Id,
            };

            return View(viewModel);
        }

        // POST: Projects/AssignPM/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignPM(AssignPMViewModel viewModel)
        {
            if (string.IsNullOrEmpty(viewModel.PMId))
            {
                ModelState.AddModelError("PMId", "You must select a PM.");
                IEnumerable<BTUser> projectManagers = await _roleService.GetUsersInRoleAsync(nameof(BTRoles.ProjectManager), _companyId);
                BTUser? currentPM = await _projectService.GetProjectManagerAsync(viewModel.ProjectId);
                viewModel.PMList = new SelectList(projectManagers, "Id", "FullName", currentPM?.Id);

                return View(viewModel);
            }

            try
            {
                bool success = await _projectService.AddProjectManagerAsync(viewModel.PMId, viewModel.ProjectId);
                if (success) return RedirectToAction(nameof(Details), new { id = viewModel.ProjectId });
                else
                {
                    ModelState.AddModelError("PMId", "Error assigning the chosen PM.");
                    IEnumerable<BTUser> projectManagers = await _roleService.GetUsersInRoleAsync(nameof(BTRoles.ProjectManager), _companyId);
                    BTUser? currentPM = await _projectService.GetProjectManagerAsync(viewModel.ProjectId);
                    viewModel.PMList = new SelectList(projectManagers, "Id", "FullName", currentPM?.Id);

                    return View(viewModel);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        // GET: Projects/AssignMembers
        [Authorize(Policy = nameof(BTPolicies.AdPm))]
        public async Task<IActionResult> AssignMembers(int? id)
        {
            if (id == null) return NotFound();

            Project? project = await _projectService
                .GetSingleCompanyProjectAsync(id, _companyId);
            if (project == null) return NotFound();

            ViewData["Members"] = new MultiSelectList(
                project.Members, "Id", "FullName");
            ViewData["NonMembers"] = new MultiSelectList(
                await _projectService.GetCompanyMembersNotOnProject(id, _companyId),
                "Id", "FullName");

            return View(project);
        }

        // POST: Projects/AssignMembers
        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = nameof(BTPolicies.AdPm))]
        public async Task<IActionResult> AssignMembers(int? id, List<string> selected, List<string> removed)
        {
            if (id == null) return NotFound();

            Project? project = await _projectService
                .GetSingleCompanyProjectAsync(
                    id, _companyId);
            if (project == null) return NotFound();

            bool validUpdate = await TryUpdateModelAsync(
                project,
                string.Empty,
                p => p.Members);

            if (validUpdate)
            {
                try
                {
                    foreach (string memberId in selected)
                    {
                        BTUser? user = await _userManager.FindByIdAsync(memberId);
                        if (user != null) project.Members.Add(user);
                    }
                    foreach (string memberId in removed)
                    {
                        BTUser? user = await _userManager.FindByIdAsync(memberId);
                        if (user != null) project.Members.Remove(user);
                    }
                    await _projectService.UpdateProjectAsync(project);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_projectService.ProjectExists(project.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(AssignMembers), new { id });
            }
            else
            {
                ViewData["Members"] = new MultiSelectList(
                    project.Members, "Id", "FullName");
                ViewData["NonMembers"] = new MultiSelectList(
                    await _projectService.GetCompanyMembersNotOnProject(id, _companyId),
                    "Id", "FullName");

                return View(project);
            }
        }

        [Authorize(Policy = nameof(BTPolicies.AdPm))]
        public async Task<IActionResult> ConfirmArchive(int? projectId)
        {
            Project? model = await _projectService.GetSingleCompanyProjectAsync(projectId, _companyId);
            if (model == null) return NotFound();

            return View(model);
        }

        [Authorize(Policy = nameof(BTPolicies.AdPm))]
        public async Task<IActionResult> ArchiveProject(int? projectId)
        {
            string? swalMessage = string.Empty;

            bool success = await _projectService.ArchiveProjectAsync(projectId, _companyId);

            if (!success) swalMessage = "Error: There was a problem while archiving the project. Please try again.";
            else swalMessage = "Success: The project was archived.";
            return RedirectToAction(nameof(Details), new { id = projectId, swalMessage });
        }

        [Authorize(Policy = nameof(BTPolicies.AdPm))]
        public async Task<IActionResult> UnarchiveProject(int? projectId)
        {
            string? swalMessage = string.Empty;

            bool success = await _projectService.UnarchiveProjectAsync(projectId, _companyId);

            if (!success) swalMessage = "Error: There was a problem while unarchiving the project. Please try again.";
            else swalMessage = "Success: The project was unarchived.";
            return RedirectToAction(nameof(Details), new { id = projectId, swalMessage });
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Chrysalis.Data;
using Chrysalis.Models;
using Chrysalis.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Chrysalis.Enums;
using Chrysalis.Services;
using System.Text;
using System.Text.RegularExpressions;

namespace Chrysalis.Controllers
{
    [Authorize]
    public class ProjectsController : BaseController
    {
        private readonly UserManager<BTUser> _userManager;
        private readonly IProjectService _projectService;
        private readonly ICompanyService _companyService;
        private readonly IFileService _fileService;
        private readonly ITicketService _ticketService;
        private readonly IRoleService _roleService;

        public ProjectsController(UserManager<BTUser> userManager,
            IProjectService projectService,
            ICompanyService companyService,
            IFileService fileService,
            ITicketService ticketService,
            IRoleService roleService)
        {
            _userManager = userManager;
            _projectService = projectService;
            _companyService = companyService;
            _fileService = fileService;
            _ticketService = ticketService;
            _roleService = roleService;
        }

        public async Task<IActionResult> Index()
        {
            BTUser? user = await _userManager.GetUserAsync(User) ?? new BTUser();
            IEnumerable<string> userRoles = await _roleService.GetUserRolesAsync(user);

            if (userRoles.Any(r => r.Equals(nameof(BTRoles.Admin))))
            {
                return RedirectToAction(nameof(AllProjects));
            }
            else return RedirectToAction(nameof(MyProjects));
        }

        public async Task<IActionResult> MyProjects()
        {
            BTUser? user = await _userManager.GetUserAsync(User) ?? new BTUser();
            ViewBag.ActionName = nameof(MyProjects);
            return View(nameof(Index), await _projectService.GetProjectsByUserIdAsync(user.Id, _companyId));
        }

        public async Task<IActionResult> AllProjects()
        {
            ViewBag.ActionName = nameof(AllProjects);
            return View(nameof(Index), await _projectService.GetProjectsAsync(_companyId));
        }

        [Authorize(Roles = nameof(BTRoles.Admin))]
        public async Task<IActionResult> ActiveProjects()
        {
            ViewBag.ActionName = nameof(ActiveProjects);
            return View(nameof(Index), await _projectService.GetActiveProjectsAsync(_companyId));
        }

        [Authorize(Roles = nameof(BTRoles.Admin))]
        public async Task<IActionResult> UnassignedProjects()
        {
            ViewBag.ActionName = nameof(UnassignedProjects);
            return View(nameof(Index), await _projectService.GetUnassignedActiveProjectsAsync(_companyId));
        }

        [Authorize(Roles = nameof(BTRoles.Admin))]
        public async Task<IActionResult> ArchivedProjects()
        {
            ViewBag.ActionName = nameof(ArchivedProjects);
            return View(nameof(Index), await _projectService.GetArchivedProjectsAsync(_companyId));
        }

        // GET: Projects/Details/5
        public async Task<IActionResult> Details(int? id, string? swalMessage = null)
        {
            ViewBag.SwalMessage = swalMessage;

            if (id == null) throw new BadHttpRequestException("Bad input", 400);

            Project? project = await _projectService
                .GetProjectAsync(id, _companyId)
                ?? throw new BadHttpRequestException("No matching project found", 400);

            return View(project);
        }

        // GET: Projects/Create
        [Authorize(Policy = nameof(BTPolicies.AdPm))]
        public async Task<IActionResult> Create()
        {
            IEnumerable<BTUser> projectManagers = await _roleService.GetUsersInRoleAsync(nameof(BTRoles.ProjectManager), _companyId);
            ViewBag.ProjectManagers = new SelectList(projectManagers, "Id", "FullName");

            ViewData["ProjectPriorities"] = new SelectList(await _projectService.GetProjectPrioritiesAsync(), "Id", "Name");
            return View();
        }

        // POST: Projects/Create
        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = nameof(BTPolicies.AdPm))]
        public async Task<IActionResult> Create([Bind("Name,Description,StartDate,EndDate,ProjectPriorityId,ImageFile")] Project project, string? projectManagerId)
        {
            ModelState.Remove("CompanyId");
            ModelState.Remove("Archived");

            if (!ModelState.IsValid)
            {
                ViewData["ProjectPriorities"] = new SelectList(await _projectService.GetProjectPrioritiesAsync(), "Id", "Name");
                return View(project);
            }
            else
            {
                try
                {
                    BTUser? user = await _userManager.GetUserAsync(User);
                    project.CompanyId = user!.CompanyId;

                    project.Created = DateTime.Now;
                    project.Archived = false;

                    // remove excess space around comment due to editor
                    project.Description = Regex.Replace(project.Description!, @"<[^>]*>", string.Empty);

                    if (project.ImageFile != null)
                    {
                        project.ImageData = await _fileService.ConvertFileToByteArrayAsync(project.ImageFile);
                        project.ImageType = project.ImageFile.ContentType;
                    }

                    bool success = await _projectService.AddProjectAsync(project);
                    if (!success) throw new BadHttpRequestException("There was an issue creating the Project", 500);

                    // no PM selected
                    if (projectManagerId?.Equals("Unassigned") == false 
                        && projectManagerId != null) 
                        success = await _projectService.AddProjectManagerAsync(projectManagerId, project.Id);

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        // GET: Projects/Edit/5
        [Authorize(Policy = nameof(BTPolicies.AdPm))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) throw new BadHttpRequestException("Bad input", 400);

            Project? project = await _projectService.GetProjectAsync(id, _companyId)
                ?? throw new BadHttpRequestException("No matching project found", 500);

            // If not project manager for the project
            // OR not an admin
            if (!_userId.Equals((await _projectService.GetProjectManagerAsync(project.Id))?.Id)
                && (!User.IsInRole(nameof(BTRoles.Admin))))
            {
                return Unauthorized();
            }

            ViewData["ProjectPriorities"] = new SelectList(
                await _projectService.GetProjectPrioritiesAsync(),
                    "Id", "Name", project.ProjectPriority);

            IEnumerable<BTUser> projectManagers = await _roleService.GetUsersInRoleAsync(nameof(BTRoles.ProjectManager), _companyId);
            BTUser? currentPM = await _projectService.GetProjectManagerAsync(id);
            ViewBag.ProjectManagers = new SelectList(projectManagers, "Id", "FullName", currentPM.Id);

            return View(project);
        }

        // POST: Projects/Edit/5
        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = nameof(BTPolicies.AdPm))]
        public async Task<IActionResult> Edit(int id, string? projectManagerId)
        {
            Project? project = await _projectService
                .GetProjectAsync(id, _companyId)
                ?? throw new BadHttpRequestException("No matching project found", 400);

            // If not project manager for the project
            // OR not an admin
            if (!_userId.Equals((await _projectService.GetProjectManagerAsync(project.Id))?.Id)
                && (!User.IsInRole(nameof(BTRoles.Admin))))
            {
                return Unauthorized();
            }

            bool validUpdate = await TryUpdateModelAsync(
                project,
                string.Empty,
                p => p.Name,
                p => p.Description,
                p => p.StartDate,
                p => p.EndDate,
                p => p.ProjectPriorityId,
                p => p.ImageFile
                );

            if (!validUpdate)
            {
                ViewData["ProjectPriorities"] = new SelectList(await
                    _projectService.GetProjectPrioritiesAsync(),
                    "Id", "Name", project.ProjectPriority);

                IEnumerable<BTUser> projectManagers = await _roleService.GetUsersInRoleAsync(nameof(BTRoles.ProjectManager), _companyId);
                BTUser? currentPM = await _projectService.GetProjectManagerAsync(id);
                ViewBag.ProjectManagers = new SelectList(projectManagers, "Id", "FullName", currentPM);

                return View(project);
            }
            else
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

                    // remove excess space around comment due to editor
                    project.Description = Regex.Replace(project.Description!, @"<[^>]*>", string.Empty);

                    bool success = await _projectService.UpdateProjectAsync(project);
                    if (!success) throw new BadHttpRequestException("There was an issue editing the Project", 500);

                    // no PM selected
                    if (projectManagerId?.Equals("Unassigned") == false
                        && projectManagerId != null)
                        await _projectService.AddProjectManagerAsync(projectManagerId, project.Id);
                    else if (projectManagerId?.Equals("Unassigned") == true)
                        await _projectService.RemoveProjectManagerAsync(project.Id);

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_projectService.ProjectExists(project.Id)) throw new BadHttpRequestException("Cannot find specified project", 400);
                    else throw new BadHttpRequestException("Issue editing the project", 500);
                }
                catch (Exception)
                {
                    throw new BadHttpRequestException("Issue editing the project", 500);
                }
            }
        }

        // GET: Projects/AssignPM/5
        [Authorize(Roles = nameof(BTRoles.Admin))]
        public async Task<IActionResult> AssignPM(int? projectId, string? swalMessage = null)
        {
            ViewBag.SwalMessage = swalMessage;

            if (projectId == null) throw new BadHttpRequestException("Bad input", 400);
            Project? project = await _projectService.GetProjectAsync(projectId, _companyId)
                ?? throw new BadHttpRequestException("No matching project found", 400);

            IEnumerable<BTUser> projectManagers = await _roleService.GetUsersInRoleAsync(nameof(BTRoles.ProjectManager), _companyId);
            BTUser? currentPM = await _projectService.GetProjectManagerAsync(projectId);
            ViewBag.ProjectManagers = new SelectList(projectManagers, "Id", "FullName", currentPM?.Id);

            return View(project);
        }

        // POST: Projects/AssignPM/5
        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = nameof(BTRoles.Admin))]
        public async Task<IActionResult> AssignPM(int projectId, string? selectedPMId)
        {
            string? swalMessage = string.Empty;

            Project? project = await _projectService.GetProjectAsync(projectId, _companyId)
                ?? throw new BadHttpRequestException("No matching project found", 400);

            if (string.IsNullOrEmpty(selectedPMId)) throw new BadHttpRequestException("You must select an option.", 400);

            try
            {
                // no PM selected
                if (selectedPMId?.Equals("Unassigned") == true)
                {
                    await _projectService.RemoveProjectManagerAsync(projectId);
                    return RedirectToAction(nameof(Index));
                }

                bool success = await _projectService.AddProjectManagerAsync(selectedPMId, projectId);

                if (!success) throw new BadHttpRequestException("There was an issue assigning the chosen PM.", 500);

                swalMessage = "Success: The chosen PM was assigned to the project.";
                return RedirectToAction(nameof(Details), new { id = projectId, swalMessage });
            }
            catch (Exception)
            {
                throw new BadHttpRequestException("There was an issue assigning the chosen PM.", 500);
            }
        }

        // GET: Projects/AssignMembers
        [Authorize(Policy = nameof(BTPolicies.AdPm))]
        public async Task<IActionResult> AssignMembers(int? projectId)
        {
            if (projectId == null) throw new BadHttpRequestException("Bad input", 400);

            Project? project = await _projectService
                .GetProjectAsync(projectId, _companyId)
                ?? throw new BadHttpRequestException("No matching project found", 400);

            // If not project manager for the project
            // OR not an admin
            if (!_userId.Equals((await _projectService.GetProjectManagerAsync(project.Id))?.Id)
                && (!User.IsInRole(nameof(BTRoles.Admin))))
            {
                return Unauthorized();
            }

            return View(project);
        }

        // POST: Projects/AssignMembers
        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = nameof(BTPolicies.AdPm))]
        public async Task<IActionResult> AssignMembers(int? projectId, List<string> developers, List<string> submitters)
        {
            if (projectId == null) throw new BadHttpRequestException("Bad input", 400);

            Project? project = await _projectService
                .GetProjectAsync(projectId, _companyId)
                ?? throw new BadHttpRequestException("No matching project found", 400);

            // If not project manager for the project
            // OR not an admin
            if (!_userId.Equals((await _projectService.GetProjectManagerAsync(project.Id))?.Id)
                && (!User.IsInRole(nameof(BTRoles.Admin))))
            {
                return Unauthorized();
            }

            try
            {
                // get the project manager, then clear membership
                BTUser? projectManager = await _projectService.GetProjectManagerAsync(projectId);
                bool success = await _projectService.RemoveProjectMembersAsync(projectId, _companyId);
                if (!success) throw new BadHttpRequestException("Encountered an issue changing project members", 500);

                if (projectManager != null)
                {
                    // put project manager back
                    success = await _projectService.AddMemberToProjectAsync(projectManager, projectId);
                    if (!success) throw new BadHttpRequestException("Encountered an issue changing project members", 500);
                }

                foreach (string memberId in developers)
                {
                    BTUser? user = await _userManager.FindByIdAsync(memberId);
                    success = user != null ? await _projectService.AddMemberToProjectAsync(user, projectId) : false;
                    if (!success) throw new BadHttpRequestException("Encountered an issue changing project members", 500);
                }
                foreach (string memberId in submitters)
                {
                    BTUser? user = await _userManager.FindByIdAsync(memberId);
                    success = user != null ? await _projectService.AddMemberToProjectAsync(user, projectId) : false;
                    if (!success) throw new BadHttpRequestException("Encountered an issue changing project members", 500);
                }
                success = await _projectService.UpdateProjectAsync(project);
                if (!success) throw new BadHttpRequestException("Encountered an issue changing project members", 500);
                
                return RedirectToAction(nameof(Details), new { id = projectId });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_projectService.ProjectExists(project.Id)) return NotFound();
                throw new BadHttpRequestException("Encountered an issue changing project members", 500);
            }
            catch (Exception)
            {
                throw new BadHttpRequestException("Encountered an issue changing project members", 500);
            }
        }

        [Authorize(Policy = nameof(BTPolicies.AdPm))]
        public async Task<IActionResult> ConfirmArchive(int? projectId)
        {
            Project? model = await _projectService.GetProjectAsync(projectId, _companyId) 
                ?? throw new BadHttpRequestException("Could not find the requested project.", 400);

            // If not project manager for the project
            // OR not an admin
            if (!_userId.Equals((await _projectService.GetProjectManagerAsync(model.Id))?.Id)
                && (!User.IsInRole(nameof(BTRoles.Admin))))
            {
                return Unauthorized();
            }


            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = nameof(BTPolicies.AdPm))]
        public async Task<IActionResult> ConfirmArchive(int projectId)
        {
            string? swalMessage = string.Empty;

            Project? project = await _projectService.GetProjectAsync(projectId, _companyId) 
                ?? throw new BadHttpRequestException("Could not find the requested project.", 400);

            // If not project manager for the project
            // OR not an admin
            if (!_userId.Equals((await _projectService.GetProjectManagerAsync(project.Id))?.Id)
                && (!User.IsInRole(nameof(BTRoles.Admin))))
            {
                return Unauthorized();
            }

            bool success = await _projectService.ArchiveProjectAsync(projectId, _companyId);

            if (!success) throw new BadHttpRequestException("There was a problem while archiving the project.", 400);
            else swalMessage = "Success: The project was archived.";
            return RedirectToAction(nameof(Details), new { id = projectId, swalMessage });
        }

        [Authorize(Policy = nameof(BTPolicies.AdPm))]
        public async Task<IActionResult> ConfirmUnarchive(int? projectId)
        {
            Project? model = await _projectService.GetProjectAsync(projectId, _companyId) 
                ?? throw new BadHttpRequestException("Could not find the requested project.", 400);

            // If not project manager for the project
            // OR not an admin
            if (!_userId.Equals((await _projectService.GetProjectManagerAsync(model.Id))?.Id)
                && (!User.IsInRole(nameof(BTRoles.Admin))))
            {
                return Unauthorized();
            }

            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = nameof(BTPolicies.AdPm))]
        public async Task<IActionResult> ConfirmUnarchive(int projectId)
        {
            string? swalMessage = string.Empty;

            Project? project = await _projectService.GetProjectAsync(projectId, _companyId)
                ?? throw new BadHttpRequestException("Could not find the requested project.", 400);

            // If not project manager for the project
            // OR not an admin
            if (!_userId.Equals((await _projectService.GetProjectManagerAsync(project.Id))?.Id)
                && (!User.IsInRole(nameof(BTRoles.Admin))))
            {
                return Unauthorized();
            }

            bool success = await _projectService.UnarchiveProjectAsync(projectId, _companyId);

            if (!success) throw new BadHttpRequestException("There was a problem while unarchiving the project.", 400);

            swalMessage = "Success: The project was unarchived.";
            return RedirectToAction(nameof(Details), new { id = projectId, swalMessage });
        }

        public IActionResult StringReverse(string input)
        {
            StringBuilder sb = new StringBuilder();

            foreach (char c in input)
            {
                sb.Append(c);
            }
            return View(sb.ToString());
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Chrysalis.Data;
using Chrysalis.Models;
using Chrysalis.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Chrysalis.Extensions;

namespace Chrysalis.Controllers
{
    [Authorize]
    public class ProjectsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<BTUser> _userManager;
        private readonly IProjectService _projectService;
        private readonly ICompanyService _companyService;
        private readonly IFileService _fileService;

        public ProjectsController(ApplicationDbContext context,
            UserManager<BTUser> userManager,
            IProjectService projectService,
            ICompanyService companyService,
            IFileService fileService)
        {
            _context = context;
            _userManager = userManager;
            _projectService = projectService;
            _companyService = companyService;
            _fileService = fileService;
        }

        // GET: Projects
        public async Task<IActionResult> Index()
        {
            Company? company = await _companyService.GetCompanyById(User.Identity!.GetCompanyId());

            return View(company.Projects);
        }

        // GET: Projects/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            Project? project = await _projectService
                .GetSingleCompanyProjectAsync(id, User.Identity!.GetCompanyId());
            if (project == null) return NotFound();

            return View(project);
        }

        // GET: Projects/Members
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Members(int? id)
        {
            if (id == null) return NotFound();

            Project? project = await _projectService
                .GetSingleCompanyProjectAsync(id, User.Identity!.GetCompanyId());
            if (project == null) return NotFound();

            ViewData["Members"] = new MultiSelectList(
                project.Members, "Id", "FullName");
            ViewData["NonMembers"] = new MultiSelectList(
                await _projectService.GetCompanyMembersNotOnProject(id, User.Identity!.GetCompanyId()),
                "Id", "FullName");

            return View(project);
        }

        // POST: Projects/Members
        [HttpPost,ValidateAntiForgeryToken, Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Members(int? id, List<string> selected, List<string> removed)
        {
            if (id == null) return NotFound();

            Project? project = await _projectService
                .GetSingleCompanyProjectAsync(
                    id, User.Identity!.GetCompanyId());
            if (project == null) return NotFound();

            bool validUpdate = await TryUpdateModelAsync(
                project,
                string.Empty,
                p => p.Members);

            if (validUpdate)
            {
                try
                {
                    foreach(string memberId in selected)
                    {
                        BTUser? user = await _userManager.FindByIdAsync(memberId);
                        if (user != null) project.Members.Add(user);
                    }
                    foreach(string memberId in removed)
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
                return RedirectToAction(nameof(Members), new { id });
            }
            else
            {
                ViewData["Members"] = new MultiSelectList(
                    project.Members, "Id", "FullName");
                ViewData["NonMembers"] = new MultiSelectList(
                    await _projectService.GetCompanyMembersNotOnProject(id, User.Identity!.GetCompanyId()),
                    "Id", "FullName");

                return View(project);
            }
        }

        // GET: Projects/Create
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Create()
        {
            ViewData["ProjectPriorities"] = new SelectList(await _projectService.GetAllProjectPrioritiesAsync(), "Id", "Name");
            return View();
        }

        // POST: Projects/Create
        [HttpPost,ValidateAntiForgeryToken, Authorize(Roles = "Admin,ProjectManager")]
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
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            Project? project = await _projectService
                .GetSingleCompanyProjectAsync(id, User.Identity!.GetCompanyId());
            if (project == null) return NotFound();

            ViewData["ProjectPriorityId"] = new SelectList(
                await _projectService.GetAllProjectPrioritiesAsync(), 
                    "Id", "Name", project.ProjectPriorityId);
            return View(project);
        }

        // POST: Projects/Edit/5
        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Edit(int id)
        {
            Project? project = await _projectService
                .GetSingleCompanyProjectAsync(id, 
                    User.Identity!.GetCompanyId());
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
                    if (project.ImageFile != null)
                    {
                        project.ImageData = await _fileService
                            .ConvertFileToByteArrayAsync(project.ImageFile);
                        project.ImageType = project.ImageFile.ContentType;
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

        // GET: Projects/Delete/5
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Projects == null)
            {
                return NotFound();
            }

            var project = await _context.Projects
                .Include(p => p.Company)
                .Include(p => p.ProjectPriority)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // POST: Projects/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Projects == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Projects'  is null.");
            }
            var project = await _context.Projects.FindAsync(id);
            if (project != null)
            {
                _context.Projects.Remove(project);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}

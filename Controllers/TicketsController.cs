using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Chrysalis.Data;
using Chrysalis.Models;
using Chrysalis.Services.Interfaces;
using Chrysalis.Enums;
using Chrysalis.Extensions;
using Chrysalis.Services;

namespace Chrysalis.Controllers
{
    [Authorize]
    public class TicketsController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<BTUser> _userManager;
        private readonly ICompanyService _companyService;
        private readonly ITicketService _ticketService;
        private readonly IRoleService _roleService;
        private readonly IFileService _fileService;
        private readonly IProjectService _projectService;
        private readonly ITicketHistoryService _historyService;
        private readonly INotificationService _notificationService;

        public TicketsController(ApplicationDbContext context,
            UserManager<BTUser> userManager,
            ICompanyService companyService,
            ITicketService ticketService,
            IRoleService roleService,
            IFileService fileService,
            IProjectService projectService,
            ITicketHistoryService historyService,
            INotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _companyService = companyService;
            _ticketService = ticketService;
            _roleService = roleService;
            _fileService = fileService;
            _projectService = projectService;
            _historyService = historyService;
            _notificationService = notificationService;
        }

        // GET: Tickets
        public async Task<IActionResult> Index()
        {
            Company? company = await _companyService.GetCompanyByIdAsync(_companyId);

            IEnumerable<Ticket> tickets = company.Projects
                .SelectMany(p => p.Tickets);

            return View(tickets);
        }

        // GET: Tickets/Details/5
        public async Task<IActionResult> Details(int? id, string? statusMessage = null)
        {
            if (id == null) return NotFound();
            ViewData["StatusMessage"] = statusMessage;

            Ticket? ticket = await _ticketService
                .GetTicketByIdAsync(id, _companyId)
                ?? throw new BadHttpRequestException("Cannot find specified ticket");

            return View(ticket);
        }

        // GET: Tickets/Create
        [Authorize(Policy = nameof(BTPolicies.NoDemo))]
        public async Task<IActionResult> Create(int? projectId)
        {
            ViewBag.ProjectId = projectId;
            ViewData["TicketTypes"] = new SelectList(await _ticketService.GetTicketTypesAsync(), "Id", "Name");
            ViewData["TicketPriorities"] = new SelectList(await _ticketService.GetTicketPrioritiesAsync(), "Id", "Name");
            ViewData["DeveloperUsers"] = new SelectList(await _projectService.GetProjectDevelopersAsync(projectId), "Id", "FullName");
            return View();
        }

        // POST: Tickets/Create
        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = nameof(BTPolicies.NoDemo))]
        public async Task<IActionResult> Create([Bind("ProjectId,Title,Description,TicketTypeId,TicketPriorityId,DeveloperUserId")] Ticket ticket)
        {
            Project? project = await _projectService.GetProjectAsync(ticket.ProjectId, _companyId)
                ?? throw new BadHttpRequestException("Cannot find specified project", 400);
            BTUser? currentUser = await _userManager.GetUserAsync(User);
            // check if user is not an admin or on the project
            if (!project.Members.Any(u => u.Id == currentUser?.Id)
                && !(await _roleService.IsUserInRoleAsync(currentUser, nameof(BTRoles.Admin))))
                throw new BadHttpRequestException("You are not on the specified project", 400);

            ModelState.Remove("SubmitterUserId");
            ModelState.Remove("TicketStatusId");
            ModelState.Remove("Created");
            ModelState.Remove("Archived");
            ModelState.Remove("ArchivedByProject");

            if (!ModelState.IsValid)
            {
                ViewBag.ProjectId = ticket.ProjectId;
                ViewData["TicketTypes"] = new SelectList(await _ticketService.GetTicketTypesAsync(), "Id", "Name");
                ViewData["TicketPriorities"] = new SelectList(await _ticketService.GetTicketPrioritiesAsync(), "Id", "Name");
                ViewData["DeveloperUsers"] = new SelectList(await _projectService.GetProjectDevelopersAsync(ticket.ProjectId), "Id", "FullName");
                return View(ticket);
            }
            else
            {
                try
                {
                    ticket.SubmitterUser = await _userManager.GetUserAsync(User);
                    ticket.Created = DateTime.Now;
                    ticket.Archived = false;
                    ticket.ArchivedByProject = false;

                    // attempt to get and assign ID of TicketStatus by the name of New
                    // if failure, assign 0 to catch exception
                    int? ticketStatusId = await _ticketService.GetTicketStatusByIdAsync(BTTicketStatuses.New) ?? 0;
                    ticket.TicketStatusId = ticketStatusId.Value;
                    await _ticketService.AddTicketAsync(ticket);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_ticketService.TicketExists(ticket.Id)) throw new BadHttpRequestException("Cannot find specified ticket", 400);
                    else throw new BadHttpRequestException("Problem creating ticket", 500);
                }
                catch (InvalidOperationException)
                {
                    if (!_ticketService.TicketExists(ticket.Id)) throw new BadHttpRequestException("Cannot find specified ticket", 400);
                    else throw new BadHttpRequestException("Problem creating ticket", 500);
                }
                catch (Exception)
                {
                    throw new BadHttpRequestException("Problem creating ticket", 500);
                }

                // add history and send notification
                await _historyService.AddHistoryAsync(null, ticket, _userManager.GetUserId(User));
                // TO-DO: test once notifications are online
                await _notificationService.NewTicketNotificationAsync(ticket.Id, _userManager.GetUserId(User));

                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Tickets/Edit/5
        [Authorize(Roles = nameof(BTRoles.Admin))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) throw new BadHttpRequestException("Bad input", 400);

            Ticket? ticket = await _ticketService.GetTicketByIdAsync(id, _companyId)
                ?? throw new BadHttpRequestException("Cannot find specified ticket");

            ViewData["TicketTypes"] = new SelectList(await _ticketService.GetTicketTypesAsync(), "Id", "Name");
            ViewData["TicketStatuses"] = new SelectList(await _ticketService.GetTicketStatusesAsync(), "Id", "Name");
            ViewData["TicketPriorities"] = new SelectList(await _ticketService.GetTicketPrioritiesAsync(), "Id", "Name");
            return View(ticket);
        }

        // POST: Tickets/Edit/5
        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = nameof(BTRoles.Admin))]
        public async Task<IActionResult> Edit(int id)
        {
            Ticket? oldTicket = await _ticketService.GetTicketByIdAsNoTrackingAsync(id, _companyId)
                ?? throw new BadHttpRequestException("Problem finding specified ticket", 400);

            bool validUpdate = await TryUpdateModelAsync(
                oldTicket,
                string.Empty,
                t => t.Title,
                t => t.Description,
                t => t.TicketTypeId,
                t => t.TicketPriorityId,
                t => t.TicketStatusId
                );

            if (!validUpdate)
            {
                ViewData["TicketTypes"] = new SelectList(await _ticketService.GetTicketTypesAsync(), "Id", "Name");
                ViewData["TicketStatuses"] = new SelectList(await _ticketService.GetTicketStatusesAsync(), "Id", "Name");
                ViewData["TicketPriorities"] = new SelectList(await _ticketService.GetTicketPrioritiesAsync(), "Id", "Name");
                return View(oldTicket);
            }
            else
            {
                try
                {
                    oldTicket.Updated = DateTime.Now;
                    bool success = await _ticketService.UpdateTicketAsync(oldTicket);
                    if (!success) throw new BadHttpRequestException("Problem updating ticket", 500);

                    // add history and send notification
                    // TO-DO: add bool checks for success here
                    Ticket? newTicket = await _ticketService.GetTicketByIdAsNoTrackingAsync(id, _companyId);
                    await _historyService.AddHistoryAsync(oldTicket, newTicket, _userManager.GetUserId(User));
                    await _notificationService.TicketUpdateNotificationAsync(id, _userManager.GetUserId(User));

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_ticketService.TicketExists(oldTicket.Id)) throw new BadHttpRequestException("Cannot find specified ticket", 400);
                    else throw new BadHttpRequestException("Problem updating ticket", 500);
                }
                catch (Exception)
                {
                    throw new BadHttpRequestException("Problem updating ticket", 500);
                }
            }
        }

        [Authorize(Policy = nameof(BTPolicies.AdPm))]
        public async Task<IActionResult> AssignDeveloper(int? ticketId)
        {
            if (ticketId == null) throw new BadHttpRequestException("You must pass a ticket ID", 400);
            Ticket? ticket = await _ticketService.GetTicketByIdAsync(ticketId, _companyId)
                ?? throw new BadHttpRequestException("Cannot find specified ticket", 400);

            if (ticket.DeveloperUser == null)
                ViewBag.Developers = new SelectList(await _projectService.GetProjectDevelopersAsync(ticket.ProjectId), "Id", "Name");
            else
                ViewBag.Developers = new SelectList(await _projectService.GetProjectDevelopersAsync(ticket.ProjectId), "Id", "Name", ticket.DeveloperUser);

            return View(ticket);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = nameof(BTPolicies.AdPm))]
        public async Task<IActionResult> AssignDeveloper(int ticketId, string selected)
        {
            Ticket? ticket = await _ticketService.GetTicketByIdAsync(ticketId, _companyId)
                ?? throw new BadHttpRequestException("Cannot find specified ticket", 400);

            ticket.DeveloperUserId = selected;
            bool success = await _ticketService.UpdateTicketAsync(ticket);
            if (!success) throw new BadHttpRequestException("There was an issue assigning the devolper", 500);
            return RedirectToAction("Details", new { id = ticketId });
        }

        [Authorize(Policy = nameof(BTPolicies.AdPmDev))]
        public async Task<IActionResult> AddTicketComment(int? id, string? comment)
        {
            if (id == null) throw new BadHttpRequestException("Bad input", 400);
            if (comment == null) return View(nameof(Details), id);

            // TO-DO: security check

            Ticket? ticket = await _ticketService.GetTicketByIdAsync(id, _companyId)
                ?? throw new BadHttpRequestException("Cannot find specified ticket", 400);

            try
            {
                TicketComment ticketComment = new()
                {
                    TicketId = ticket.Id,
                    UserId = _userManager.GetUserId(User),
                    Comment = comment,
                    Created = DateTime.Now
                };

                ticket.Comments.Add(ticketComment);
                bool success = await _ticketService.UpdateTicketAsync(ticket);
                if (!success) throw new BadHttpRequestException("Problem adding comment", 500);

                return RedirectToAction(nameof(Details), new { id, statusMessage = "Success: Comment added" });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_ticketService.TicketExists(ticket.Id)) throw new BadHttpRequestException("Cannot find specified ticket");
                else throw new BadHttpRequestException("Problem adding comment", 500);
            }
            catch (Exception)
            {
                throw new BadHttpRequestException("Problem adding comment", 500);
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Policy = nameof(BTPolicies.AdPmDev))]
        public async Task<IActionResult> AddTicketAttachment([Bind("FormFile,Description,TicketId")] TicketAttachment ticketAttachment)
        {
            string statusMessage;

            ModelState.Remove("UserId");

            if (ticketAttachment.FormFile == null) throw new BadHttpRequestException("Must include a file", 400);
            else if (!ModelState.IsValid) throw new BadHttpRequestException("File is too large. Max size is 1MB.", 400);

            ticketAttachment.FileData = await _fileService.ConvertFileToByteArrayAsync(ticketAttachment.FormFile);
            ticketAttachment.FileName = ticketAttachment.FormFile.FileName;
            ticketAttachment.FileType = ticketAttachment.FormFile.ContentType;

            ticketAttachment.Created = DateTime.Now;
            ticketAttachment.UserId = _userManager.GetUserId(User);

            await _ticketService.AddTicketAttachmentAsync(ticketAttachment);
            statusMessage = "Success: New attachment added to Ticket.";

            return RedirectToAction("Details", new { id = ticketAttachment.TicketId, statusMessage });
        }

        [Authorize]
        public async Task<IActionResult> ShowFile(int id)
        {
            TicketAttachment? ticketAttachment = await _ticketService.GetTicketAttachmentByIdAsync(id)
                ?? throw new BadHttpRequestException("Cannot find the requested attachment", 400);

            string fileName = ticketAttachment.FileName!;
            byte[] fileData = ticketAttachment.FileData!;
            string ext = Path.GetExtension(fileName)!.Replace(".", "");

            Response.Headers.Add("Content-Disposition", $"inline; filename={fileName}");
            return File(fileData, $"application/{ext}");
        }
    }
}
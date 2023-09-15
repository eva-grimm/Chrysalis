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
                .GetTicketAsync(id, _companyId);
            if (ticket == null) return NotFound();

            return View(ticket);
        }

        // GET: Tickets/Create
        [Authorize(Policy = nameof(BTPolicies.NoDemo))]
        public async Task<IActionResult> Create()
        {
            ViewData["CompanyProjects"] = new SelectList(await _projectService.GetCompanyProjectsAsync(_companyId), "Id", "Name");
            ViewData["TicketTypes"] = new SelectList(await _ticketService.GetAllTicketTypes(), "Id", "Name");
            ViewData["TicketPriorities"] = new SelectList(await _ticketService.GetAllTicketPriorities(), "Id", "Name");
            ViewData["DeveloperUsers"] = new SelectList(await _roleService.GetUsersInRoleAsync(BTRoles.Developer.ToString(), _companyId), "Id", "FullName");
            return View();
        }

        // POST: Tickets/Create
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Policy = nameof(BTPolicies.NoDemo))]
        public async Task<IActionResult> Create([Bind("ProjectId,TicketTypeId,TicketPriorityId,DeveloperUserId,Title,Description")] Ticket ticket)
        {
            ModelState.Remove("SubmitterUserId");
            ModelState.Remove("TicketStatusId");
            ModelState.Remove("Created");
            ModelState.Remove("Archived");
            ModelState.Remove("ArchivedByProject");

            if (ModelState.IsValid)
            {
                try
                {
                    ticket.SubmitterUser = await _userManager.GetUserAsync(User);
                    ticket.Created = DateTime.Now;
                    ticket.Archived = false;
                    ticket.ArchivedByProject = false;

                    // attempt to get and assign ID of TicketStatus by the name of New
                    // if failure, assign 0 to catch exception
                    int? ticketStatusId = await _ticketService.GetTicketStatusIdAsync(BTTicketStatuses.New) ?? 0;
                    ticket.TicketStatusId = ticketStatusId.Value;
                    await _ticketService.AddTicketAsync(ticket);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_ticketService.TicketExists(ticket.Id)) return NotFound();
                    else throw;
                }
                catch (InvalidOperationException)
                {
                    if (!_ticketService.TicketExists(ticket.Id)) return NotFound();
                    else throw;
                }

                // add history and send notification
                await _historyService.AddHistoryAsync(null, ticket, _userManager.GetUserId(User));
                // TO-DO: test once notifications are online
                await _notificationService.NewTicketNotificationAsync(ticket.Id, _userManager.GetUserId(User));

                return RedirectToAction(nameof(Index));
            }
            else
            {
                ViewData["CompanyProjects"] = new SelectList(await _projectService.GetCompanyProjectsAsync(_companyId), "Id", "Name");
                ViewData["TicketTypes"] = new SelectList(await _ticketService.GetAllTicketTypes(), "Id", "Name");
                ViewData["TicketPriorities"] = new SelectList(await _ticketService.GetAllTicketPriorities(), "Id", "Name");
                ViewData["DeveloperUsers"] = new SelectList(await _roleService.GetUsersInRoleAsync(BTRoles.Developer.ToString(), _companyId), "Id", "FullName");
                return View(ticket);
            }
        }

        // GET: Tickets/Edit/5
        [Authorize(Policy = nameof(BTPolicies.AdPmDev))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            Ticket? ticket = await _ticketService.GetTicketAsync(id, _companyId);
            if (ticket == null) return NotFound();

            ViewData["TicketTypes"] = new SelectList(await _ticketService.GetAllTicketTypes(), "Id", "Name");
            ViewData["TicketStatuses"] = new SelectList(await _ticketService.GetAllTicketStatuses(), "Id", "Name");
            ViewData["TicketPriorities"] = new SelectList(await _ticketService.GetAllTicketPriorities(), "Id", "Name");
            ViewData["DeveloperUsers"] = new SelectList(await _roleService.GetUsersInRoleAsync(BTRoles.Developer.ToString(), _companyId), "Id", "FullName");
            return View(ticket);
        }

        // POST: Tickets/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Policy = nameof(BTPolicies.AdPmDev))]
        public async Task<IActionResult> Edit(int id)
        {
            // TO-DO: security check
            // who should be allowed to edit the ticket?
            Ticket? oldTicket = await _ticketService.GetTicketAsNoTrackingAsync(id, _companyId);
            if (oldTicket == null) return NotFound();

            bool validUpdate = await TryUpdateModelAsync(
                oldTicket,
                string.Empty,
                t => t.TicketTypeId,
                t => t.TicketStatusId,
                t => t.TicketPriorityId,
                t => t.DeveloperUserId,
                t => t.Title,
                t => t.Description,
                t => t.Archived
                );

            if (validUpdate)
            {
                // attempt to update ticket
                try
                {
                    oldTicket.Updated = DateTime.Now;
                    await _ticketService.UpdateTicketAsync(oldTicket);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_ticketService.TicketExists(oldTicket.Id)) return NotFound();
                    else throw;
                }

                // add history and send notification
                Ticket? newTicket = await _ticketService.GetTicketAsNoTrackingAsync(id, _companyId);
                await _historyService.AddHistoryAsync(oldTicket, newTicket, _userManager.GetUserId(User));
                await _notificationService.TicketUpdateNotificationAsync(id, _userManager.GetUserId(User));

                return RedirectToAction(nameof(Index));
            }
            else
            {
                ViewData["TicketTypes"] = new SelectList(await _ticketService.GetAllTicketTypes(), "Id", "Name");
                ViewData["TicketStatuses"] = new SelectList(await _ticketService.GetAllTicketStatuses(), "Id", "Name");
                ViewData["TicketPriorities"] = new SelectList(await _ticketService.GetAllTicketPriorities(), "Id", "Name");
                ViewData["DeveloperUsers"] = new SelectList(await _roleService.GetUsersInRoleAsync(BTRoles.Developer.ToString(), _companyId), "Id", "FullName");
                return View(oldTicket);
            }
        }

        [Authorize(Policy = nameof(BTPolicies.AdPmDev))]
        public async Task<IActionResult> AddTicketComment(int? id, string? comment)
        {
            if (id == null) return NotFound();
            if (comment == null) return View(nameof(Details), id);
            // TO-DO: security check
            // who should be allowed to edit the ticket?

            Ticket? ticket = await _ticketService.GetTicketAsync(id, _companyId);
            if (ticket == null) return NotFound();

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
                await _ticketService.UpdateTicketAsync(ticket);
            }
            catch (DbUpdateConcurrencyException)
            {
                // sweet alert that cannot add comment
                if (!_ticketService.TicketExists(ticket.Id)) return NotFound();
                else throw;
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost,ValidateAntiForgeryToken]
        [Authorize(Policy = nameof(BTPolicies.AdPmDev))]
        public async Task<IActionResult> AddTicketAttachment([Bind("FormFile,Description,TicketId")] TicketAttachment ticketAttachment)
        {
            string statusMessage;

            ModelState.Remove("UserId");

            if (ModelState.IsValid && ticketAttachment.FormFile != null)
            {
                ticketAttachment.FileData = await _fileService.ConvertFileToByteArrayAsync(ticketAttachment.FormFile);
                ticketAttachment.FileName = ticketAttachment.FormFile.FileName;
                ticketAttachment.FileType = ticketAttachment.FormFile.ContentType;

                ticketAttachment.Created = DateTime.Now;
                ticketAttachment.UserId = _userManager.GetUserId(User);

                await _ticketService.AddTicketAttachmentAsync(ticketAttachment);
                statusMessage = "Success: New attachment added to Ticket.";
            }
            else if (!ModelState.IsValid && ticketAttachment.FormFile != null)
            {
                statusMessage = "Error: File too large. Max size is 1MB.";
            }
            else
            {
                statusMessage = "Error: Invalid data.";
            }

            return RedirectToAction("Details", new { id = ticketAttachment.TicketId, statusMessage });
        }

        [Authorize]
        public async Task<IActionResult> ShowFile(int id)
        {
            TicketAttachment? ticketAttachment = await _ticketService.GetTicketAttachmentByIdAsync(id);
            if (ticketAttachment == null) return NotFound();

            string fileName = ticketAttachment.FileName!;
            byte[] fileData = ticketAttachment.FileData!;
            string ext = Path.GetExtension(fileName)!.Replace(".", "");

            Response.Headers.Add("Content-Disposition", $"inline; filename={fileName}");
            return File(fileData, $"application/{ext}");
        }
    }
}

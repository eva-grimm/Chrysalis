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
using System.Text.RegularExpressions;

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
            BTUser? user = await _userManager.GetUserAsync(User) ?? new BTUser();
            IEnumerable<string> userRoles = await _roleService.GetUserRolesAsync(user);

            if (userRoles.Any(r => r.Equals(nameof(BTRoles.Admin))))
            {
                return RedirectToAction(nameof(AllTickets));
            }
            else return RedirectToAction(nameof(MyTickets));
        }

        public async Task<IActionResult> MyTickets()
        {
            BTUser? user = await _userManager.GetUserAsync(User) ?? new BTUser();
            ViewBag.ActionName = nameof(MyTickets);
            return View(nameof(Index), await _ticketService.GetTicketsByUserIdAsync(user.Id, _companyId));
        }

        [Authorize(Roles = nameof(BTRoles.Admin))]
        public async Task<IActionResult> AllTickets()
        {
            ViewBag.ActionName = nameof(AllTickets);
            return View(nameof(Index), await _ticketService.GetTicketsAsync(_companyId));
        }

        [Authorize(Roles = nameof(BTRoles.Admin))]
        public async Task<IActionResult> ActiveTickets()
        {
            ViewBag.ActionName = nameof(ActiveTickets);
            return View(nameof(Index), await _ticketService.GetActiveTicketsAsync(_companyId));
        }

        [Authorize(Roles = nameof(BTRoles.Admin))]
        public async Task<IActionResult> UnassignedTickets()
        {
            ViewBag.ActionName = nameof(UnassignedTickets);
            return View(nameof(Index), await _ticketService.GetUnassignedActiveTicketsAsync(_companyId));
        }

        [Authorize(Roles = nameof(BTRoles.Admin))]
        public async Task<IActionResult> ArchivedTickets()
        {
            ViewBag.ActionName = nameof(ArchivedTickets);
            return View(nameof(Index), await _ticketService.GetArchivedTicketsAsync(_companyId));
        }

        // GET: Tickets/Details/5
        public async Task<IActionResult> Details(int? ticketId, string? statusMessage = null)
        {
            if (ticketId == null) return NotFound();
            ViewData["StatusMessage"] = statusMessage;

            Ticket? ticket = await _ticketService
                .GetTicketByIdAsync(ticketId, _companyId)
                ?? throw new BadHttpRequestException("Cannot find specified ticket");

            ViewBag.TicketPriorities = new SelectList(await _ticketService.GetTicketPrioritiesAsync(), "Id", "Name", ticket.TicketPriorityId);
            ViewBag.TicketStatuses = new SelectList(await _ticketService.GetTicketStatusesAsync(), "Id", "Name", ticket.TicketStatusId);
            ViewBag.TicketTypes = new SelectList(await _ticketService.GetTicketTypesAsync(), "Id", "Name", ticket.TicketTypeId);
            ViewBag.Developers = new SelectList(await _projectService.GetProjectDevelopersAsync(ticket.ProjectId), "Id", "FullName", ticket.DeveloperUserId);

            return View(ticket);
        }

        // GET: Tickets/Create
        [Authorize(Policy = nameof(BTPolicies.NoDemo))]
        public async Task<IActionResult> Create(int? projectId)
        {
            Project? project = await _projectService.GetProjectAsync(projectId, _companyId)
                ?? throw new BadHttpRequestException("Cannot find specified project", 400);
            BTUser? currentUser = await _userManager.GetUserAsync(User);

            // check if user is not on the project and not an admin
            if (!project.Members.Any(u => u.Id == currentUser?.Id)
                && !await _roleService.IsUserInRoleAsync(currentUser, nameof(BTRoles.Admin)))
                throw new BadHttpRequestException("You are not on the specified project", 400);

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

            // check if user is not on the project and not an admin
            if (!project.Members.Any(u => u.Id == currentUser?.Id)
                && !await _roleService.IsUserInRoleAsync(currentUser, nameof(BTRoles.Admin)))
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

                    // remove excess space around comment due to editor
                    ticket.Description = Regex.Replace(ticket.Description!, @"<[^>]*>", string.Empty);

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
                await _notificationService.NewTicketNotificationAsync(ticket.Id, _userManager.GetUserId(User));

                return RedirectToAction("Details", "Projects", new { id = ticket.ProjectId });
            }
        }

        // GET: Tickets/Edit/5
        public async Task<IActionResult> Edit(int? ticketId)
        {
            if (ticketId == null) throw new BadHttpRequestException("Bad input", 400);

            Ticket? ticket = await _ticketService.GetTicketByIdAsync(ticketId, _companyId)
            ?? throw new BadHttpRequestException("Cannot find specified ticket");

            /* verify admin
            OR project manager for the ticket's project
            OR developer and assigned to the ticket
            OR submitter and submitted the ticket */
            BTUser? currentUser = await _userManager.GetUserAsync(User);
            BTRoles currentUserRole = await _roleService.GetActiveUserRoleAsync(currentUser!.Id);

            if ((currentUserRole == BTRoles.ProjectManager
                && await _projectService.GetProjectManagerAsync(ticket.ProjectId) != currentUser)
            || (currentUserRole == BTRoles.Developer
                && !ticket.DeveloperUserId!.Equals(currentUser.Id))
            || (currentUserRole == BTRoles.Submitter
                && !ticket.SubmitterUserId!.Equals(currentUser.Id)))
            {
                return Unauthorized();
            }

            return View(ticket);
        }

        // POST: Tickets/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int ticketId)
        {
            Ticket? oldTicket = await _ticketService.GetTicketByIdAsNoTrackingAsync(ticketId, _companyId)
                ?? throw new BadHttpRequestException("Problem finding specified ticket", 400);

            /* verify admin
            OR project manager for the ticket's project
            OR developer and assigned to the ticket
            OR submitter and submitted the ticket */
            BTUser? currentUser = await _userManager.GetUserAsync(User);
            BTRoles currentUserRole = await _roleService.GetActiveUserRoleAsync(currentUser!.Id);

            if ((currentUserRole == BTRoles.ProjectManager
                && await _projectService.GetProjectManagerAsync(oldTicket.ProjectId) != currentUser)
            || (currentUserRole == BTRoles.Developer
                && !oldTicket.DeveloperUserId!.Equals(currentUser.Id))
            || (currentUserRole == BTRoles.Submitter
                && !oldTicket.SubmitterUserId!.Equals(currentUser.Id)))
            {
                return Unauthorized();
            }

            bool validUpdate = await TryUpdateModelAsync(
            oldTicket,
            string.Empty,
            t => t.Title,
            t => t.Description
            );

            if (!validUpdate) return View(oldTicket);
            else
            {
                try
                {
                    oldTicket.Updated = DateTime.Now;
                    bool success = await _ticketService.UpdateTicketAsync(oldTicket);
                    if (!success) throw new BadHttpRequestException("Problem updating ticket", 500);

                    oldTicket.Description = Regex.Replace(oldTicket.Description!, @"<[^>]*>", string.Empty);

                    // add history and send notification
                    // TO-DO: add bool checks for success here
                    Ticket? newTicket = await _ticketService.GetTicketByIdAsNoTrackingAsync(ticketId, _companyId);
                    await _historyService.AddHistoryAsync(oldTicket, newTicket, _userManager.GetUserId(User));
                    await _notificationService.TicketUpdateNotificationAsync(ticketId, _userManager.GetUserId(User));
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

                // add history and send notification
                await _historyService.AddHistoryAsync(null, oldTicket, _userManager.GetUserId(User));
                // TO-DO: test once notifications are online
                await _notificationService.NewTicketNotificationAsync(oldTicket.Id, _userManager.GetUserId(User));

                return RedirectToAction(nameof(Details), new { ticketId });
            }
        }

        public async Task<IActionResult> AssignDeveloper(int? ticketId)
        {
            if (ticketId == null) throw new BadHttpRequestException("Bad input", 400);

            Ticket? ticket = await _ticketService.GetTicketByIdAsync(ticketId, _companyId)
            ?? throw new BadHttpRequestException("Cannot find specified ticket");

            /* verify admin
            OR project manager for the ticket's project 
            NOT developer or submitter */
            BTUser? currentUser = await _userManager.GetUserAsync(User);
            BTRoles currentUserRole = await _roleService.GetActiveUserRoleAsync(currentUser!.Id);
            if (currentUserRole == BTRoles.ProjectManager
                && ticket.Project!.Members.Any(m => m.Id.Equals(currentUser.Id))
            || (currentUserRole == BTRoles.Developer)
            || (currentUserRole == BTRoles.Submitter))
            {
                return Unauthorized();
            }

            ViewBag.Developers = new SelectList(await _projectService.GetProjectDevelopersAsync(ticket.ProjectId), "Id", "FullName", ticket.DeveloperUserId);

            return View(ticket);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = nameof(BTPolicies.AdPm))]
        public async Task<IActionResult> AssignDeveloper(int ticketId, string selected)
        {
            Ticket? oldTicket = await _ticketService.GetTicketByIdAsNoTrackingAsync(ticketId, _companyId)
                ?? throw new BadHttpRequestException("Problem finding specified ticket", 400);

            /* verify admin
            OR project manager for the ticket's project 
            NOT developer or submitter */
            BTUser? currentUser = await _userManager.GetUserAsync(User);
            BTRoles currentUserRole = await _roleService.GetActiveUserRoleAsync(currentUser!.Id);
            if ((currentUserRole == BTRoles.ProjectManager
                && await _projectService.GetProjectManagerAsync(oldTicket.ProjectId) != currentUser)
            || (currentUserRole == BTRoles.Developer)
            || (currentUserRole == BTRoles.Submitter))
            {
                return Unauthorized();
            }

            oldTicket.Updated = DateTime.Now;
            oldTicket.DeveloperUserId = selected;
            bool success = await _ticketService.UpdateTicketAsync(oldTicket);
            if (!success) throw new BadHttpRequestException("There was an issue assigning the developer", 500);

            // add history and send notification
            // TO-DO: add bool checks for success here
            Ticket? newTicket = await _ticketService.GetTicketByIdAsNoTrackingAsync(ticketId, _companyId);
            await _historyService.AddHistoryAsync(oldTicket, newTicket, _userManager.GetUserId(User));
            await _notificationService.TicketUpdateNotificationAsync(ticketId, _userManager.GetUserId(User));

            return RedirectToAction("Details", new { ticketId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePriority(int ticketId, int selected)
        {
            Ticket? oldTicket = await _ticketService.GetTicketByIdAsNoTrackingAsync(ticketId, _companyId)
                ?? throw new BadHttpRequestException("Problem finding specified ticket", 400);

            /* verify admin
            OR project manager for the ticket's project
            OR developer and assigned to the ticket
            OR submitter and submitted the ticket */
            BTUser? currentUser = await _userManager.GetUserAsync(User);
            BTRoles currentUserRole = await _roleService.GetActiveUserRoleAsync(currentUser!.Id);

            if ((currentUserRole == BTRoles.ProjectManager
                && await _projectService.GetProjectManagerAsync(oldTicket.ProjectId) != currentUser)
            || (currentUserRole == BTRoles.Developer
                && !oldTicket.DeveloperUserId!.Equals(currentUser.Id))
            || (currentUserRole == BTRoles.Submitter
                && !oldTicket.SubmitterUserId!.Equals(currentUser.Id)))
            {
                return Unauthorized();
            }

            oldTicket.Updated = DateTime.Now;
            oldTicket.TicketPriorityId = selected;
            bool success = await _ticketService.UpdateTicketAsync(oldTicket);
            if (!success) throw new BadHttpRequestException("There was an issue changing the ticket priority", 500);

            // add history and send notification
            // TO-DO: add bool checks for success here
            Ticket? newTicket = await _ticketService.GetTicketByIdAsNoTrackingAsync(ticketId, _companyId);
            await _historyService.AddHistoryAsync(oldTicket, newTicket, _userManager.GetUserId(User));
            await _notificationService.TicketUpdateNotificationAsync(ticketId, _userManager.GetUserId(User));

            return RedirectToAction("Details", new { ticketId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int ticketId, int selected)
        {
            Ticket? oldTicket = await _ticketService.GetTicketByIdAsNoTrackingAsync(ticketId, _companyId)
                ?? throw new BadHttpRequestException("Problem finding specified ticket", 400);

            /* verify admin
            OR project manager for the ticket's project
            OR developer and assigned to the ticket
            OR submitter and submitted the ticket */
            BTUser? currentUser = await _userManager.GetUserAsync(User);
            BTRoles currentUserRole = await _roleService.GetActiveUserRoleAsync(currentUser!.Id);

            if ((currentUserRole == BTRoles.ProjectManager
                && await _projectService.GetProjectManagerAsync(oldTicket.ProjectId) != currentUser)
            || (currentUserRole == BTRoles.Developer
                && !oldTicket.DeveloperUserId!.Equals(currentUser.Id))
            || (currentUserRole == BTRoles.Submitter
                && !oldTicket.SubmitterUserId!.Equals(currentUser.Id)))
            {
                return Unauthorized();
            }

            oldTicket.Updated = DateTime.Now;
            oldTicket.TicketStatusId = selected;
            bool success = await _ticketService.UpdateTicketAsync(oldTicket);
            if (!success) throw new BadHttpRequestException("There was an issue changing the ticket status", 500);

            // add history and send notification
            // TO-DO: add bool checks for success here
            Ticket? newTicket = await _ticketService.GetTicketByIdAsNoTrackingAsync(ticketId, _companyId);
            await _historyService.AddHistoryAsync(oldTicket, newTicket, _userManager.GetUserId(User));
            await _notificationService.TicketUpdateNotificationAsync(ticketId, _userManager.GetUserId(User));

            return RedirectToAction("Details", new { ticketId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateType(int ticketId, int selected)
        {
            Ticket? oldTicket = await _ticketService.GetTicketByIdAsNoTrackingAsync(ticketId, _companyId)
                ?? throw new BadHttpRequestException("Problem finding specified ticket", 400);

            /* verify admin
            OR project manager for the ticket's project
            OR developer and assigned to the ticket
            OR submitter and submitted the ticket */
            BTUser? currentUser = await _userManager.GetUserAsync(User);
            BTRoles currentUserRole = await _roleService.GetActiveUserRoleAsync(currentUser!.Id);

            if ((currentUserRole == BTRoles.ProjectManager
                && await _projectService.GetProjectManagerAsync(oldTicket.ProjectId) != currentUser)
            || (currentUserRole == BTRoles.Developer
                && !oldTicket.DeveloperUserId!.Equals(currentUser.Id))
            || (currentUserRole == BTRoles.Submitter
                && !oldTicket.SubmitterUserId!.Equals(currentUser.Id)))
            {
                return Unauthorized();
            }

            oldTicket.Updated = DateTime.Now;
            oldTicket.TicketTypeId = selected;
            bool success = await _ticketService.UpdateTicketAsync(oldTicket);
            if (!success) throw new BadHttpRequestException("There was an issue changing the ticket type", 500);

            // add history and send notification
            // TO-DO: add bool checks for success here
            Ticket? newTicket = await _ticketService.GetTicketByIdAsNoTrackingAsync(ticketId, _companyId);
            await _historyService.AddHistoryAsync(oldTicket, newTicket, _userManager.GetUserId(User));
            await _notificationService.TicketUpdateNotificationAsync(ticketId, _userManager.GetUserId(User));

            return RedirectToAction("Details", new { ticketId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int? ticketId, string? comment)
        {
            if (ticketId == null) throw new BadHttpRequestException("Bad input", 400);
            if (comment == null) return View(nameof(Details), ticketId);

            Ticket? oldTicket = await _ticketService.GetTicketByIdAsync(ticketId, _companyId)
                ?? throw new BadHttpRequestException("Cannot find specified ticket", 400);

            /* verify admin
            OR project manager for the ticket's project
            OR developer and assigned to the ticket
            OR submitter and submitted the ticket */
            BTUser? currentUser = await _userManager.GetUserAsync(User);
            BTRoles currentUserRole = await _roleService.GetActiveUserRoleAsync(currentUser!.Id);

            if ((currentUserRole == BTRoles.ProjectManager
                && await _projectService.GetProjectManagerAsync(oldTicket.ProjectId) != currentUser)
            || (currentUserRole == BTRoles.Developer
                && !oldTicket.DeveloperUserId!.Equals(currentUser.Id))
            || (currentUserRole == BTRoles.Submitter
                && !oldTicket.SubmitterUserId!.Equals(currentUser.Id)))
            {
                return Unauthorized();
            }

            try
            {
                TicketComment ticketComment = new()
                {
                    TicketId = oldTicket.Id,
                    UserId = _userManager.GetUserId(User),
                    Comment = comment,
                    Created = DateTime.Now
                };

                oldTicket.Comments.Add(ticketComment);
                bool success = await _ticketService.UpdateTicketAsync(oldTicket);
                if (!success) throw new BadHttpRequestException("Problem adding comment", 500);

                // add history and send notification
                // TO-DO: add bool checks for success here
                Ticket? newTicket = await _ticketService.GetTicketByIdAsNoTrackingAsync(ticketId, _companyId);
                await _historyService.AddHistoryAsync(oldTicket, newTicket, _userManager.GetUserId(User));
                await _notificationService.TicketUpdateNotificationAsync(ticketId, _userManager.GetUserId(User));

                return RedirectToAction(nameof(Details), new { ticketId, statusMessage = "Success: Comment added" });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_ticketService.TicketExists(oldTicket.Id)) throw new BadHttpRequestException("Cannot find specified ticket");
                else throw new BadHttpRequestException("Problem adding comment", 500);
            }
            catch (Exception)
            {
                throw new BadHttpRequestException("Problem adding comment", 500);
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTicketAttachment([Bind("FormFile,Description,TicketId")] TicketAttachment attachment)
        {
            string statusMessage;
            ModelState.Remove("UserId");

            if (!ModelState.IsValid)
                throw new BadHttpRequestException("File is too large. Max size is 1MB.", 400);

            Ticket? ticket = await _ticketService.GetTicketByIdAsync(attachment.TicketId, _companyId)
                ?? throw new BadHttpRequestException("Cannot find specified ticket", 400);

            /* verify admin
            OR project manager for the ticket's project
            OR developer and assigned to the ticket
            OR submitter and submitted the ticket */
            BTUser? currentUser = await _userManager.GetUserAsync(User);
            BTRoles currentUserRole = await _roleService.GetActiveUserRoleAsync(currentUser!.Id);

            if ((currentUserRole == BTRoles.ProjectManager
                && await _projectService.GetProjectManagerAsync(ticket.ProjectId) != currentUser)
            || (currentUserRole == BTRoles.Developer
                && !ticket.DeveloperUserId!.Equals(currentUser.Id))
            || (currentUserRole == BTRoles.Submitter
                && !ticket.SubmitterUserId!.Equals(currentUser.Id)))
            {
                return Unauthorized();
            }

            try
            {
                if (attachment.FormFile == null) throw new BadHttpRequestException("No file attached", 400);
                attachment.FileData = await _fileService.ConvertFileToByteArrayAsync(attachment.FormFile);
                attachment.FileName = attachment.FormFile.FileName;
                attachment.FileType = attachment.FormFile.ContentType;

                attachment.Created = DateTime.Now;
                attachment.UserId = _userManager.GetUserId(User);

                bool success = await _ticketService.AddTicketAttachmentAsync(attachment);
                if (!success) throw new BadHttpRequestException("There was a problem saving the attachment", 500);
                statusMessage = "Success: New attachment added to Ticket.";
                return RedirectToAction("Details", new { ticketId = attachment.TicketId, statusMessage });
            }
            catch (Exception)
            {
                throw new BadHttpRequestException("There was a problem saving the attachment", 500);
            }
        }

        public async Task<IActionResult> EditTicketAttachment(int? attachmentId, int ticketId)
        {
            if (attachmentId == null) throw new BadHttpRequestException("Bad input", 400);
            TicketAttachment? attachment = await _ticketService.GetTicketAttachmentByIdAsync(attachmentId.Value)
                ?? throw new BadHttpRequestException("Cannot find the requested attachment", 400);

            Ticket? ticket = await _ticketService.GetTicketByIdAsync(ticketId, _companyId)
                ?? throw new BadHttpRequestException("Cannot find specified ticket", 400);

            /* verify admin
            OR project manager for the ticket's project
            OR developer and assigned to the ticket
            OR submitter and submitted the ticket */
            BTUser? currentUser = await _userManager.GetUserAsync(User);
            BTRoles currentUserRole = await _roleService.GetActiveUserRoleAsync(currentUser!.Id);

            if ((currentUserRole == BTRoles.ProjectManager
                && await _projectService.GetProjectManagerAsync(ticket.ProjectId) != currentUser)
            || (currentUserRole == BTRoles.Developer
                && !ticket.DeveloperUserId!.Equals(currentUser.Id))
            || (currentUserRole == BTRoles.Submitter
                && !ticket.SubmitterUserId!.Equals(currentUser.Id)))
            {
                return Unauthorized();
            }

            return View(attachment);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTicketAttachment(int id, int ticketId)
        {
            TicketAttachment? attachment = await _ticketService.GetTicketAttachmentByIdAsync(id)
                ?? throw new BadHttpRequestException("Cannot find the requested attachment", 400);

            Ticket? ticket = await _ticketService.GetTicketByIdAsync(ticketId, _companyId)
                ?? throw new BadHttpRequestException("Cannot find specified ticket", 400);

            /* verify admin
            OR project manager for the ticket's project
            OR developer and assigned to the ticket
            OR submitter and submitted the ticket */
            BTUser? currentUser = await _userManager.GetUserAsync(User);
            BTRoles currentUserRole = await _roleService.GetActiveUserRoleAsync(currentUser!.Id);

            if ((currentUserRole == BTRoles.ProjectManager
                && await _projectService.GetProjectManagerAsync(ticket.ProjectId) != currentUser)
            || (currentUserRole == BTRoles.Developer
                && !ticket.DeveloperUserId!.Equals(currentUser.Id))
            || (currentUserRole == BTRoles.Submitter
                && !ticket.SubmitterUserId!.Equals(currentUser.Id)))
            {
                return Unauthorized();
            }

            bool validUpdate = await TryUpdateModelAsync(
                attachment,
                string.Empty,
                t => t.Description);
                if (!validUpdate) throw new BadHttpRequestException("Cannot edit the specified attachment", 400);

            try
            {
                bool success = await _ticketService.UpdateTicketAttachmentAsync(attachment);
                if (!success) throw new BadHttpRequestException("There was a problem editing the specified attachment", 500);
                return RedirectToAction(nameof(Details), new { ticketId, statusMessage = "Success: Attachment edited" });
            }
            catch (Exception)
            {
                throw new BadHttpRequestException("There was a problem editing the specified attachment", 500);
            }

        }

        public async Task<IActionResult> DeleteTicketAttachment(int? attachmentId, int ticketId)
        {
            if (attachmentId == null) throw new BadHttpRequestException("Bad input", 400);
            TicketAttachment? attachment = await _ticketService.GetTicketAttachmentByIdAsync(attachmentId.Value)
                ?? throw new BadHttpRequestException("Cannot find the requested attachment", 400);

            Ticket? ticket = await _ticketService.GetTicketByIdAsync(ticketId, _companyId)
                ?? throw new BadHttpRequestException("Cannot find specified ticket", 400);

            /* verify admin
            OR project manager for the ticket's project
            OR developer and assigned to the ticket
            OR submitter and submitted the ticket */
            BTUser? currentUser = await _userManager.GetUserAsync(User);
            BTRoles currentUserRole = await _roleService.GetActiveUserRoleAsync(currentUser!.Id);

            if ((currentUserRole == BTRoles.ProjectManager
                && await _projectService.GetProjectManagerAsync(ticket.ProjectId) != currentUser)
            || (currentUserRole == BTRoles.Developer
                && !ticket.DeveloperUserId!.Equals(currentUser.Id))
            || (currentUserRole == BTRoles.Submitter
                && !ticket.SubmitterUserId!.Equals(currentUser.Id)))
            {
                return Unauthorized();
            }

            return View(attachment);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTicketAttachment(int id, int ticketId)
        {
            TicketAttachment? attachment = await _ticketService.GetTicketAttachmentByIdAsync(id)
                ?? throw new BadHttpRequestException("Cannot find the requested attachment", 400);

            Ticket? ticket = await _ticketService.GetTicketByIdAsync(ticketId, _companyId)
                ?? throw new BadHttpRequestException("Cannot find specified ticket", 400);

            /* verify admin
            OR project manager for the ticket's project
            OR developer and assigned to the ticket
            OR submitter and submitted the ticket */
            BTUser? currentUser = await _userManager.GetUserAsync(User);
            BTRoles currentUserRole = await _roleService.GetActiveUserRoleAsync(currentUser!.Id);

            if ((currentUserRole == BTRoles.ProjectManager
                && await _projectService.GetProjectManagerAsync(ticket.ProjectId) != currentUser)
            || (currentUserRole == BTRoles.Developer
                && !ticket.DeveloperUserId!.Equals(currentUser.Id))
            || (currentUserRole == BTRoles.Submitter
                && !ticket.SubmitterUserId!.Equals(currentUser.Id)))
            {
                return Unauthorized();
            }

            try
            {
                bool success = await _ticketService.DeleteTicketAttachmentAsync(attachment);
                if (!success) throw new BadHttpRequestException("There was a problem editing the specified attachment", 500);
                return RedirectToAction(nameof(Details), new { ticketId, statusMessage = "Success: Attachment deleted" });
            }
            catch (Exception)
            {
                throw new BadHttpRequestException("There was a problem deleting the specified attachment", 500);
            }
        }

        [Authorize]
        public async Task<IActionResult> ShowFile(int attachmentId)
        {
            TicketAttachment? ticketAttachment = await _ticketService.GetTicketAttachmentByIdAsync(attachmentId)
                ?? throw new BadHttpRequestException("Cannot find the requested attachment", 400);

            string fileName = ticketAttachment.FileName!;
            byte[] fileData = ticketAttachment.FileData!;
            string ext = Path.GetExtension(fileName)!.Replace(".", "");

            Response.Headers.Add("Content-Disposition", $"inline; filename={fileName}");
            return File(fileData, $"application/{ext}");
        }
    }
}
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
        private readonly ILookupService _lookupService;

        public TicketsController(ApplicationDbContext context,
            UserManager<BTUser> userManager,
            ICompanyService companyService,
            ITicketService ticketService,
            IRoleService roleService,
            IFileService fileService,
            IProjectService projectService,
            ITicketHistoryService historyService,
            INotificationService notificationService,
            ILookupService lookupService)
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
            _lookupService = lookupService;
        }

        // GET: Tickets
        public IActionResult Index()
        {
            if (User.IsInRole(nameof(BTRoles.Admin))) return RedirectToAction(nameof(AllTickets));
            else return RedirectToAction(nameof(MyTickets));
        }

        public async Task<IActionResult> MyTickets()
        {
            IEnumerable<Ticket> tickets = await _ticketService.GetTicketsByUserIdAsync(_userId, _companyId);

            ViewBag.ActionName = nameof(MyTickets);
            return View(nameof(Index), tickets);
        }

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

            Ticket? ticket = await _ticketService.GetTicketByIdAsNoTrackingAsync(ticketId, _companyId)
                ?? throw new BadHttpRequestException("Cannot find specified ticket");

            ViewBag.TicketPriorities = new SelectList(await _lookupService.GetTicketPrioritiesAsync(), "Id", "Name", ticket.TicketPriorityId);
            ViewBag.TicketStatuses = new SelectList(await _lookupService.GetTicketStatusesAsync(), "Id", "Name", ticket.TicketStatusId);
            ViewBag.TicketTypes = new SelectList(await _lookupService.GetTicketTypesAsync(), "Id", "Name", ticket.TicketTypeId);
            ViewBag.Developers = new SelectList(await _projectService.GetProjectDevelopersAsync(ticket.ProjectId), "Id", "FullName", ticket.DeveloperUserId);

            return View(ticket);
        }

        // GET: Tickets/Create
        [Authorize(Policy = nameof(BTPolicies.NoDemo))]
        public async Task<IActionResult> Create(int? projectId)
        {
            Project? project = await _projectService.GetProjectAsync(projectId, _companyId)
                ?? throw new BadHttpRequestException("Cannot find specified project", 400);

            // if user is not admin and not on the project
            if (!User.IsInRole(nameof(BTRoles.Admin))
                && !project.Members.Any(u => u.Id == _userId))
                throw new BadHttpRequestException("You are not on the specified project", 400);

            ViewBag.ProjectId = projectId;
            ViewData["TicketTypes"] = new SelectList(await _lookupService.GetTicketTypesAsync(), "Id", "Name");
            ViewData["TicketPriorities"] = new SelectList(await _lookupService.GetTicketPrioritiesAsync(), "Id", "Name");
            ViewData["DeveloperUsers"] = new SelectList(await _projectService.GetProjectDevelopersAsync(projectId), "Id", "FullName");
            return View();
        }

        // POST: Tickets/Create
        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = nameof(BTPolicies.NoDemo))]
        public async Task<IActionResult> Create([Bind("ProjectId,Title,Description,TicketTypeId,TicketPriorityId,DeveloperUserId")] Ticket ticket, string? selectedDevId)
        {
            Project? project = await _projectService.GetProjectAsync(ticket.ProjectId, _companyId)
                ?? throw new BadHttpRequestException("Cannot find specified project", 400);

            // if user is not admin and not on the project
            if (!User.IsInRole(nameof(BTRoles.Admin))
                && !project.Members.Any(u => u.Id == _userId))
                throw new BadHttpRequestException("You are not on the specified project", 400);

            ModelState.Remove("DeveloperUserId");
            ModelState.Remove("SubmitterUserId");
            ModelState.Remove("TicketStatusId");
            ModelState.Remove("Created");
            ModelState.Remove("Archived");
            ModelState.Remove("ArchivedByProject");

            if (!ModelState.IsValid)
            {
                ViewBag.ProjectId = ticket.ProjectId;
                ViewData["TicketTypes"] = new SelectList(await _lookupService.GetTicketTypesAsync(), "Id", "Name");
                ViewData["TicketPriorities"] = new SelectList(await _lookupService.GetTicketPrioritiesAsync(), "Id", "Name");
                ViewData["DeveloperUsers"] = new SelectList(await _projectService.GetProjectDevelopersAsync(ticket.ProjectId), "Id", "FullName");
                return View(ticket);
            }

            try
            {
                ticket.SubmitterUser = await _companyService.GetCompanyUserByIdAsync(_userId, _companyId);
                ticket.Created = DateTime.Now;
                ticket.Archived = false;
                ticket.ArchivedByProject = false;

                if (selectedDevId?.Equals("Unassigned") == true) ticket.DeveloperUserId = null;
                else ticket.DeveloperUserId = selectedDevId;

                // remove excess space around comment due to editor
                ticket.Description = Regex.Replace(ticket.Description!, @"<[^>]*>", string.Empty);

                TicketStatus newStatus = await _lookupService.GetTicketStatusByEnumAsync(BTTicketStatuses.New);
                ticket.TicketStatusId = newStatus.Id;
                await _ticketService.AddTicketAsync(ticket);

                // add history and send notification
                await _historyService.AddHistoryAsync(null, ticket, _userId);
                await _notificationService.NewTicketNotificationAsync(ticket.Id, _userId);

                return RedirectToAction("Details", "Projects", new { id = ticket.ProjectId });
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
        }

        // GET: Tickets/Edit/5
        public async Task<IActionResult> Edit(int? ticketId)
        {
            if (ticketId == null) throw new BadHttpRequestException("Bad input", 400);

            Ticket? ticket = await _ticketService.GetTicketByIdAsync(ticketId, _companyId)
            ?? throw new BadHttpRequestException("Cannot find specified ticket");

            // If not project manager for the ticket's project
            // OR not an admin AND neither Developer or Submitter for ticket
            if (!_userId.Equals((await _projectService.GetProjectManagerAsync(ticket.ProjectId))?.Id)
                || (!User.IsInRole(nameof(BTRoles.Admin))
                    && !ticket.DeveloperUserId?.Equals(_userId) == false
                    && !ticket.SubmitterUserId?.Equals(_userId) == false))
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
            Ticket? ticket = await _ticketService.GetTicketByIdAsync(ticketId, _companyId)
                ?? throw new BadHttpRequestException("Problem finding specified ticket", 400);

            // If not project manager for the ticket's project
            // OR not an admin AND neither Developer or Submitter for ticket
            if (!_userId.Equals((await _projectService.GetProjectManagerAsync(ticket.ProjectId))?.Id)
                || (!User.IsInRole(nameof(BTRoles.Admin))
                    && !ticket.DeveloperUserId?.Equals(_userId) == false
                    && !ticket.SubmitterUserId?.Equals(_userId) == false))
            {
                return Unauthorized();
            }

            bool validUpdate = await TryUpdateModelAsync(
                ticket,
                string.Empty,
                t => t.Title,
                t => t.Description
                );

            if (!validUpdate) return View(ticket);

            try
            {
                // remove excess space around comment due to editor
                ticket.Description = Regex.Replace(ticket.Description!, @"<[^>]*>", string.Empty);

                ticket.Updated = DateTime.Now;
                bool success = await _ticketService.UpdateTicketAsync(ticket);
                if (!success) throw new BadHttpRequestException("Problem updating ticket", 500);

                // add history and send notification
                Ticket? newTicket = await _ticketService.GetTicketByIdAsNoTrackingAsync(ticketId, _companyId);
                await _historyService.AddHistoryAsync(oldTicket, newTicket, _userId);
                await _notificationService.TicketUpdateNotificationAsync(ticketId, _userId);

                return RedirectToAction(nameof(Details), new { ticketId });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_ticketService.TicketExists(ticketId)) throw new BadHttpRequestException("Cannot find specified ticket", 400);
                else throw new BadHttpRequestException("Problem updating ticket", 500);
            }
            catch (Exception)
            {
                throw new BadHttpRequestException("Problem updating ticket", 500);
            }
        }

        public async Task<IActionResult> ConfirmArchive(int? ticketId)
        {
            Ticket? ticket = await _ticketService.GetTicketByIdAsNoTrackingAsync(ticketId, _companyId)
                ?? throw new BadHttpRequestException("Problem finding specified ticket", 400);

            // If not project manager for the ticket's project
            // OR not an admin AND neither Developer or Submitter for ticket
            if (!_userId.Equals((await _projectService.GetProjectManagerAsync(ticket.ProjectId))?.Id)
                || (!User.IsInRole(nameof(BTRoles.Admin))
                    && !ticket.DeveloperUserId?.Equals(_userId) == false
                    && !ticket.SubmitterUserId?.Equals(_userId) == false))
            {
                return Unauthorized();
            }

            return View(ticket);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmArchive(int ticketId)
        {
            Ticket? oldTicket = await _ticketService.GetTicketByIdAsNoTrackingAsync(ticketId, _companyId)
                ?? throw new BadHttpRequestException("Problem finding specified ticket", 400);
            Ticket? ticket = await _ticketService.GetTicketByIdAsync(ticketId, _companyId)
                ?? throw new BadHttpRequestException("Problem finding specified ticket", 400);

            // If not project manager for the ticket's project
            // OR not an admin AND neither Developer or Submitter for ticket
            if (!_userId.Equals((await _projectService.GetProjectManagerAsync(ticket.ProjectId))?.Id)
                || (!User.IsInRole(nameof(BTRoles.Admin))
                    && !ticket.DeveloperUserId?.Equals(_userId) == false
                    && !ticket.SubmitterUserId?.Equals(_userId) == false))
            {
                return Unauthorized();
            }

            bool validUpdate = await TryUpdateModelAsync(
                ticket,
                string.Empty,
                t => t.Archived);

            if (!validUpdate) return View(ticket);

            try
            {
                ticket.Updated = DateTime.Now;
                ticket.Archived = true;
                bool success = await _ticketService.UpdateTicketAsync(ticket);
                if (!success) throw new BadHttpRequestException("Problem updating ticket", 500);

                // add history and send notification
                Ticket? newTicket = await _ticketService.GetTicketByIdAsNoTrackingAsync(ticketId, _companyId);
                await _historyService.AddHistoryAsync(oldTicket, newTicket, _userId);
                await _notificationService.TicketUpdateNotificationAsync(ticketId, _userId);

                return RedirectToAction(nameof(ActiveTickets));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_ticketService.TicketExists(ticketId)) throw new BadHttpRequestException("Cannot find specified ticket", 400);
                else throw new BadHttpRequestException("Problem updating ticket", 500);
            }
            catch (Exception)
            {
                throw new BadHttpRequestException("Problem updating ticket", 500);
            }
        }

        public async Task<IActionResult> ConfirmUnarchive(int? ticketId)
        {
            Ticket? ticket = await _ticketService.GetTicketByIdAsNoTrackingAsync(ticketId, _companyId)
                ?? throw new BadHttpRequestException("Problem finding specified ticket", 400);

            // If not project manager for the ticket's project
            // OR not an admin AND neither Developer or Submitter for ticket
            if (!_userId.Equals((await _projectService.GetProjectManagerAsync(ticket.ProjectId))?.Id)
                || (!User.IsInRole(nameof(BTRoles.Admin))
                    && !ticket.DeveloperUserId?.Equals(_userId) == false
                    && !ticket.SubmitterUserId?.Equals(_userId) == false))
            {
                return Unauthorized();
            }

            return View(ticket);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmUnarchive(int ticketId)
        {
            Ticket? oldTicket = await _ticketService.GetTicketByIdAsNoTrackingAsync(ticketId, _companyId)
                ?? throw new BadHttpRequestException("Problem finding specified ticket", 400);
            Ticket? ticket = await _ticketService.GetTicketByIdAsync(ticketId, _companyId)
                ?? throw new BadHttpRequestException("Problem finding specified ticket", 400);

            // If not project manager for the ticket's project
            // OR not an admin AND neither Developer or Submitter for ticket
            if (!_userId.Equals((await _projectService.GetProjectManagerAsync(ticket.ProjectId))?.Id)
                || (!User.IsInRole(nameof(BTRoles.Admin))
                    && !ticket.DeveloperUserId?.Equals(_userId) == false
                    && !ticket.SubmitterUserId?.Equals(_userId) == false))
            {
                return Unauthorized();
            }

            bool validUpdate = await TryUpdateModelAsync(
                ticket,
                string.Empty,
                t => t.Archived);

            if (!validUpdate) return View(ticket);

            try
            {
                ticket.Updated = DateTime.Now;
                ticket.Archived = false;
                bool success = await _ticketService.UpdateTicketAsync(ticket);
                if (!success) throw new BadHttpRequestException("Problem updating ticket", 500);

                // add history and send notification
                Ticket? newTicket = await _ticketService.GetTicketByIdAsNoTrackingAsync(ticketId, _companyId);
                await _historyService.AddHistoryAsync(oldTicket, newTicket, _userId);
                await _notificationService.TicketUpdateNotificationAsync(ticketId, _userId);

                return RedirectToAction(nameof(Details), new { ticketId });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_ticketService.TicketExists(ticketId)) throw new BadHttpRequestException("Cannot find specified ticket", 400);
                else throw new BadHttpRequestException("Problem updating ticket", 500);
            }
            catch (Exception)
            {
                throw new BadHttpRequestException("Problem updating ticket", 500);
            }
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = nameof(BTPolicies.AdPm))]
        public async Task<IActionResult> AssignDeveloper(int ticketId, string selectedDevId)
        {
            Ticket? oldTicket = await _ticketService.GetTicketByIdAsNoTrackingAsync(ticketId, _companyId)
                ?? throw new BadHttpRequestException("Problem finding specified ticket", 400);
            Ticket? ticket = await _ticketService.GetTicketByIdAsync(ticketId, _companyId)
                ?? throw new BadHttpRequestException("Problem finding specified ticket", 400);

            // If not project manager for the ticket's project
            // OR not an admin
            if (!_userId.Equals((await _projectService.GetProjectManagerAsync(ticket.ProjectId))?.Id)
                && (!User.IsInRole(nameof(BTRoles.Admin))))
            {
                return Unauthorized();
            }

            try
            {
                ticket.Updated = DateTime.Now;

                if (selectedDevId?.Equals("Unassigned") == true) ticket.DeveloperUserId = null;
                else ticket.DeveloperUserId = selectedDevId;

                bool success = await _ticketService.UpdateTicketAsync(ticket);
                if (!success) throw new BadHttpRequestException("There was an issue assigning the developer", 500);

                // add history and send notification
                Ticket? newTicket = await _ticketService.GetTicketByIdAsNoTrackingAsync(ticketId, _companyId);
                await _historyService.AddHistoryAsync(oldTicket, newTicket, _userId);
                await _notificationService.TicketUpdateNotificationAsync(ticketId, _userId);

                return RedirectToAction(nameof(Details), new { ticketId });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_ticketService.TicketExists(ticketId)) throw new BadHttpRequestException("Cannot find specified ticket", 400);
                else throw new BadHttpRequestException("Problem updating ticket", 500);
            }
            catch (Exception)
            {
                throw new BadHttpRequestException("Problem updating ticket", 500);
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePriority(int ticketId, int selected)
        {
            Ticket? oldTicket = await _ticketService.GetTicketByIdAsNoTrackingAsync(ticketId, _companyId)
                ?? throw new BadHttpRequestException("Problem finding specified ticket", 400);
            Ticket? ticket = await _ticketService.GetTicketByIdAsync(ticketId, _companyId)
                ?? throw new BadHttpRequestException("Problem finding specified ticket", 400);

            // If not project manager for the ticket's project
            // OR not an admin AND neither Developer or Submitter for ticket
            if (!_userId.Equals((await _projectService.GetProjectManagerAsync(ticket.ProjectId))?.Id)
                || (!User.IsInRole(nameof(BTRoles.Admin))
                    && !ticket.DeveloperUserId?.Equals(_userId) == false
                    && !ticket.SubmitterUserId?.Equals(_userId) == false))
            {
                return Unauthorized();
            }

            try
            {
                ticket.Updated = DateTime.Now;
                ticket.TicketPriorityId = selected;
                bool success = await _ticketService.UpdateTicketAsync(ticket);
                if (!success) throw new BadHttpRequestException("There was an issue changing the ticket priority", 500);

                // add history and send notification
                Ticket? newTicket = await _ticketService.GetTicketByIdAsNoTrackingAsync(ticketId, _companyId);
                await _historyService.AddHistoryAsync(oldTicket, newTicket, _userManager.GetUserId(User));
                await _notificationService.TicketUpdateNotificationAsync(ticketId, _userManager.GetUserId(User));

                return RedirectToAction(nameof(Details), new { ticketId });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_ticketService.TicketExists(ticketId)) throw new BadHttpRequestException("Cannot find specified ticket", 400);
                else throw new BadHttpRequestException("Problem updating ticket", 500);
            }
            catch (Exception)
            {
                throw new BadHttpRequestException("Problem updating ticket", 500);
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int ticketId, int selected)
        {
            Ticket? oldTicket = await _ticketService.GetTicketByIdAsNoTrackingAsync(ticketId, _companyId)
                ?? throw new BadHttpRequestException("Problem finding specified ticket", 400);
            Ticket? ticket = await _ticketService.GetTicketByIdAsync(ticketId, _companyId)
                ?? throw new BadHttpRequestException("Problem finding specified ticket", 400);

            // If not project manager for the ticket's project
            // OR not an admin AND neither Developer or Submitter for ticket
            if (!_userId.Equals((await _projectService.GetProjectManagerAsync(ticket.ProjectId))?.Id)
                || (!User.IsInRole(nameof(BTRoles.Admin))
                    && !ticket.DeveloperUserId?.Equals(_userId) == false
                    && !ticket.SubmitterUserId?.Equals(_userId) == false))
            {
                return Unauthorized();
            }

            try
            {
                ticket.Updated = DateTime.Now;
                ticket.TicketStatusId = selected;
                bool success = await _ticketService.UpdateTicketAsync(ticket);
                if (!success) throw new BadHttpRequestException("There was an issue changing the ticket status", 500);

                // add history and send notification
                Ticket? newTicket = await _ticketService.GetTicketByIdAsNoTrackingAsync(ticketId, _companyId);
                await _historyService.AddHistoryAsync(oldTicket, newTicket, _userId);
                await _notificationService.TicketUpdateNotificationAsync(ticketId, _userId);

                return RedirectToAction(nameof(Details), new { ticketId });
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

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateType(int ticketId, int selected)
        {
            Ticket? oldTicket = await _ticketService.GetTicketByIdAsNoTrackingAsync(ticketId, _companyId)
                ?? throw new BadHttpRequestException("Problem finding specified ticket", 400);
            Ticket? ticket = await _ticketService.GetTicketByIdAsync(ticketId, _companyId)
                ?? throw new BadHttpRequestException("Problem finding specified ticket", 400);

            // If not project manager for the ticket's project
            // OR not an admin AND neither Developer or Submitter for ticket
            if (!_userId.Equals((await _projectService.GetProjectManagerAsync(ticket.ProjectId))?.Id)
                || (!User.IsInRole(nameof(BTRoles.Admin))
                    && !ticket.DeveloperUserId?.Equals(_userId) == false
                    && !ticket.SubmitterUserId?.Equals(_userId) == false))
            {
                return Unauthorized();
            }

            try
            {
                ticket.Updated = DateTime.Now;
                ticket.TicketTypeId = selected;
                bool success = await _ticketService.UpdateTicketAsync(ticket);
                if (!success) throw new BadHttpRequestException("There was an issue changing the ticket type", 500);

                // add history and send notification
                Ticket? newTicket = await _ticketService.GetTicketByIdAsNoTrackingAsync(ticketId, _companyId);
                await _historyService.AddHistoryAsync(oldTicket, newTicket, _userManager.GetUserId(User));
                await _notificationService.TicketUpdateNotificationAsync(ticketId, _userManager.GetUserId(User));

                return RedirectToAction(nameof(Details), new { ticketId });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_ticketService.TicketExists(ticketId)) throw new BadHttpRequestException("Cannot find specified ticket", 400);
                else throw new BadHttpRequestException("Problem updating ticket", 500);
            }
            catch (Exception)
            {
                throw new BadHttpRequestException("Problem updating ticket", 500);
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int? ticketId, string? comment)
        {
            if (ticketId == null) throw new BadHttpRequestException("Bad input", 400);
            if (comment == null) return RedirectToAction(nameof(Details), new { ticketId });

            Ticket? oldTicket = await _ticketService.GetTicketByIdAsNoTrackingAsync(ticketId, _companyId)
                ?? throw new BadHttpRequestException("Cannot find specified ticket", 400);
            Ticket? ticket = await _ticketService.GetTicketByIdAsync(ticketId, _companyId)
                ?? throw new BadHttpRequestException("Cannot find specified ticket", 400);

            // If not project manager for the ticket's project
            // OR not an admin AND neither Developer or Submitter for ticket
            if (!_userId.Equals((await _projectService.GetProjectManagerAsync(ticket.ProjectId))?.Id)
                || (!User.IsInRole(nameof(BTRoles.Admin))
                    && !ticket.DeveloperUserId?.Equals(_userId) == false
                    && !ticket.SubmitterUserId?.Equals(_userId) == false))
            {
                return Unauthorized();
            }

            try
            {
                TicketComment ticketComment = new()
                {
                    TicketId = ticket.Id,
                    UserId = _userId,
                    Comment = comment,
                    Created = DateTime.Now
                };
                ticket.Comments.Add(ticketComment);

                bool success = await _ticketService.UpdateTicketAsync(ticket);
                if (!success) throw new BadHttpRequestException("Problem adding comment", 500);

                // add history and send notification
                Ticket? newTicket = await _ticketService.GetTicketByIdAsNoTrackingAsync(ticketId, _companyId);
                await _historyService.AddHistoryAsync(oldTicket, newTicket, _userId);
                await _notificationService.TicketUpdateNotificationAsync(ticketId, _userId);

                return RedirectToAction(nameof(Details), new { ticketId, successMessage = "Success: Comment Added" });
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
            ModelState.Remove("UserId");

            if (!ModelState.IsValid)
                throw new BadHttpRequestException("File is too large. Max size is 1MB.", 400);

            Ticket? oldTicket = await _ticketService.GetTicketByIdAsNoTrackingAsync(attachment.TicketId, _companyId)
                ?? throw new BadHttpRequestException("Cannot find specified ticket", 400);
            Ticket? ticket = await _ticketService.GetTicketByIdAsync(attachment.TicketId, _companyId)
                ?? throw new BadHttpRequestException("Cannot find specified ticket", 400);

            // If not project manager for the ticket's project
            // OR not an admin AND neither Developer or Submitter for ticket
            if (!_userId.Equals((await _projectService.GetProjectManagerAsync(ticket.ProjectId))?.Id)
                || (!User.IsInRole(nameof(BTRoles.Admin))
                    && !ticket.DeveloperUserId?.Equals(_userId) == false
                    && !ticket.SubmitterUserId?.Equals(_userId) == false))
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
                attachment.UserId = _userId;

                bool success = await _ticketService.AddTicketAttachmentAsync(attachment);
                if (!success) throw new BadHttpRequestException("There was a problem saving the attachment", 500);

                // add history and send notification
                Ticket? newTicket = await _ticketService.GetTicketByIdAsNoTrackingAsync(attachment.TicketId, _companyId);
                await _historyService.AddHistoryAsync(oldTicket, newTicket, _userId);
                await _notificationService.TicketUpdateNotificationAsync(attachment.TicketId, _userId);

                return RedirectToAction(nameof(Details), new { ticketId = attachment.TicketId, statusMessage = "Success: Attachment Added" });
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

            // If not project manager for the ticket's project
            // OR not an admin AND attachment is not current user's
            if (!_userId.Equals((await _projectService.GetProjectManagerAsync(ticket.ProjectId))?.Id)
                && !User.IsInRole(nameof(BTRoles.Admin))
                && attachment.UserId?.Equals(_userId) == false)
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

            Ticket? oldTicket = await _ticketService.GetTicketByIdAsNoTrackingAsync(ticketId, _companyId)
                ?? throw new BadHttpRequestException("Cannot find specified ticket", 400);
            Ticket? ticket = await _ticketService.GetTicketByIdAsync(ticketId, _companyId)
                ?? throw new BadHttpRequestException("Cannot find specified ticket", 400);

            // If not project manager for the ticket's project
            // AND not an admin
            // AND attachment is not current user's
            if (!_userId.Equals((await _projectService.GetProjectManagerAsync(ticket.ProjectId))?.Id)
                && !User.IsInRole(nameof(BTRoles.Admin))
                && attachment.UserId?.Equals(_userId) == false)
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

                // add history and send notification
                Ticket? newTicket = await _ticketService.GetTicketByIdAsNoTrackingAsync(ticketId, _companyId);
                await _historyService.AddHistoryAsync(oldTicket, newTicket, _userId);
                await _notificationService.TicketUpdateNotificationAsync(ticketId, _userId);

                return RedirectToAction(nameof(Details), new { ticketId, statusMessage = "Success: Attachment edited" });
            }
            catch (Exception)
            {
                throw new BadHttpRequestException("There was a problem editing the specified attachment", 500);
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
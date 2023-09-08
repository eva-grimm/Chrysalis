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

namespace Chrysalis.Controllers
{
    [Authorize]
    public class TicketsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<BTUser> _userManager;
        private readonly ICompanyService _companyService;
        private readonly ITicketService _ticketService;
        private readonly IBTRolesService _roleService;

        public TicketsController(ApplicationDbContext context,
            UserManager<BTUser> userManager,
            ICompanyService companyService,
            ITicketService ticketService,
            IBTRolesService roleService)
        {
            _context = context;
            _userManager = userManager;
            _companyService = companyService;
            _ticketService = ticketService;
            _roleService = roleService;
        }

        // GET: Tickets
        public async Task<IActionResult> Index()
        {
            BTUser? user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            Company? company = await _companyService.GetCompanyById(user.CompanyId);

            IEnumerable<Ticket> tickets = company.Projects
                .SelectMany(p => p.Tickets);

            return View(tickets);
        }

        // GET: Tickets/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            Ticket? ticket = await _ticketService
                .GetSingleCompanyTicketAsync(id, User.Identity!.GetCompanyId());
            if (ticket == null) return NotFound();

            return View(ticket);
        }

        // GET: Tickets/Create
        public async Task<IActionResult> Create()
        {
            ViewData["CompanyProjects"] = new SelectList(await _companyService.GetAllCompanyProjectsAsync(User.Identity!.GetCompanyId()), "Id", "Name");
            ViewData["TicketTypes"] = new SelectList(await _ticketService.GetAllTicketTypes(), "Id", "Name");
            ViewData["TicketPriorities"] = new SelectList(await _ticketService.GetAllTicketPriorities(), "Id", "Name");
            ViewData["DeveloperUsers"] = new SelectList(await _roleService.GetUsersInRoleAsync(BTRoles.Developer.ToString(), User.Identity!.GetCompanyId()), "Id", "FullName");
            return View();
        }

        // POST: Tickets/Create
        [HttpPost, ValidateAntiForgeryToken]
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

                    // get and assign ID of TicketStatus by the name of New
                    // if failure, assign 0 to catch exception
                    int? ticketStatusId = await _ticketService.GetTicketStatusIdAsync(BTTicketStatuses.New);
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
                return RedirectToAction(nameof(Index));
            }
            else
            {
                ViewData["CompanyProjects"] = new SelectList(await _companyService.GetAllCompanyProjectsAsync(User.Identity!.GetCompanyId()), "Id", "Name");
                ViewData["TicketTypes"] = new SelectList(await _ticketService.GetAllTicketTypes(), "Id", "Name");
                ViewData["TicketPriorities"] = new SelectList(await _ticketService.GetAllTicketPriorities(), "Id", "Name");
                ViewData["DeveloperUsers"] = new SelectList(await _roleService.GetUsersInRoleAsync(BTRoles.Developer.ToString(), User.Identity!.GetCompanyId()), "Id", "FullName");
                return View(ticket);
            }
        }

        // GET: Tickets/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            Ticket? ticket = await _ticketService.GetSingleCompanyTicketAsync(id, User.Identity!.GetCompanyId());
            if (ticket == null) return NotFound();

            ViewData["TicketTypes"] = new SelectList(await _ticketService.GetAllTicketTypes(), "Id", "Name");
            ViewData["TicketStatuses"] = new SelectList(await _ticketService.GetAllTicketStatuses(), "Id", "Name");
            ViewData["TicketPriorities"] = new SelectList(await _ticketService.GetAllTicketPriorities(), "Id", "Name");
            ViewData["DeveloperUsers"] = new SelectList(await _roleService.GetUsersInRoleAsync(BTRoles.Developer.ToString(), User.Identity!.GetCompanyId()), "Id", "FullName");
            return View(ticket);
        }

        // POST: Tickets/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id)
        {
            // TO-DO: security check
            // who should be allowed to edit the ticket?
            Ticket? ticket = await _ticketService.GetSingleCompanyTicketAsync(id, User.Identity!.GetCompanyId());
            if (ticket == null) return NotFound();

            bool validUpdate = await TryUpdateModelAsync(
                ticket,
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
                try
                {
                    ticket.Updated = DateTime.Now;
                    await _ticketService.UpdateTicketAsync(ticket);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_ticketService.TicketExists(ticket.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            else
            {
                ViewData["TicketTypes"] = new SelectList(await _ticketService.GetAllTicketTypes(), "Id", "Name");
                ViewData["TicketStatuses"] = new SelectList(await _ticketService.GetAllTicketStatuses(), "Id", "Name");
                ViewData["TicketPriorities"] = new SelectList(await _ticketService.GetAllTicketPriorities(), "Id", "Name");
                ViewData["DeveloperUsers"] = new SelectList(await _roleService.GetUsersInRoleAsync(BTRoles.Developer.ToString(), User.Identity!.GetCompanyId()), "Id", "FullName");
                return View(ticket);
            }
        }

        // GET: Tickets/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Tickets == null)
            {
                return NotFound();
            }

            var ticket = await _context.Tickets
                .Include(t => t.DeveloperUser)
                .Include(t => t.Project)
                .Include(t => t.SubmitterUser)
                .Include(t => t.TicketPriority)
                .Include(t => t.TicketStatus)
                .Include(t => t.TicketType)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket);
        }

        // POST: Tickets/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Tickets == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Tickets'  is null.");
            }
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket != null)
            {
                _context.Tickets.Remove(ticket);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        // POST: AddTicketComment
        public async Task<IActionResult> AddTicketComment(int? id, string? comment)
        {
            if (id == null) return NotFound();
            if (comment == null) return View(nameof(Details), id);
            // TO-DO: security check
            // who should be allowed to edit the ticket?

            Ticket? ticket = await _ticketService.GetSingleCompanyTicketAsync(id, User.Identity!.GetCompanyId());
            if (ticket == null) return NotFound();

            //bool validUpdate = await TryUpdateModelAsync(ticket, string.Empty,
            //                    t => t.);
            try
            {
                TicketComment ticketComment = new TicketComment
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
    }
}

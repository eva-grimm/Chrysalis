using Chrysalis.Enums;
using Chrysalis.Models;
using Chrysalis.Services;
using Chrysalis.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Chrysalis.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<BTUser> _userManager;
        private readonly ITicketService _ticketService;
        private readonly ICompanyService _companyService;

        public HomeController(ILogger<HomeController> logger,
            UserManager<BTUser> userManager,
            ITicketService ticketService,
            ICompanyService companyService)
        {
            _logger = logger;
            _userManager = userManager;
            _ticketService = ticketService;
            _companyService = companyService;
        }

        // Dashboard
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == false) return RedirectToAction(nameof(LandingPage));
			else return RedirectToAction(nameof(Dashboard));
        }

        public async Task<IActionResult> Dashboard()
        {
            BTUser? currentUser = await _companyService.GetCompanyUserByIdAsync(_userManager.GetUserId(User), _companyId);
            IEnumerable<Ticket> userTickets = await _ticketService.GetTicketsByUserIdAsync(currentUser.Id, _companyId);
            IEnumerable<Ticket> importantTickets = userTickets
                .Where(t => t.TicketStatusId != (int)BTTicketStatuses.Resolved)
                .Where(t => t.TicketPriorityId != (int)BTTicketPriorities.High
                    || t.TicketPriorityId != (int)BTTicketPriorities.Urgent);

            ViewBag.Title = "Your Dashboard";
            ViewBag.CurrentUser = currentUser;
            ViewBag.UserTickets = userTickets;
            ViewBag.ImportantTickets = importantTickets;
            return View();
        }

        public IActionResult LandingPage()
        {
            return View();
        }

        public IActionResult ChrysalisException(string? exceptionMessage, int? errorCode)
        {
            return View(new ErrorViewModel { ExceptionMessage = exceptionMessage, ErrorCode = errorCode });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
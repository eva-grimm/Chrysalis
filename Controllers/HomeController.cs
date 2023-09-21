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
        private readonly IRoleService _roleService;
        private readonly ILookupService _lookupService;

        public HomeController(ILogger<HomeController> logger,
            UserManager<BTUser> userManager,
            ITicketService ticketService,
            ICompanyService companyService,
            IRoleService roleService,
            ILookupService lookupService)
        {
            _logger = logger;
            _userManager = userManager;
            _ticketService = ticketService;
            _companyService = companyService;
            _roleService = roleService;
            _lookupService = lookupService;
        }

        // Dashboard
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == false) return RedirectToAction(nameof(LandingPage));
			else return RedirectToAction(nameof(Dashboard));
        }

        public async Task<IActionResult> Dashboard()
        {
            BTUser? currentUser = await _companyService.GetCompanyUserByIdAsync(_userId, _companyId);
            ViewBag.Title = "Your Dashboard";

            return View(currentUser);
        }

        public IActionResult LandingPage()
        {
            return View();
        }

        public async Task<IActionResult> Profile(string? userId)
        {
            BTUser user = await _companyService.GetCompanyUserByIdAsync(userId, _companyId);
            return View(user);
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
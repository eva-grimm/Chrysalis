using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Chrysalis.Data;
using Chrysalis.Models;
using Chrysalis.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace Chrysalis.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly UserManager<BTUser> _userManager;

        public NotificationsController(ApplicationDbContext context,
            INotificationService notificationService,
            UserManager<BTUser> userManager)
        {
            _context = context;
            _notificationService = notificationService;
            _userManager = userManager;
        }

        // GET: Notifications
        public async Task<IActionResult> Index()
        {
            BTUser? currentUser = await _userManager.GetUserAsync(User);

            return currentUser == null
                ? throw new BadHttpRequestException("Could not find your user information", 500)
                : View(currentUser);
        }

        [HttpPost,ValidateAntiForgeryToken]
        public async Task MarkRead(int? notificationId)
        {
            BTUser? currentUser = await _userManager.GetUserAsync(User);
            bool success = await _notificationService.MarkNotificationReadAsync(notificationId, currentUser!.Id);
            if (!success) throw new BadHttpRequestException("Cannot mark the specified notification read", 400);
        }
        
        [HttpPost,ValidateAntiForgeryToken]
        public async Task MarkUnread(int? notificationId)
        {
            BTUser? currentUser = await _userManager.GetUserAsync(User);
            bool success = await _notificationService.MarkNotificationUnreadAsync(notificationId, currentUser!.Id);
            if (!success) throw new BadHttpRequestException("Cannot mark the specified notification unread", 400);
        }
    }
}

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Chrysalis.Data;
using Chrysalis.Models;
using Microsoft.AspNetCore.Authorization;
using Chrysalis.Enums;
using Chrysalis.Services.Interfaces;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Identity;
using Chrysalis.Extensions;
using System.ComponentModel.Design;

namespace Chrysalis.Controllers
{
    [Authorize(Roles = nameof(BTRoles.Admin))]
    public class InvitesController : BaseController
    {
        private readonly IProjectService _projectService;
        private readonly IDataProtector _protector;
        private readonly ICompanyService _companyService;
        private readonly IEmailSender _emailService;
        private readonly UserManager<BTUser> _userManager;
        private readonly IInviteService _inviteService;

        public InvitesController(IProjectService projectService,
            IDataProtectionProvider dataProtectionProvider,
            ICompanyService companyService,
            IEmailSender emailSender,
            UserManager<BTUser> userManager,
            IInviteService inviteService)
        {
            _projectService = projectService;
            _protector = dataProtectionProvider.CreateProtector("CF.StaRLinK.BugTr@cker.2022");
            _companyService = companyService;
            _emailService = emailSender;
            _userManager = userManager;
            _inviteService = inviteService;
        }

        // GET: Invites/Create
        public async Task<IActionResult> Create()
        {
            ViewData["ProjectId"] = new SelectList(await _projectService.GetProjectsAsync(_companyId), "Id", "Name");
            return View();
        }

        // POST: Invites/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProjectId,InviteeEmail,InviteeFirstName,InviteeLastName,Message")] Invite invite)
        {
            ModelState.Remove("InvitorId");

            if (!ModelState.IsValid)
            {
                ViewData["ProjectId"] = new SelectList(await _projectService.GetProjectsAsync(_companyId), "Id", "Name");
                return View(invite);
            }

            try
            {
                //encrypt code for invite
                Guid guid = Guid.NewGuid();

                string tokenAsString = _protector.Protect(guid.ToString());
                string emailAsString = _protector.Protect(invite.InviteeEmail!);
                string companyAsString = _protector.Protect(_companyId.ToString());

                string? callbackUrl = Url.Action("ProcessInvite", "Invites", new { tokenAsString, emailAsString, companyAsString }, protocol: Request.Scheme);

                string body = $@"{invite.Message} <br />
                       Please click the following link to join our team and start collaborating! <br />
                       <a href=""{callbackUrl}"">JOIN</a>";

                string destination = invite.InviteeEmail!;

                Company company = await _companyService.GetCompanyByIdAsync(_companyId);

                string? subject = $" Nova Tracker: {company.Name} Invite";

                await _emailService.SendEmailAsync(destination, subject, body);

                // Save invite in the DB
                invite.CompanyToken = guid;
                invite.CompanyId = _companyId;
                invite.InviteDate = DateTime.Now;
                invite.InvitorId = _userManager.GetUserId(User);
                invite.IsValid = true;

                await _inviteService.AddNewInviteAsync(invite);

                return RedirectToAction("Index", "Home");

                // TO-DO: Possibly use SWAL message
            }
            catch (Exception)
            {
                ViewData["ProjectId"] = new SelectList(await _projectService.GetProjectsAsync(_companyId), "Id", "Name");
                return View(invite);
            }
        }

        [HttpPost,ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelInvite(int? inviteId)
        {
            if (inviteId == null) return BadRequest();

            bool success = await _inviteService.CancelInviteAsync(inviteId, _companyId);
            if (!success) throw new BadHttpRequestException("Error cancelling invite", 500);

            return View("Index", "Companies");
        }
        
        [HttpPost,ValidateAntiForgeryToken]
        public async Task<IActionResult> RenewInvite(int? inviteId)
        {
            if (inviteId == null) return BadRequest();

            bool success = await _inviteService.RenewInviteAsync(inviteId, _companyId);
            if (!success) throw new BadHttpRequestException("Error renewing invite", 500);

            return View("Index", "Companies");
        }

        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> ProcessInvite(string token, string email, string company)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(company))
            {
                return NotFound();
            }

            Guid companyToken = Guid.Parse(_protector.Unprotect(token));
            string? inviteeEmail = _protector.Unprotect(email);
            int companyId = int.Parse(_protector.Unprotect(company));

            try
            {
                Invite? invite = await _inviteService.GetInviteAsync(companyToken, inviteeEmail, companyId);

                if (invite == null) return NotFound();
                else if (!invite.IsValid 
                    || (DateTime.Now - invite.InviteDate.ToLocalTime()).TotalDays <= 7) 
                    throw new BadHttpRequestException("This invite is no longer valid", 400);

                return View(invite);
            }
            catch (Exception)
            {
                return NotFound();
            }
        }
    }
}
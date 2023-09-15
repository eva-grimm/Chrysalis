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
using Chrysalis.Models.ViewModels;
using Chrysalis.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Chrysalis.Enums;

namespace Chrysalis.Controllers
{
    public class CompaniesController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<BTUser> _userManager;
        private readonly ICompanyService _companyService;
        private readonly IRoleService _roleService;

        public CompaniesController(ApplicationDbContext context,
            UserManager<BTUser> userManager,
            ICompanyService companyService,
            IRoleService roleService)
        {
            _context = context;
            _userManager = userManager;
            _companyService = companyService;
            _roleService = roleService;
        }

        // GET: Companies
        //public async Task<IActionResult> Index()
        //{
        //      return _context.Companies != null ? 
        //                  View(await _context.Companies.ToListAsync()) :
        //                  Problem("Entity set 'ApplicationDbContext.Companies'  is null.");
        //}

        // GET: Companies/Details/5
        public async Task<IActionResult> Details()
        {
            Company?  company = await _context.Companies
                .FirstOrDefaultAsync(m => m.Id == _companyId);
            if (company == null) return NotFound();

            return View(company);
        }

        // GET: Companies/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Companies/Create
        [HttpPost,ValidateAntiForgeryToken,Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Name,Description,ImageData,ImageType")] Company company)
        {
            if (ModelState.IsValid)
            {
                _context.Add(company);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(company);
        }

        // GET: Companies/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit()
        {
            Company? company = await _context.Companies.FindAsync(_companyId);
            if (company == null) return NotFound();

            return View(company);
        }

        // POST: Companies/Edit/5
        [HttpPost,ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit([Bind("Id,Name,Description,ImageData,ImageType")] Company company)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(company);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_companyService.CompanyExists(company.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(company);
        }

        // GET: Companies/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete()
        {
            Company? company = await _context.Companies
                .FirstOrDefaultAsync(m => m.Id == _companyId);
            if (company == null) return NotFound();

            return View(company);
        }

        // POST: Companies/Delete/5
        [HttpPost, ActionName("Delete"), Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed()
        {
            if (_context.Companies == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Companies'  is null.");
            }
            var company = await _context.Companies.FindAsync(_companyId);
            if (company != null)
            {
                _context.Companies.Remove(company);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet,Authorize(Roles=nameof(BTRoles.Admin))]
        public async Task<IActionResult> ManageUserRoles()
        {
            List<ManageUserRolesViewModel> model = new();

            List<BTUser> companyUsers = await _companyService.GetAllCompanyUsersAsync(_companyId);
            BTUser currentUser = await _companyService.GetCompanyUserByIdAsync(_userManager.GetUserId(User));
            companyUsers.Remove(currentUser); // to prevent removing own permissions

            foreach (BTUser companyUser in companyUsers)
            {
                IEnumerable<string>? currentRoles = await _roleService.GetUserRolesAsync(companyUser);

                ManageUserRolesViewModel viewModel = new()
                {
                    BTUser = companyUser,
                    Roles = new MultiSelectList (await _roleService.GetRolesAsync(),"Name","Name", currentRoles),
                };

                model.Add(viewModel);
            }
            return View(model);
        }

        [HttpPost,ValidateAntiForgeryToken,Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManageUserRoles(ManageUserRolesViewModel viewModel)
        {
            BTUser? user = await _companyService.GetCompanyUserByIdAsync(_userManager.GetUserId(User));

            IEnumerable<string>? currentRoles = await _roleService.GetUserRolesAsync(user);
            string? selectedRole = viewModel.SelectedRoles!.FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(selectedRole))
            {
                if (await _roleService.RemoveUserFromRolesAsync(user,currentRoles))
                {
                    await _roleService.AddUserToRoleAsync(user, selectedRole);
                }
            }

            return RedirectToAction(nameof(ManageUserRoles), viewModel);
        }
    }
}

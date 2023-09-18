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
		private readonly IFileService _fileService;

		public CompaniesController(ApplicationDbContext context,
			UserManager<BTUser> userManager,
			ICompanyService companyService,
			IRoleService roleService,
			IFileService fileService)
		{
			_context = context;
			_userManager = userManager;
			_companyService = companyService;
			_roleService = roleService;
			_fileService = fileService;
		}

		// GET: Companies
		public async Task<IActionResult> Index()
		{
			return View(await _companyService.GetCompanyByIdAsync(_companyId));
		}

		// GET: Companies/Details/5
		public async Task<IActionResult> Details()
		{
			Company? company = await _context.Companies
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
		[HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
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
			Company? company = await _context.Companies.FindAsync(_companyId)
				?? throw new BadHttpRequestException("Cannot find your company", 500);

			return View(company);
		}

		// POST: Companies/Edit/5
		[HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
		public async Task<IActionResult> Edit(int companyId)
		{
			Company? company = await _companyService.GetCompanyByIdAsync(_companyId)
				?? throw new BadHttpRequestException("Cannot find specified company", 400);

			// TO-DO: security check

			bool validUpdate = await TryUpdateModelAsync(
				company,
				string.Empty,
				c => c.Name,
				c => c.Description,
				c => c.ImageFile);

			if (!validUpdate) return View();
			else
			{
				try
				{
					if (company.ImageFile != null)
					{
						company.ImageData = await _fileService
							.ConvertFileToByteArrayAsync(company.ImageFile);
						company.ImageType = company.ImageFile.ContentType;
					}

					bool success = await _companyService.UpdateCompanyAsync(company);
					if (!success) throw new BadHttpRequestException("Problem updating the company", 500);

					return RedirectToAction(nameof(Index));
				}
				catch (Exception)
				{
					throw new BadHttpRequestException("Problem updating the company", 500);
				}
			}
		}

		public async Task<IActionResult> CompanyUsers(string? swalMessage = null)
		{
			ViewBag.SwalMessage = swalMessage;

			IEnumerable<BTUser> model = await _companyService.GetCompanyUsersAsync(_companyId);

			return View(model);
		}

		[HttpGet, Authorize(Roles = nameof(BTRoles.Admin))]
		public async Task<IActionResult> ManageUserRoles(string? incomingMessage = null)
		{
			ViewBag.SwalMessage = incomingMessage;

			BTUser? currentUser = await _userManager.GetUserAsync(User)
				?? throw new BadHttpRequestException("Error retrieving your user login", 500);
			List<BTUser> model = await _companyService.GetCompanyUsersAsync(_companyId)
				?? throw new BadHttpRequestException("Error retrieving company users", 400);
			model.Remove(currentUser);

			return View(model);
		}

		[HttpPost, ValidateAntiForgeryToken, Authorize(Roles = nameof(BTRoles.Admin))]
		public async Task<IActionResult> ManageUserRolesConfirmed(string userId, string? selected)
		{
			string? swalMessage = string.Empty;
			BTUser? currentUser = await _userManager.GetUserAsync(User);
			BTUser? userToEdit = await _companyService.GetCompanyUserByIdAsync(userId);

			if (currentUser == userToEdit) throw new BadHttpRequestException("You cannot modify your own roles", 400);

            IEnumerable<string>? currentRoles = await _roleService.GetUserRolesAsync(userToEdit);

			if (string.IsNullOrWhiteSpace(selected)) throw new BadHttpRequestException("You must select a role for the user.", 400);

			bool success = await _roleService.RemoveUserFromRolesAsync(userToEdit, currentRoles);
			if (!success) throw new BadHttpRequestException("There was a problem removing the user's existing role.", 500);

			success = await _roleService.AddUserToRoleAsync(userToEdit, selected);
			if (!success) throw new BadHttpRequestException("The user's existing role was removed, but there was a problem adding the selected role.", 500);

			swalMessage = "Success: User role changed.";
			return RedirectToAction(nameof(CompanyUsers), new { incomingMessage = swalMessage });
		}
	}
}

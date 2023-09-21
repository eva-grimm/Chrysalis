// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Chrysalis.Models;
using Chrysalis.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Chrysalis.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<BTUser> _userManager;
        private readonly SignInManager<BTUser> _signInManager;
        private IFileService _fileService;

        public IndexModel(
            UserManager<BTUser> userManager,
            SignInManager<BTUser> signInManager,
            IFileService fileService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _fileService = fileService;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            [Required, Display(Name = "First Name")]
            [StringLength(50, ErrorMessage = "The {0} must be at least {2} and max {1} characters long", MinimumLength = 2)]
            public string FirstName { get; set; }

            [Required, Display(Name = "Last Name")]
            [StringLength(50, ErrorMessage = "The {0} must be at least {2} and max {1} characters long", MinimumLength = 2)]
            public string LastName { get; set; }

            [NotMapped]
            public string FullName { get { return $"{FirstName} {LastName}"; } }

            // User Profile Image
            [NotMapped]
            public IFormFile ImageFile { get; set; }
            public byte[] ImageData { get; set; }
            public string ImageType { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Phone]
            [Display(Name = "Phone number")]
            public string PhoneNumber { get; set; }
        }

        private async Task LoadAsync(BTUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            Username = userName;

            Input = new InputModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                ImageData = user.ImageData,
                ImageType = user.ImageType,
                PhoneNumber = phoneNumber
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            BTUser user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            user.FirstName = Input.FirstName;
            user.LastName = Input.LastName;
            if (Input.ImageFile != null)
            {
                user.ImageData = await _fileService.ConvertFileToByteArrayAsync(Input.ImageFile);
                user.ImageType = Input.ImageFile.ContentType;
            }
            await _userManager.UpdateAsync(user);

            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to set phone number.";
                    return RedirectToPage();
                }
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated";
            return RedirectToPage();
        }
    }
}

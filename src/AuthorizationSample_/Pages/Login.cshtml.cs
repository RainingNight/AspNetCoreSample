using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AuthorizationSample.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace AuthorizationSample.Pages
{
    public class LoginModel : PageModel
    {
        private readonly UserStore _userStore;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(UserStore userStore, ILogger<LoginModel> logger)
        {
            _userStore = userStore;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            public string UserName { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public void OnGet(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            if (ModelState.IsValid)
            {
                var user = _userStore.FindUser(Input.UserName, Input.Password);
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "用户名或密码错误。");
                    return Page();
                }
                var claimIdentity = new ClaimsIdentity("Cookie");
                claimIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
                claimIdentity.AddClaim(new Claim(ClaimTypes.Name, user.Name));
                claimIdentity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
                claimIdentity.AddClaim(new Claim(ClaimTypes.MobilePhone, user.PhoneNumber));
                claimIdentity.AddClaim(new Claim(ClaimTypes.DateOfBirth, user.Birthday.ToString()));
                await HttpContext.SignInAsync(new ClaimsPrincipal(claimIdentity), new AuthenticationProperties { IsPersistent = Input.RememberMe });
            }
            return Page();
        }
    }
}
using AuthorizationSample.Data;
using AuthorizationSample.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AuthorizationSample.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserStore _userStore;
        private readonly ILogger<AccountController> _logger;

        public AccountController(UserStore userStore, ILogger<AccountController> logger)
        {
            _userStore = userStore;
            _logger = logger;
        }

        [TempData]
        public string ErrorMessage { get; set; }


        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var user = _userStore.Find(model.UserName, model.Password);
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "用户名或密码错误。");
                    return View(model);
                }
                else
                {
                    var claimIdentity = new ClaimsIdentity("Cookie");
                    claimIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
                    claimIdentity.AddClaim(new Claim(ClaimTypes.Name, user.Name));
                    claimIdentity.AddClaim(new Claim(ClaimTypes.Role, user.Role));
                    await HttpContext.SignInAsync(new ClaimsPrincipal(claimIdentity), new AuthenticationProperties { IsPersistent = model.RememberMe });
                    _logger.LogInformation("User logged in.");
                    return RedirectToLocal(returnUrl);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }


        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }
    }
}
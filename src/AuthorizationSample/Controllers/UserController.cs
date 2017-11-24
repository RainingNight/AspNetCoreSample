using AuthorizationSample.Authorization;
using AuthorizationSample.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AuthorizationSample.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly UserStore _userStore;
        private readonly ILogger<UserController> _logger;

        public UserController(UserStore userStore, ILogger<UserController> logger)
        {
            _userStore = userStore;
            _logger = logger;
        }


        [PermissionFilter(Permissions.UserRead)]
        public ActionResult Index()
        {
            return View(_userStore.GetAll());
        }

        [PermissionFilter(Permissions.UserRead)]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var user = _userStore.Find(id.Value);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        [PermissionFilter(Permissions.UserCreate)]
        public ActionResult Create()
        {
            return View();
        }

        [PermissionFilter(Permissions.UserCreate)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create([Bind("Title")] User user)
        {
            if (ModelState.IsValid)
            {
                _userStore.Add(user);
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        [PermissionFilter(Permissions.UserUpdate)]
        public IActionResult Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = _userStore.Find(id.Value);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        [PermissionFilter(Permissions.UserUpdate)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, [Bind("Id,Title")] User user)
        {
            if (id != user.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                _userStore.Update(id, user);
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        [PermissionFilter(Permissions.UserDelete)]
        public IActionResult Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = _userStore.Find(id.Value);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        [PermissionFilter(Permissions.UserDelete)]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var user = _userStore.Find(id);
            _userStore.Remove(user);
            return RedirectToAction(nameof(Index));
        }
    }
}
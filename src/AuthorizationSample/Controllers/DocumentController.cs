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
    public class DocumentController : Controller
    {
        private readonly DocumentStore _docStore;
        private readonly IAuthorizationService _authorizationService;
        private readonly ILogger<DocumentController> _logger;

        public DocumentController(DocumentStore docStore, IAuthorizationService authorizationService, ILogger<DocumentController> logger)
        {
            _docStore = docStore;
            _authorizationService = authorizationService;
            _logger = logger;
        }



        public ActionResult Index()
        {
            return View(_docStore.GetAll());
        }

        // GET: Documents/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var document = _docStore.Find(id.Value);
            if (document == null)
            {
                return NotFound();
            }
            if ((await _authorizationService.AuthorizeAsync(User, document, Operations.Read)).Succeeded)
            {
                return View(document);
            }
            else
            {
                return new ForbidResult();
            }
        }

        // GET: Documents/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Documents/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create([Bind("Title")] Document document)
        {
            if (ModelState.IsValid)
            {
                document.Creator = User.Identity.Name;
                document.CreationTime = DateTime.Now;
                _docStore.Add(document);
                return RedirectToAction(nameof(Index));
            }
            return View(document);
        }

        // GET: Documents/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var document = _docStore.Find(id.Value);
            if (document == null)
            {
                return NotFound();
            }
            if ((await _authorizationService.AuthorizeAsync(User, document, Operations.Update)).Succeeded)
            {
                return View(document);
            }
            else
            {
                return new ForbidResult();
            }
        }

        // POST: Documents/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, [Bind("Id,Title")] Document document)
        {
            if (id != document.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                _docStore.Update(id, document);
                return RedirectToAction(nameof(Index));
            }
            return View(document);
        }

        // GET: Documents/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var document = _docStore.Find(id.Value);
            if (document == null)
            {
                return NotFound();
            }
            if ((await _authorizationService.AuthorizeAsync(User, document, Operations.Delete)).Succeeded)
            {
                return View(document);
            }
            else
            {
                return new ForbidResult();
            }
        }

        // POST: Documents/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var document = _docStore.Find(id);
            if ((await _authorizationService.AuthorizeAsync(User, document, Operations.Delete)).Succeeded)
            {
                _docStore.Remove(document);
                return RedirectToAction(nameof(Index));
            }
            else
            {
                return new ForbidResult();
            }
        }

        private bool DocumentExists(int id)
        {
            return _docStore.Exists(id);
        }
    }
}
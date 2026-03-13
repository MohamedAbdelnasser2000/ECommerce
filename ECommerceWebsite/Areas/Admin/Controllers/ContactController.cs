using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerceWebsite.Repository;
using System.Threading.Tasks;
using System.Linq;

namespace ECommerceWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ContactController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public ContactController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 20, bool unreadOnly = false)
        {
            var messages = await _unitOfWork.ContactMessage.GetAllAsync(
                filter: unreadOnly ? (m => !m.IsRead) : null
            );

            var ordered = messages
                .OrderBy(m => m.IsRead)
                .ThenByDescending(m => m.CreatedAt);

            var total = ordered.Count();
            var items = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.Total = total;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.UnreadOnly = unreadOnly;

            return View(items);
        }

        public async Task<IActionResult> Details(int id)
        {
            var msg = await _unitOfWork.ContactMessage.GetByIdAsync(id);
            if (msg == null) return NotFound();

            if (!msg.IsRead)
            {
                msg.IsRead = true;
                _unitOfWork.ContactMessage.Update(msg);
                await _unitOfWork.SaveAsync();
            }
            return View(msg);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(int id)
        {
            var msg = await _unitOfWork.ContactMessage.GetByIdAsync(id);
            if (msg == null) return NotFound();
            if (!msg.IsRead)
            {
                msg.IsRead = true;
                _unitOfWork.ContactMessage.Update(msg);
                await _unitOfWork.SaveAsync();
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var msg = await _unitOfWork.ContactMessage.GetByIdAsync(id);
            if (msg == null) return NotFound();
            _unitOfWork.ContactMessage.Remove(msg);
            await _unitOfWork.SaveAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}

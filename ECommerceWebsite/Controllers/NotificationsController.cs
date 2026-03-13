using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceWebsite.Controllers
{
    [Authorize]
    [Route("Notifications")]
    [Route("Notification")] // support singular path
    public class NotificationsController : Controller
    {
        [HttpGet("")]
        [HttpGet("Index")]
        public IActionResult Index()
        {
            return View();
        }
    }
}

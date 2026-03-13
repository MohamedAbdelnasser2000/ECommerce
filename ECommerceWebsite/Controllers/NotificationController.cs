using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ECommerceWebsite.Models;
using ECommerceWebsite.Services;
using Microsoft.Extensions.Logging;

namespace ECommerceWebsite.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(
            INotificationService notificationService,
            UserManager<ApplicationUser> userManager,
            ILogger<NotificationController> logger)
        {
            _notificationService = notificationService;
            _userManager = userManager;
            _logger = logger;
        }

        // POST: api/Notification/mark-all-read
        [HttpPost("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                await _notificationService.MarkAllAsReadAsync(userId);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return StatusCode(500, "An error occurred while marking all notifications as read");
            }
        }

        // GET: api/notification
        [HttpGet]
        public async Task<ActionResult<IEnumerable<NotificationDto>>> GetNotifications(bool unreadOnly = false)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var notifications = await _notificationService.GetUserNotificationsAsync(userId, unreadOnly);
                var dtos = notifications.Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type.ToString(),
                    Icon = n.Icon,
                    ActionUrl = n.ActionUrl,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                });
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notifications");
                return StatusCode(500, new { message = "An error occurred while retrieving notifications." });
            }
        }

        // GET: api/Notification/count
        [HttpGet("count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var count = await _notificationService.GetUnreadCountAsync(userId);
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread notification count");
                return StatusCode(500, "An error occurred while getting unread notification count");
            }
        }

        // POST: api/Notification/mark-read/{id}
        [HttpPost("mark-read/{id}")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                await _notificationService.MarkAsReadAsync(id);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {NotificationId} as read", id);
                return StatusCode(500, "An error occurred while marking the notification as read");
            }
        }

        // GET: api/Notification/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetNotification(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var notification = await _notificationService.GetNotificationByIdAsync(id);
                if (notification == null || notification.UserId != userId)
                {
                    return NotFound();
                }

                var dto = new NotificationDto
                {
                    Id = notification.Id,
                    Title = notification.Title,
                    Message = notification.Message,
                    Type = notification.Type.ToString(),
                    Icon = notification.Icon,
                    ActionUrl = notification.ActionUrl,
                    IsRead = notification.IsRead,
                    CreatedAt = notification.CreatedAt
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notification {NotificationId}", id);
                return StatusCode(500, "An error occurred while retrieving the notification");
            }
        }

    }

    public class NotificationDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; }
        public string Icon { get; set; }
        public string ActionUrl { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

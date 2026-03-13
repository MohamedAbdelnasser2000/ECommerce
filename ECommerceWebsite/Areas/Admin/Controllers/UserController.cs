using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ECommerceWebsite.Models;
using ECommerceWebsite.Repository;
using System.Text;
using System.Linq;

namespace ECommerceWebsite.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class UserController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UserController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IActionResult> Index(string? search, string? role, string? status, int page = 1, int pageSize = 20)
    {
        // Ensure we materialize the result into a List to avoid dynamic/extension method ambiguities
        var users = (await _unitOfWork.ApplicationUser.GetAllAsync(
            includeProperties: "Orders"
        )).ToList();

        // Apply search filter
        if (!string.IsNullOrEmpty(search))
        {
            users = users.Where(u =>
                u.FirstName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                u.LastName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (u.Email != null && u.Email.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (u.UserName != null && u.UserName.Contains(search, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }

        // Apply status filter
        if (!string.IsNullOrEmpty(status))
        {
            if (status == "verified")
                users = users.Where(u => u.EmailConfirmed).ToList();
            else if (status == "unverified")
                users = users.Where(u => !u.EmailConfirmed).ToList();
        }

        // Get user roles and order counts
        var userRoles = new Dictionary<string, List<string>>();
        var userOrders = new Dictionary<string, int>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userRoles[user.Id] = roles.ToList();

            var orderCount = user.Orders?.Count ?? 0;
            userOrders[user.Id] = orderCount;
        }

        // Apply role filter
        if (!string.IsNullOrEmpty(role))
        {
            var filteredUsers = new List<ApplicationUser>();
            foreach (var user in users)
            {
                if (userRoles.ContainsKey(user.Id) && userRoles[user.Id].Contains(role))
                {
                    filteredUsers.Add(user);
                }
            }
            users = filteredUsers;
        }

        var orderedUsers = users.OrderByDescending(u => u.CreatedAt).ToList();
        var pagedUsers = orderedUsers.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        ViewBag.UserRoles = userRoles;
        ViewBag.UserOrders = userOrders;
        ViewBag.Search = search;
        ViewBag.Role = role;
        ViewBag.Status = status;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)users.Count / pageSize);
        ViewBag.TotalUsers = users.Count;

        return View(pagedUsers);
    }

    // GET: Create User
    public IActionResult Create()
    {
        return View(new CreateUserViewModel());
    }

    // POST: Create User
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        if (ModelState.IsValid)
        {
            // Check if email already exists
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "A user with this email already exists.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber,
                Address = model.Address,
                City = model.City,
                PostalCode = model.PostalCode,
                EmailConfirmed = model.EmailConfirmed,
                CreatedAt = DateTime.Now
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                // Normalize role and ensure it exists before assignment
                var normalizedRole = (model.Role ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(normalizedRole))
                {
                    ModelState.AddModelError("Role", "Role is required.");
                    return View(model);
                }

                // Identity is case-insensitive but we ensure canonical role names
                if (!await _roleManager.RoleExistsAsync(normalizedRole))
                {
                    var createRoleResult = await _roleManager.CreateAsync(new IdentityRole(normalizedRole));
                    if (!createRoleResult.Succeeded)
                    {
                        foreach (var error in createRoleResult.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }
                        return View(model);
                    }
                }

                await _userManager.AddToRoleAsync(user, normalizedRole);

                // Send welcome email if requested
                if (model.SendWelcomeEmail)
                {
                    // TODO: Implement email sending
                    // await _emailService.SendWelcomeEmailAsync(user.Email, user.FirstName);
                }

                TempData["Success"] = $"User '{user.FirstName} {user.LastName}' created successfully!";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
        }

        return View(model);
    }

    public async Task<IActionResult> Details(string id)
    {
        var user = await _unitOfWork.ApplicationUser.GetFirstOrDefaultAsync(
            filter: u => u.Id == id,
            includeProperties: "Orders,Orders.OrderItems,Orders.OrderItems.Product,Reviews,CartItems,WishlistItems"
        );

        if (user == null)
        {
            return NotFound();
        }

        // Calculate user statistics
        ViewBag.TotalOrders = user.Orders.Count;
        ViewBag.TotalSpent = user.Orders.Where(o => o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount);
        ViewBag.PendingOrders = user.Orders.Count(o => o.Status == OrderStatus.Pending);
        ViewBag.CompletedOrders = user.Orders.Count(o => o.Status == OrderStatus.Delivered);
        ViewBag.TotalReviews = user.Reviews.Count;
        ViewBag.CartItemsCount = user.CartItems.Count;
        ViewBag.WishlistItemsCount = user.WishlistItems.Count;

        return View(user);
    }

    public async Task<IActionResult> Edit(string id)
    {
        var user = await _unitOfWork.ApplicationUser.GetFirstOrDefaultAsync(
            filter: u => u.Id == id
        );

        if (user == null)
        {
            return NotFound();
        }

        // Get user statistics
        var orders = user.Orders ?? new List<Order>();
        var reviews = (await _unitOfWork.Review.GetAllAsync(filter: r => r.UserId == id)).ToList();
        var cartItems = (await _unitOfWork.CartItem.GetAllAsync(filter: c => c.UserId == id)).ToList();
        var wishlistItems = (await _unitOfWork.WishlistItem.GetAllAsync(filter: w => w.UserId == id)).ToList();

        ViewBag.TotalOrders = orders.Count;
        ViewBag.CompletedOrders = orders.Count(o => o.Status == OrderStatus.Delivered);
        ViewBag.PendingOrders = orders.Count(o => o.Status == OrderStatus.Pending);
        ViewBag.TotalSpent = orders.Where(o => o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount);
        ViewBag.TotalReviews = reviews.Count;
        ViewBag.CartItemsCount = cartItems.Count;
        ViewBag.WishlistItemsCount = wishlistItems.Count;

        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, ApplicationUser model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        // Update user properties
        user.FirstName = model.FirstName ?? string.Empty;
        user.LastName = model.LastName ?? string.Empty;
        user.PhoneNumber = model.PhoneNumber;
        user.Address = model.Address ?? string.Empty;
        user.City = model.City ?? string.Empty;
        user.PostalCode = model.PostalCode ?? string.Empty;

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            TempData["Success"] = "User updated successfully!";
            return RedirectToAction(nameof(Details), new { id });
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ToggleLockout(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return Json(new { success = false, message = "User not found" });
        }

        var isLockedOut = await _userManager.IsLockedOutAsync(user);

        if (isLockedOut)
        {
            // Unlock user
            await _userManager.SetLockoutEndDateAsync(user, null);
            return Json(new { success = true, isLocked = false, message = "User unlocked successfully" });
        }
        else
        {
            // Lock user for 100 years (effectively permanent)
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            return Json(new { success = true, isLocked = true, message = "User locked successfully" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return Json(new { success = false, message = "User not found" });
        }

        // Generate password reset token
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        // Generate a temporary password
        var tempPassword = GenerateTemporaryPassword();

        // Reset password
        var result = await _userManager.ResetPasswordAsync(user, token, tempPassword);

        if (result.Succeeded)
        {
            return Json(new {
                success = true,
                message = "Password reset successfully",
                tempPassword = tempPassword
            });
        }

        return Json(new { success = false, message = "Failed to reset password" });
    }

    public async Task<IActionResult> Delete(string id)
    {
        var user = await _unitOfWork.ApplicationUser.GetFirstOrDefaultAsync(
            filter: u => u.Id == id,
            includeProperties: "Orders"
        );

        if (user == null)
        {
            return NotFound();
        }

        // Check if user has orders
        if (user.Orders.Any())
        {
            TempData["Error"] = "Cannot delete user with existing orders!";
            return RedirectToAction(nameof(Details), new { id });
        }

        return View(user);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        // Check if user has orders
        var hasOrders = await _unitOfWork.Order.AnyAsync(o => o.UserId == id);
        if (hasOrders)
        {
            TempData["Error"] = "Cannot delete user with existing orders!";
            return RedirectToAction(nameof(Index));
        }

        var result = await _userManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            TempData["Success"] = "User deleted successfully!";
        }
        else
        {
            TempData["Error"] = "Failed to delete user!";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteUser(string id)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            // Check if user has orders
            var hasOrders = await _unitOfWork.Order.AnyAsync(o => o.UserId == id);
            if (hasOrders)
            {
                return Json(new { success = false, message = "Cannot delete user with existing orders!" });
            }

            // Don't allow deleting admin user
            if (user.Email == "admin@ecommerce.com")
            {
                return Json(new { success = false, message = "Cannot delete admin user!" });
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                return Json(new { success = true, message = "User deleted successfully!" });
            }
            else
            {
                return Json(new { success = false, message = "Failed to delete user!" });
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error deleting user" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetUserStats()
    {
        var users = (await _unitOfWork.ApplicationUser.GetAllAsync(includeProperties: "Orders")).ToList();

        var stats = new
        {
            total = users.Count,
            activeToday = users.Count(u => u.CreatedAt.Date == DateTime.Today),
            withOrders = users.Count(u => (u.Orders?.Count ?? 0) > 0),
            totalRevenue = users.SelectMany(u => u.Orders ?? Enumerable.Empty<Order>())
                               .Where(o => o.Status == OrderStatus.Delivered)
                               .Sum(o => o.TotalAmount)
        };

        return Json(stats);
    }

    [HttpPost]
    public async Task<IActionResult> SendNotification(string id, string subject, string message)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return Json(new { success = false, message = "User not found" });
        }

        // Here you would implement email sending logic
        // For now, we'll just return success
        _ = subject;
        _ = message;

        return Json(new { success = true, message = "Notification sent successfully" });
    }

    // Export Users to CSV
    public async Task<IActionResult> Export()
    {
        var users = (await _unitOfWork.ApplicationUser.GetAllAsync(includeProperties: "Orders")).ToList();

        var csv = new StringBuilder();
        csv.AppendLine("First Name,Last Name,Email,Phone,Address,City,Postal Code,Email Confirmed,Created Date,Total Orders,Role");

        string EscapeCsv(string? input) => (input ?? string.Empty).Replace("\"", "\"\"");

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var roleString = string.Join(";", roles);

            var fields = new[] {
                user.FirstName,
                user.LastName,
                user.Email,
                user.PhoneNumber ?? string.Empty,
                user.Address ?? string.Empty,
                user.City ?? string.Empty,
                user.PostalCode ?? string.Empty,
                user.EmailConfirmed.ToString(),
                user.CreatedAt.ToString("yyyy-MM-dd"),
                (user.Orders?.Count ?? 0).ToString(),
                roleString
            };

            var line = string.Join(",", fields.Select(f => $"\"{EscapeCsv(f)}\""));
            csv.AppendLine(line);
        }

        var fileName = $"Users_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        var bytes = Encoding.UTF8.GetBytes(csv.ToString());

        return File(bytes, "text/csv", fileName);
    }

    // Bulk Export with Filters
    public async Task<IActionResult> ExportFiltered(string? search, string? role, string? status)
    {
        var users = (await _unitOfWork.ApplicationUser.GetAllAsync(includeProperties: "Orders")).ToList();

        // Apply same filters as Index
        if (!string.IsNullOrEmpty(search))
        {
            users = users.Where(u =>
                u.FirstName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                u.LastName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (u.Email != null && u.Email.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (u.UserName != null && u.UserName.Contains(search, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }

        if (!string.IsNullOrEmpty(status))
        {
            if (status == "verified")
                users = users.Where(u => u.EmailConfirmed).ToList();
            else if (status == "unverified")
                users = users.Where(u => !u.EmailConfirmed).ToList();
        }

        // Filter by role
        if (!string.IsNullOrEmpty(role))
        {
            var filteredUsers = new List<ApplicationUser>();
            foreach (var user in users)
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                if (userRoles.Contains(role))
                {
                    filteredUsers.Add(user);
                }
            }
            users = filteredUsers;
        }

        var csv = new StringBuilder();
        csv.AppendLine("First Name,Last Name,Email,Phone,Address,City,Postal Code,Email Confirmed,Created Date,Total Orders,Role");

        string EscapeCsv2(string? input) => (input ?? string.Empty).Replace("\"", "\"\"");

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var roleString = string.Join(";", roles);

            var fields = new[] {
                user.FirstName,
                user.LastName,
                user.Email,
                user.PhoneNumber ?? string.Empty,
                user.Address ?? string.Empty,
                user.City ?? string.Empty,
                user.PostalCode ?? string.Empty,
                user.EmailConfirmed.ToString(),
                user.CreatedAt.ToString("yyyy-MM-dd"),
                (user.Orders?.Count ?? 0).ToString(),
                roleString
            };

            var line = string.Join(",", fields.Select(f => $"\"{EscapeCsv2(f)}\""));
            csv.AppendLine(line);
        }

        var fileName = $"Users_Filtered_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        var bytes = Encoding.UTF8.GetBytes(csv.ToString());

        return File(bytes, "text/csv", fileName);
    }

    private string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 12)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}

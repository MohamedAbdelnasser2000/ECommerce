using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ECommerceWebsite.Models;
using ECommerceWebsite.Repository;
using ECommerceWebsite.Services;

namespace ECommerceWebsite.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class OrderController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;
    private readonly UserManager<ApplicationUser> _userManager;

    public OrderController(IUnitOfWork unitOfWork, IEmailService emailService, INotificationService notificationService, UserManager<ApplicationUser> userManager)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _notificationService = notificationService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? status, string? search, int page = 1, int pageSize = 20)
    {
        var orders = await _unitOfWork.Order.GetAllAsync(
            includeProperties: "User,OrderItems,OrderItems.Product"
        );

        // Apply filters
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, out var orderStatus))
        {
            orders = orders.Where(o => o.Status == orderStatus);
        }

        if (!string.IsNullOrEmpty(search))
        {
            orders = orders.Where(o =>
                o.OrderNumber.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                o.User.FirstName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                o.User.LastName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (o.User.Email != null && o.User.Email.Contains(search, StringComparison.OrdinalIgnoreCase))
            );
        }

        var orderedOrders = orders.OrderByDescending(o => o.OrderDate);
        var pagedOrders = orderedOrders.Skip((page - 1) * pageSize).Take(pageSize);

        ViewBag.CurrentStatus = status;
        ViewBag.CurrentSearch = search;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)orders.Count() / pageSize);
        ViewBag.TotalOrders = orders.Count();

        return View(pagedOrders);
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await _unitOfWork.Order.GetFirstOrDefaultAsync(
            filter: o => o.Id == id,
            includeProperties: "User,OrderItems,OrderItems.Product,OrderItems.Product.Category"
        );

        if (order == null)
        {
            return NotFound();
        }

        return View(order);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var order = await _unitOfWork.Order.GetFirstOrDefaultAsync(
            filter: o => o.Id == id,
            includeProperties: "User,OrderItems,OrderItems.Product"
        );

        if (order == null)
        {
            return NotFound();
        }

        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, OrderStatus status, string? notes)
    {
        var order = await _unitOfWork.Order.GetByIdAsync(id);
        if (order == null)
        {
            return NotFound();
        }

        var oldStatus = order.Status;
        order.Status = status;

        if (!string.IsNullOrEmpty(notes))
        {
            order.Notes = notes;
        }

        // Update timestamps based on status
        switch (status)
        {
            case OrderStatus.Shipped:
                if (oldStatus != OrderStatus.Shipped)
                    order.ShippedDate = DateTime.Now;
                break;
            case OrderStatus.Delivered:
                if (oldStatus != OrderStatus.Delivered)
                    order.DeliveredDate = DateTime.Now;
                break;
        }

        _unitOfWork.Order.Update(order);
        await _unitOfWork.SaveAsync();

        // Send notification and email to customer
        try
        {
            var user = await _userManager.FindByIdAsync(order.UserId);
            if (user != null)
            {
                // Send email notification
                await _emailService.SendOrderStatusUpdateAsync(user.Email, order);

                // Send in-app notification
                var message = status switch
                {
                    OrderStatus.Processing => "Your order is now being processed",
                    OrderStatus.Shipped => "Your order has been shipped and is on its way",
                    OrderStatus.Delivered => "Your order has been delivered successfully",
                    OrderStatus.Cancelled => "Your order has been cancelled",
                    _ => $"Your order status has been updated to {status}"
                };

                await _notificationService.CreateOrderNotificationAsync(order.UserId, order, message);
            }
        }
        catch (Exception ex)
        {
            // Log error but don't fail the status update
            // _logger.LogError(ex, "Failed to send notification for order {OrderId}", id);
        }

        TempData["Success"] = $"Order status updated to {status}";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> QuickStatusUpdate(int id, OrderStatus status)
    {
        var order = await _unitOfWork.Order.GetByIdAsync(id);
        if (order == null)
        {
            return Json(new { success = false, message = "Order not found" });
        }

        var oldStatus = order.Status;
        order.Status = status;

        // Update timestamps based on status
        switch (status)
        {
            case OrderStatus.Shipped:
                if (oldStatus != OrderStatus.Shipped)
                    order.ShippedDate = DateTime.Now;
                break;
            case OrderStatus.Delivered:
                if (oldStatus != OrderStatus.Delivered)
                    order.DeliveredDate = DateTime.Now;
                break;
        }

        _unitOfWork.Order.Update(order);
        await _unitOfWork.SaveAsync();

        return Json(new { success = true, status = status.ToString() });
    }

    [HttpPost]
    public async Task<IActionResult> QuickPaymentUpdate(int id, PaymentStatus paymentStatus)
    {
        var order = await _unitOfWork.Order.GetByIdAsync(id);
        if (order == null)
        {
            return Json(new { success = false, message = "Order not found" });
        }

        order.PaymentStatus = paymentStatus;
        _unitOfWork.Order.Update(order);
        await _unitOfWork.SaveAsync();

        return Json(new { success = true, paymentStatus = paymentStatus.ToString() });
    }

    public async Task<IActionResult> Delete(int id)
    {
        var order = await _unitOfWork.Order.GetFirstOrDefaultAsync(
            filter: o => o.Id == id,
            includeProperties: "User,OrderItems,OrderItems.Product"
        );

        if (order == null)
        {
            return NotFound();
        }

        // Only allow deletion of cancelled orders
        if (order.Status != OrderStatus.Cancelled)
        {
            TempData["Error"] = "Only cancelled orders can be deleted!";
            return RedirectToAction(nameof(Details), new { id });
        }

        return View(order);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var order = await _unitOfWork.Order.GetFirstOrDefaultAsync(
            filter: o => o.Id == id,
            includeProperties: "OrderItems"
        );

        if (order == null)
        {
            return NotFound();
        }

        // Only allow deletion of cancelled orders
        if (order.Status != OrderStatus.Cancelled)
        {
            TempData["Error"] = "Only cancelled orders can be deleted!";
            return RedirectToAction(nameof(Index));
        }

        // Remove order items first
        _unitOfWork.OrderItem.RemoveRange(order.OrderItems);

        // Remove order
        _unitOfWork.Order.Remove(order);
        await _unitOfWork.SaveAsync();

        TempData["Success"] = "Order deleted successfully!";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> GetOrderStats()
    {
        var orders = await _unitOfWork.Order.GetAllAsync();

        var stats = new
        {
            total = orders.Count(),
            pending = orders.Count(o => o.Status == OrderStatus.Pending),
            processing = orders.Count(o => o.Status == OrderStatus.Processing),
            shipped = orders.Count(o => o.Status == OrderStatus.Shipped),
            delivered = orders.Count(o => o.Status == OrderStatus.Delivered),
            cancelled = orders.Count(o => o.Status == OrderStatus.Cancelled),
            totalRevenue = orders.Where(o => o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount)
        };

        return Json(stats);
    }

    [HttpPost]
    public async Task<IActionResult> BulkStatusUpdate(int[] orderIds, OrderStatus status)
    {
        if (orderIds == null || orderIds.Length == 0)
        {
            return Json(new { success = false, message = "No orders selected" });
        }

        var orders = await _unitOfWork.Order.GetAllAsync(filter: o => orderIds.Contains(o.Id));

        foreach (var order in orders)
        {
            var oldStatus = order.Status;
            order.Status = status;

            // Update timestamps based on status
            switch (status)
            {
                case OrderStatus.Shipped:
                    if (oldStatus != OrderStatus.Shipped)
                        order.ShippedDate = DateTime.Now;
                    break;
                case OrderStatus.Delivered:
                    if (oldStatus != OrderStatus.Delivered)
                        order.DeliveredDate = DateTime.Now;
                    break;
            }

            _unitOfWork.Order.Update(order);
        }

        await _unitOfWork.SaveAsync();

        return Json(new { success = true, message = $"{orders.Count()} orders updated to {status}" });
    }

    [HttpPost]
    public async Task<IActionResult> BulkPaymentUpdate(int[] orderIds, PaymentStatus paymentStatus)
    {
        if (orderIds == null || orderIds.Length == 0)
        {
            return Json(new { success = false, message = "No orders selected" });
        }

        var orders = await _unitOfWork.Order.GetAllAsync(filter: o => orderIds.Contains(o.Id));

        foreach (var order in orders)
        {
            order.PaymentStatus = paymentStatus;
            _unitOfWork.Order.Update(order);
        }

        await _unitOfWork.SaveAsync();

        return Json(new { success = true, message = $"{orders.Count()} orders payment status updated to {paymentStatus}" });
    }
}

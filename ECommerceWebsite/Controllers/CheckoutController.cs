using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ECommerceWebsite.Models;
using ECommerceWebsite.Repository;
using ECommerceWebsite.Services;
using Newtonsoft.Json;

namespace ECommerceWebsite.Controllers;

public class CheckoutController : BaseController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;
    private readonly IInventoryService _inventoryService;

    public CheckoutController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, IEmailService emailService, INotificationService notificationService, IInventoryService inventoryService) : base(unitOfWork)
    {
        _userManager = userManager;
        _emailService = emailService;
        _notificationService = notificationService;
        _inventoryService = inventoryService;
    }

    // GET: Checkout
    public async Task<IActionResult> Index()
    {
        List<CartItem> cartItems;

        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            cartItems = (await _unitOfWork.CartItem.GetAllAsync(
                filter: c => c.UserId == user.Id,
                includeProperties: "Product,Product.Category"
            )).ToList();

            if (!cartItems.Any())
            {
                TempData["Error"] = "Your cart is empty!";
                return RedirectToAction("Index", "Cart");
            }

            // Check stock availability
            foreach (var item in cartItems)
            {
                if (item.Product.StockQuantity < item.Quantity)
                {
                    TempData["Error"] = $"Insufficient stock for {item.Product.Name}. Available: {item.Product.StockQuantity}";
                    return RedirectToAction("Index", "Cart");
                }
            }

            // Check for applied coupon
            decimal discountAmount = 0;
            string? appliedCouponCode = HttpContext.Session.GetString("AppliedCouponCode");
            string? discountAmountStr = HttpContext.Session.GetString("DiscountAmount");

            if (!string.IsNullOrEmpty(appliedCouponCode) && !string.IsNullOrEmpty(discountAmountStr))
            {
                decimal.TryParse(discountAmountStr, out discountAmount);
            }

            // Get active shipping methods
            var activeShippingMethods = (await _unitOfWork.ShippingMethod.GetAllAsync(
                filter: sm => sm.IsActive
            )).ToList();

            // Get active tax settings
            var activeTaxSettings = (await _unitOfWork.TaxSetting.GetAllAsync(
                filter: ts => ts.IsActive
            )).ToList();

            var model = new CheckoutViewModel
            {
                CartItems = cartItems,
                ShippingFirstName = user.FirstName,
                ShippingLastName = user.LastName,
                ShippingAddress = user.Address ?? "",
                ShippingCity = user.City ?? "",
                ShippingPostalCode = user.PostalCode ?? "",
                ShippingPhone = user.PhoneNumber ?? "",
                PaymentMethod = PaymentMethod.CreditCard,
                DiscountAmount = discountAmount,
                AppliedCouponCode = appliedCouponCode,
                AvailableShippingMethods = activeShippingMethods,
                AvailableTaxSettings = activeTaxSettings,
                ShippingCountry = user.Country ?? "",
                ShippingRegion = user.City ?? ""
            };

            // Set initial calculated values
            model.SubtotalValue = model.Subtotal;
            model.ShippingValue = model.Shipping;
            model.TaxValue = model.Tax;
            model.TotalValue = model.Total;

            return View(model);
        }
        else
        {
            // Guest checkout
            var sessionCart = GetSessionCart();
            cartItems = new List<CartItem>();

            foreach (var item in sessionCart)
            {
                var product = await _unitOfWork.Product.GetFirstOrDefaultAsync(
                    filter: p => p.Id == item.ProductId,
                    includeProperties: "Category"
                );
                if (product != null)
                {
                    if (product.StockQuantity < item.Quantity)
                    {
                        TempData["Error"] = $"Insufficient stock for {product.Name}. Available: {product.StockQuantity}";
                        return RedirectToAction("Index", "Cart");
                    }

                    cartItems.Add(new CartItem
                    {
                        ProductId = product.Id,
                        Product = product,
                        Quantity = item.Quantity
                    });
                }
            }

            if (!cartItems.Any())
            {
                TempData["Error"] = "Your cart is empty!";
                return RedirectToAction("Index", "Cart");
            }

            // Check for applied coupon
            decimal discountAmount = 0;
            string? appliedCouponCode = HttpContext.Session.GetString("AppliedCouponCode");
            string? discountAmountStr = HttpContext.Session.GetString("DiscountAmount");

            if (!string.IsNullOrEmpty(appliedCouponCode) && !string.IsNullOrEmpty(discountAmountStr))
            {
                decimal.TryParse(discountAmountStr, out discountAmount);
            }

            // Get active shipping methods
            var activeShippingMethods = (await _unitOfWork.ShippingMethod.GetAllAsync(
                filter: sm => sm.IsActive
            )).ToList();

            // Get active tax settings
            var activeTaxSettings = (await _unitOfWork.TaxSetting.GetAllAsync(
                filter: ts => ts.IsActive
            )).ToList();

            var model = new CheckoutViewModel
            {
                CartItems = cartItems,
                PaymentMethod = PaymentMethod.CreditCard,
                DiscountAmount = discountAmount,
                AppliedCouponCode = appliedCouponCode,
                AvailableShippingMethods = activeShippingMethods,
                AvailableTaxSettings = activeTaxSettings
            };

            // Set initial calculated values
            model.SubtotalValue = model.Subtotal;
            model.ShippingValue = model.Shipping;
            model.TaxValue = model.Tax;
            model.TotalValue = model.Total;

            return View(model);
        }
    }

    // POST: Checkout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(CheckoutViewModel model)
    {
        List<CartItem> cartItems;
        bool isGuest = User.Identity?.IsAuthenticated != true;

        if (isGuest)
        {
            // Guest checkout
            var sessionCart = GetSessionCart();
            cartItems = new List<CartItem>();

            foreach (var item in sessionCart)
            {
                var product = await _unitOfWork.Product.GetFirstOrDefaultAsync(
                    filter: p => p.Id == item.ProductId,
                    includeProperties: "Category"
                );
                if (product != null)
                {
                    cartItems.Add(new CartItem
                    {
                        ProductId = product.Id,
                        Product = product,
                        Quantity = item.Quantity
                    });
                }
            }

            // Validate required fields for guests
            if (string.IsNullOrEmpty(model.GuestEmail))
            {
                ModelState.AddModelError("GuestEmail", "Email is required for guest checkout");
            }
        }
        else
        {
            // Authenticated user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            cartItems = (await _unitOfWork.CartItem.GetAllAsync(
                filter: c => c.UserId == user.Id,
                includeProperties: "Product,Product.Category"
            )).ToList();
        }

        if (!cartItems.Any())
        {
            TempData["Error"] = "Your cart is empty!";
            return RedirectToAction("Index", "Cart");
        }

        // Validate shipping method selection
        if (!model.SelectedShippingMethodId.HasValue || !model.AvailableShippingMethods.Any(sm => sm.Id == model.SelectedShippingMethodId))
        {
            ModelState.AddModelError("SelectedShippingMethodId", "Please select a shipping method");
        }

        // Validate tax setting selection
        if (!model.AppliedTaxSettingId.HasValue || !model.AvailableTaxSettings.Any(ts => ts.Id == model.AppliedTaxSettingId))
        {
            ModelState.AddModelError("AppliedTaxSettingId", "Please select a tax setting");
        }

        // Check stock availability again
        foreach (var item in cartItems)
        {
            if (item.Product.StockQuantity < item.Quantity)
            {
                ModelState.AddModelError("", $"Insufficient stock for {item.Product.Name}. Available: {item.Product.StockQuantity}");
                return View(model);
            }
        }

        try
        {
            // Calculate final totals based on selections
            decimal subtotal = cartItems.Sum(item => item.TotalPrice);
            decimal shippingCost = 0;
            if (model.SelectedShippingMethodId.HasValue)
            {
                var selectedShipping = model.AvailableShippingMethods.FirstOrDefault(sm => sm.Id == model.SelectedShippingMethodId);
                if (selectedShipping != null)
                {
                    shippingCost = selectedShipping.Cost;
                }
            }

            decimal taxAmount = 0;
            if (model.AppliedTaxSettingId.HasValue)
            {
                var selectedTax = model.AvailableTaxSettings.FirstOrDefault(ts => ts.Id == model.AppliedTaxSettingId);
                if (selectedTax != null)
                {
                    taxAmount = subtotal * (selectedTax.TaxRate / 100);
                }
            }

            decimal discountAmount = model.DiscountAmount;

            // Set the calculated values in the model for form posting
            model.SubtotalValue = subtotal;
            model.ShippingValue = shippingCost;
            model.TaxValue = taxAmount;
            model.TotalValue = subtotal + shippingCost + taxAmount - discountAmount;

            // Create order
            var order = new Order
            {
                UserId = isGuest ? null : (await _userManager.GetUserAsync(User))?.Id,
                GuestEmail = isGuest ? model.GuestEmail : null,
                OrderNumber = GenerateOrderNumber(),
                OrderDate = DateTime.Now,
                Status = OrderStatus.Pending,
                PaymentMethod = model.PaymentMethod,
                PaymentStatus = PaymentStatus.Pending,
                ShippingFirstName = model.ShippingFirstName,
                ShippingLastName = model.ShippingLastName,
                ShippingAddress = model.ShippingAddress,
                ShippingCity = model.ShippingCity,
                ShippingPostalCode = model.ShippingPostalCode,
                ShippingPhone = model.ShippingPhone,
                ShippingCountry = model.ShippingCountry,
                ShippingRegion = model.ShippingRegion,
                Subtotal = model.SubtotalValue,
                ShippingCost = model.ShippingValue,
                TaxAmount = model.TaxValue,
                TotalAmount = model.TotalValue,
                Notes = model.Notes
            };

            // Set shipping method name if selected
            if (model.SelectedShippingMethodId.HasValue)
            {
                var selectedShipping = model.AvailableShippingMethods.FirstOrDefault(sm => sm.Id == model.SelectedShippingMethodId);
                if (selectedShipping != null)
                {
                    order.ShippingMethodName = selectedShipping.Name;
                    order.SelectedShippingMethod = selectedShipping;
                }
            }

            // Apply coupon if exists
            string? appliedCouponCode = HttpContext.Session.GetString("AppliedCouponCode");
            string? appliedCouponId = HttpContext.Session.GetString("AppliedCouponId");

            if (!string.IsNullOrEmpty(appliedCouponCode))
            {
                order.CouponCode = appliedCouponCode;
                order.CouponId = int.TryParse(appliedCouponId, out int couponId) ? couponId : null;
                order.DiscountAmount = discountAmount;
            }

            await _unitOfWork.Order.AddAsync(order);
            await _unitOfWork.SaveAsync();

            // Reserve stock atomically and create order items
            var reserveItems = cartItems.Select(ci => (ci.ProductId, ci.Quantity));
            var reserved = await _inventoryService.ReserveStocksAsync(reserveItems);
            if (!reserved)
            {
                ModelState.AddModelError("", "One or more items are out of stock. Please review your cart.");
                return View(model);
            }

            foreach (var cartItem in cartItems)
            {
                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = cartItem.ProductId,
                    Quantity = cartItem.Quantity,
                    UnitPrice = cartItem.Product.DiscountPrice ?? cartItem.Product.Price,
                    TotalPrice = cartItem.TotalPrice
                };
                await _unitOfWork.OrderItem.AddAsync(orderItem);
            }

            // Create coupon usage record if coupon was applied and user is authenticated
            if (order.CouponId.HasValue && order.DiscountAmount > 0 && !isGuest)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    var couponUsage = new CouponUsage
                    {
                        CouponId = order.CouponId.Value,
                        UserId = user.Id,
                        OrderId = order.Id,
                        DiscountAmount = order.DiscountAmount,
                        UsedAt = DateTime.Now
                    };

                    await _unitOfWork.CouponUsage.AddAsync(couponUsage);

                    // Update coupon usage count
                    var coupon = await _unitOfWork.Coupon.GetByIdAsync(order.CouponId.Value);
                    if (coupon != null)
                    {
                        coupon.UsedCount++;
                        _unitOfWork.Coupon.Update(coupon);
                    }
                }
            }

            // Clear cart
            if (!isGuest)
            {
                _unitOfWork.CartItem.RemoveRange(cartItems);
            }
            else
            {
                // Clear session cart
                HttpContext.Session.Remove("GuestCart");
            }

            // Clear coupon session
            HttpContext.Session.Remove("AppliedCouponCode");
            HttpContext.Session.Remove("AppliedCouponId");
            HttpContext.Session.Remove("DiscountAmount");

            await _unitOfWork.SaveAsync();

            // Check stock levels post-reservation and notify admins on low/out-of-stock
            try
            {
                const int lowStockThreshold = 10; // TODO: make configurable via settings
                var productIds = cartItems.Select(ci => ci.ProductId).Distinct().ToList();
                foreach (var pid in productIds)
                {
                    var product = await _unitOfWork.Product.GetByIdAsync(pid);
                    if (product != null && product.IsActive && product.StockQuantity <= lowStockThreshold)
                    {
                        await _notificationService.CreateLowStockNotificationAsync(product);
                    }
                }
            }
            catch
            {
                // ignore stock notification failures to not affect checkout flow
            }

            // Process payment (simplified)
            await ProcessPayment(order, model);

            // Send confirmation email and notification
            try
            {
                string emailToSend = isGuest ? order.GuestEmail : (await _userManager.GetUserAsync(User))?.Email;
                if (!string.IsNullOrEmpty(emailToSend))
                {
                    await _emailService.SendOrderConfirmationAsync(emailToSend, order);
                }

                if (!isGuest)
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                    {
                        await _notificationService.CreateOrderNotificationAsync(
                            user.Id,
                            order,
                            $"Your order #{order.OrderNumber} has been confirmed and is being processed."
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the order creation
                // _logger.LogError(ex, "Failed to send order confirmation for order {OrderId}", order.Id);
            }

            // Notify admins about the new order (non-blocking)
            try
            {
                await _notificationService.NotifyAdminsNewOrderAsync(order);
            }
            catch
            {
                // ignore admin notification failures to not affect checkout flow
            }

            // Store order id in session for guest confirmation access
            if (isGuest)
            {
                HttpContext.Session.SetString("GuestOrderId", order.Id.ToString());
            }

            TempData["Success"] = "Order placed successfully!";
            return RedirectToAction("Confirmation", new { id = order.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "An error occurred while processing your order. Please try again.");
            return View(model);
        }
    }

    // GET: Checkout/Confirmation
    public async Task<IActionResult> Confirmation(int id)
    {
        Order? order;

        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            order = await _unitOfWork.Order.GetFirstOrDefaultAsync(
                filter: o => o.Id == id && o.UserId == user.Id,
                includeProperties: "OrderItems,OrderItems.Product,OrderItems.Product.Category"
            );
        }
        else
        {
            // Guest confirmation
            string? guestOrderIdStr = HttpContext.Session.GetString("GuestOrderId");
            if (string.IsNullOrEmpty(guestOrderIdStr) || !int.TryParse(guestOrderIdStr, out int guestOrderId) || guestOrderId != id)
            {
                return NotFound();
            }

            order = await _unitOfWork.Order.GetFirstOrDefaultAsync(
                filter: o => o.Id == id && o.UserId == null,
                includeProperties: "OrderItems,OrderItems.Product,OrderItems.Product.Category"
            );
        }

        if (order == null)
        {
            return NotFound();
        }

        return View(order);
    }

    // GET: Checkout/Guest
    public IActionResult Guest()
    {
        // For guest checkout (if implemented)
        return View();
    }

    private string GenerateOrderNumber()
    {
        return $"ORD-{DateTime.Now:yyyyMMdd}-{DateTime.Now.Ticks.ToString().Substring(10)}";
    }

    private async Task ProcessPayment(Order order, CheckoutViewModel model)
    {
        // Simplified payment processing
        // In a real application, you would integrate with payment gateways like Stripe, PayPal, etc.

        switch (model.PaymentMethod)
        {
            case PaymentMethod.CreditCard:
                // Process credit card payment
                order.PaymentStatus = PaymentStatus.Paid;
                order.Status = OrderStatus.Processing;
                break;
            case PaymentMethod.PayPal:
                // Process PayPal payment
                order.PaymentStatus = PaymentStatus.Paid;
                order.Status = OrderStatus.Processing;
                break;
            case PaymentMethod.CashOnDelivery:
                // Cash on delivery
                order.PaymentStatus = PaymentStatus.Pending;
                order.Status = OrderStatus.Processing;
                break;
            default:
                order.PaymentStatus = PaymentStatus.Failed;
                order.Status = OrderStatus.Cancelled;
                break;
        }

        _unitOfWork.Order.Update(order);
        await _unitOfWork.SaveAsync();
    }

    [HttpPost]
    public async Task<IActionResult> ApplyCoupon([FromBody] ApplyCouponRequest request)
    {
        try
        {
            List<CartItem> cartItems;
            decimal cartTotal;
            string? userId = null;

            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }
                userId = user.Id;

                cartItems = (await _unitOfWork.CartItem.GetAllAsync(
                    filter: c => c.UserId == user.Id,
                    includeProperties: "Product"
                )).ToList();
            }
            else
            {
                // Guest cart
                var sessionCart = GetSessionCart();
                cartItems = new List<CartItem>();

                foreach (var item in sessionCart)
                {
                    var product = await _unitOfWork.Product.GetFirstOrDefaultAsync(
                        filter: p => p.Id == item.ProductId
                    );
                    if (product != null)
                    {
                        cartItems.Add(new CartItem
                        {
                            Product = product,
                            Quantity = item.Quantity
                        });
                    }
                }
            }

            if (!cartItems.Any())
            {
                return Json(new { success = false, message = "Cart is empty" });
            }

            // Calculate cart total
            cartTotal = cartItems.Sum(c => c.Product.FinalPrice * c.Quantity);

            // Validate coupon
            var validationResult = await _unitOfWork.Coupon.ValidateCouponAsync(
                request.CouponCode,
                userId,
                cartTotal
            );

            if (!validationResult.IsValid)
            {
                return Json(new { success = false, message = validationResult.Message });
            }

            // Store coupon in session
            HttpContext.Session.SetString("AppliedCouponCode", validationResult.Coupon!.Code);
            HttpContext.Session.SetString("AppliedCouponId", validationResult.Coupon.Id.ToString());
            HttpContext.Session.SetString("DiscountAmount", validationResult.DiscountAmount.ToString());

            return Json(new {
                success = true,
                message = "Coupon applied successfully!",
                discountAmount = validationResult.DiscountAmount,
                couponCode = validationResult.Coupon.Code,
                newTotal = cartTotal - validationResult.DiscountAmount
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error applying coupon" });
        }
    }

    [HttpPost]
    public IActionResult RemoveCoupon()
    {
        try
        {
            HttpContext.Session.Remove("AppliedCouponCode");
            HttpContext.Session.Remove("AppliedCouponId");
            HttpContext.Session.Remove("DiscountAmount");

            return Json(new { success = true, message = "Coupon removed successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error removing coupon" });
        }
    }

    private List<SessionCartItem> GetSessionCart()
    {
        var sessionCartJson = HttpContext.Session.GetString("GuestCart");
        if (string.IsNullOrEmpty(sessionCartJson))
        {
            return new List<SessionCartItem>();
        }
        return JsonConvert.DeserializeObject<List<SessionCartItem>>(sessionCartJson) ?? new List<SessionCartItem>();
    }

    [HttpPost]
    public async Task<IActionResult> CalculateShipping(string city, string postalCode)
    {
        // Calculate shipping based on location
        decimal shippingCost = 5.99m; // Default shipping

        // You can implement more complex shipping calculation here
        if (city?.ToLower() == "cairo" || city?.ToLower() == "alexandria")
        {
            shippingCost = 3.99m;
        }

        return Json(new { success = true, shippingCost });
    }
}

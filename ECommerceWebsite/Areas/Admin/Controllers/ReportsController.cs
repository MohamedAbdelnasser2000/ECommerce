using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerceWebsite.Models;
using ECommerceWebsite.Repository;
using ECommerceWebsite.ViewModels;

namespace ECommerceWebsite.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class ReportsController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public ReportsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Index()
    {
        var model = new ReportsViewModel
        {
            TotalProducts = await _unitOfWork.Product.CountAsync(),
            TotalCategories = await _unitOfWork.Category.CountAsync(),
            TotalOrders = await _unitOfWork.Order.CountAsync(),
            TotalUsers = await _unitOfWork.ApplicationUser.CountAsync(),

            PendingOrders = await _unitOfWork.Order.CountAsync(o => o.Status == OrderStatus.Pending),
            ProcessingOrders = await _unitOfWork.Order.CountAsync(o => o.Status == OrderStatus.Processing),
            ShippedOrders = await _unitOfWork.Order.CountAsync(o => o.Status == OrderStatus.Shipped),
            DeliveredOrders = await _unitOfWork.Order.CountAsync(o => o.Status == OrderStatus.Delivered),
            CancelledOrders = await _unitOfWork.Order.CountAsync(o => o.Status == OrderStatus.Cancelled),

            ActiveProducts = await _unitOfWork.Product.CountAsync(p => p.IsActive),
            InactiveProducts = await _unitOfWork.Product.CountAsync(p => !p.IsActive),
            FeaturedProducts = await _unitOfWork.Product.CountAsync(p => p.IsFeatured),

            LowStockProducts = await _unitOfWork.Product.CountAsync(p => p.StockQuantity <= 5),
            OutOfStockProducts = await _unitOfWork.Product.CountAsync(p => p.StockQuantity == 0)
        };

        // Calculate revenue
        var deliveredOrders = await _unitOfWork.Order.GetAllAsync(filter: o => o.Status == OrderStatus.Delivered);
        model.TotalRevenue = deliveredOrders.Sum(o => o.TotalAmount);

        // Get recent orders for quick view
        var recentOrders = await _unitOfWork.Order.GetAllAsync(
            includeProperties: "User,OrderItems"
        );
        model.RecentOrders = recentOrders
            .OrderByDescending(o => o.OrderDate)
            .Take(5)
            .ToList();

        return View(model);
    }

    // Advanced analytics dashboard (default 30 days)
    public async Task<IActionResult> Advanced(int days = 30)
    {
        var today = DateTime.Today;
        var start30 = today.AddDays(-days);
        var start7 = today.AddDays(-7);

        var orders30 = await _unitOfWork.Order.GetAllAsync(
            filter: o => o.Status == OrderStatus.Delivered && o.OrderDate >= start30,
            includeProperties: "OrderItems,OrderItems.Product,OrderItems.Product.Category"
        );
        var orders7 = orders30.Where(o => o.OrderDate >= start7).ToList();
        var ordersToday = orders30.Where(o => o.OrderDate.Date == today).ToList();

        var model = new AdvancedDashboardViewModel
        {
            RevenueToday = ordersToday.Sum(o => o.TotalAmount),
            Revenue7Days = orders7.Sum(o => o.TotalAmount),
            Revenue30Days = orders30.Sum(o => o.TotalAmount),
            OrdersToday = ordersToday.Count(),
            Orders7Days = orders7.Count(),
            Orders30Days = orders30.Count(),
            AverageOrderValue30Days = orders30.Any() ? orders30.Average(o => o.TotalAmount) : 0
        };

        // Sales trend (last N days)
        var trend = orders30
            .GroupBy(o => o.OrderDate.Date)
            .OrderBy(g => g.Key)
            .Select(g => new { Date = g.Key.ToString("yyyy-MM-dd"), Revenue = g.Sum(o => o.TotalAmount) })
            .ToList();
        model.SalesDates = trend.Select(x => x.Date).ToList();
        model.SalesValues = trend.Select(x => x.Revenue).ToList();

        // Top products by revenue (last N days)
        var orderItems30 = orders30.SelectMany(o => o.OrderItems);
        var topProducts = orderItems30
            .GroupBy(oi => oi.Product.Name)
            .Select(g => new { Name = g.Key, Revenue = g.Sum(x => x.TotalPrice) })
            .OrderByDescending(x => x.Revenue)
            .Take(10)
            .ToList();
        model.TopProductsLabels = topProducts.Select(x => x.Name).ToList();
        model.TopProductsRevenue = topProducts.Select(x => x.Revenue).ToList();

        // Top categories by revenue (last N days)
        var topCategories = orderItems30
            .GroupBy(oi => oi.Product.Category.Name)
            .Select(g => new { Name = g.Key, Revenue = g.Sum(x => x.TotalPrice) })
            .OrderByDescending(x => x.Revenue)
            .Take(10)
            .ToList();
        model.TopCategoriesLabels = topCategories.Select(x => x.Name).ToList();
        model.TopCategoriesRevenue = topCategories.Select(x => x.Revenue).ToList();

        // Payment breakdown (last N days)
        var payments = orders30
            .GroupBy(o => o.PaymentMethod)
            .Select(g => new { Method = g.Key.ToString(), Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();
        model.PaymentLabels = payments.Select(x => x.Method).ToList();
        model.PaymentCounts = payments.Select(x => x.Count).ToList();

        return View(model);
    }

    // Inventory analytics
    public async Task<IActionResult> Inventory(int lowStockThreshold = 5, int days = 30)
    {
        var start = DateTime.Today.AddDays(-days);

        var products = await _unitOfWork.Product.GetAllAsync(includeProperties: "Category,OrderItems");
        var orders = await _unitOfWork.Order.GetAllAsync(
            filter: o => o.Status == OrderStatus.Delivered && o.OrderDate >= start,
            includeProperties: "OrderItems,OrderItems.Product"
        );

        var itemsSoldLookup = orders
            .SelectMany(o => o.OrderItems)
            .GroupBy(oi => oi.ProductId)
            .ToDictionary(g => g.Key, g => new
            {
                Units = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.TotalPrice),
                LastSale = g.Max(x => x.Order.OrderDate)
            });

        var model = new InventoryReportViewModel
        {
            LowStock = products
                .Where(p => p.StockQuantity > 0 && p.StockQuantity <= lowStockThreshold)
                .Select(p => new InventoryItemRow
                {
                    Product = p,
                    Stock = p.StockQuantity,
                    UnitsSoldLast30 = itemsSoldLookup.ContainsKey(p.Id) ? itemsSoldLookup[p.Id].Units : 0,
                    RevenueLast30 = itemsSoldLookup.ContainsKey(p.Id) ? itemsSoldLookup[p.Id].Revenue : 0,
                    DaysSinceLastSale = itemsSoldLookup.ContainsKey(p.Id) ? (int)(DateTime.Today - itemsSoldLookup[p.Id].LastSale.Date).TotalDays : int.MaxValue
                })
                .OrderBy(p => p.Stock)
                .ToList(),

            OutOfStock = products
                .Where(p => p.StockQuantity == 0)
                .Select(p => new InventoryItemRow
                {
                    Product = p,
                    Stock = p.StockQuantity,
                    UnitsSoldLast30 = itemsSoldLookup.ContainsKey(p.Id) ? itemsSoldLookup[p.Id].Units : 0,
                    RevenueLast30 = itemsSoldLookup.ContainsKey(p.Id) ? itemsSoldLookup[p.Id].Revenue : 0,
                    DaysSinceLastSale = itemsSoldLookup.ContainsKey(p.Id) ? (int)(DateTime.Today - itemsSoldLookup[p.Id].LastSale.Date).TotalDays : int.MaxValue
                })
                .OrderByDescending(p => p.DaysSinceLastSale)
                .ToList(),

            SlowMovers = products
                .Select(p => new InventoryItemRow
                {
                    Product = p,
                    Stock = p.StockQuantity,
                    UnitsSoldLast30 = itemsSoldLookup.ContainsKey(p.Id) ? itemsSoldLookup[p.Id].Units : 0,
                    RevenueLast30 = itemsSoldLookup.ContainsKey(p.Id) ? itemsSoldLookup[p.Id].Revenue : 0,
                    DaysSinceLastSale = itemsSoldLookup.ContainsKey(p.Id) ? (int)(DateTime.Today - itemsSoldLookup[p.Id].LastSale.Date).TotalDays : int.MaxValue
                })
                .Where(x => x.UnitsSoldLast30 <= 2) // simple heuristic
                .OrderByDescending(x => x.DaysSinceLastSale)
                .Take(50)
                .ToList()
        };

        return View(model);
    }

    // Customer behavior analytics
    public async Task<IActionResult> CustomerBehavior(int days = 30)
    {
        var start = DateTime.Today.AddDays(-days);

        var users = await _unitOfWork.ApplicationUser.GetAllAsync();
        var orders = await _unitOfWork.Order.GetAllAsync(
            filter: o => o.Status == OrderStatus.Delivered,
            includeProperties: "User"
        );

        // RFM
        var rfm = users.Select(u =>
        {
            var userOrders = orders.Where(o => o.UserId == u.Id).ToList();
            var totalSpent = userOrders.Sum(o => o.TotalAmount);
            var lastOrder = userOrders.OrderByDescending(o => o.OrderDate).FirstOrDefault()?.OrderDate;
            var ordersCount = userOrders.Count;
            var recencyDays = lastOrder.HasValue ? (int)(DateTime.Today - lastOrder.Value.Date).TotalDays : int.MaxValue;
            var segment = totalSpent >= 1000 ? "VIP" : totalSpent >= 100 ? "Regular" : ordersCount > 0 ? "New" : "Inactive";

            return new CustomerRfmItem
            {
                User = u,
                OrdersCount = ordersCount,
                TotalSpent = totalSpent,
                LastOrderDate = lastOrder,
                Segment = segment
            };
        }).OrderByDescending(x => x.TotalSpent).ToList();

        // Cohorts by first order month (ignore orders without a user id)
        var firstOrderByUser = orders
            .Where(o => !string.IsNullOrEmpty(o.UserId))
            .GroupBy(o => o.UserId!)
            .Select(g => new { UserId = g.Key!, First = g.Min(x => x.OrderDate) })
            .ToDictionary(x => x.UserId, x => x.First);

        var cohorts = users
            .Select(u => new
            {
                Cohort = firstOrderByUser.ContainsKey(u.Id) ? firstOrderByUser[u.Id].ToString("yyyy-MM") : "No Orders",
                UserId = u.Id
            })
            .GroupBy(x => x.Cohort)
            .Select(g => new CohortRow
            {
                Cohort = g.Key,
                Customers = g.Count(),
                Orders = orders.Count(o => g.Select(x => x.UserId).Contains(o.UserId)),
                Revenue = orders.Where(o => g.Select(x => x.UserId).Contains(o.UserId)).Sum(o => o.TotalAmount)
            })
            .OrderBy(c => c.Cohort)
            .ToList();

        var model = new CustomerBehaviorViewModel
        {
            Rfm = rfm,
            Cohorts = cohorts
        };

        return View(model);
    }

    public async Task<IActionResult> Sales(DateTime? startDate, DateTime? endDate)
    {
        startDate ??= DateTime.Now.AddMonths(-1);
        endDate ??= DateTime.Now;

        var orders = await _unitOfWork.Order.GetAllAsync(
            filter: o => o.OrderDate >= startDate && o.OrderDate <= endDate && o.Status == OrderStatus.Delivered,
            includeProperties: "User,OrderItems,OrderItems.Product"
        );

        var model = new SalesReportViewModel
        {
            StartDate = startDate.Value,
            EndDate = endDate.Value,
            Orders = orders.OrderByDescending(o => o.OrderDate).ToList(),
            TotalRevenue = orders.Sum(o => o.TotalAmount),
            TotalOrders = orders.Count(),
            AverageOrderValue = orders.Any() ? orders.Average(o => o.TotalAmount) : 0
        };

        return View(model);
    }

    public async Task<IActionResult> Products()
    {
        var products = await _unitOfWork.Product.GetAllAsync(includeProperties: "Category,Reviews,OrderItems");
        var orders = await _unitOfWork.Order.GetAllAsync(
            filter: o => o.Status == OrderStatus.Delivered,
            includeProperties: "OrderItems"
        );

        var model = products.Select(p => new ProductReportItem
        {
            Product = p,
            TotalSold = orders.SelectMany(o => o.OrderItems).Where(oi => oi.ProductId == p.Id).Sum(oi => oi.Quantity),
            Revenue = orders.SelectMany(o => o.OrderItems).Where(oi => oi.ProductId == p.Id).Sum(oi => oi.TotalPrice),
            AverageRating = p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0,
            ReviewCount = p.Reviews.Count
        }).OrderByDescending(p => p.Revenue).ToList();

        return View(model);
    }

    public async Task<IActionResult> Customers()
    {
        var users = await _unitOfWork.ApplicationUser.GetAllAsync();
        var orders = await _unitOfWork.Order.GetAllAsync(includeProperties: "User");

        var model = users.Select(u => new CustomerReportItem
        {
            User = u,
            TotalOrders = orders.Count(o => o.UserId == u.Id),
            TotalSpent = orders.Where(o => o.UserId == u.Id && o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount),
            LastOrderDate = orders.Where(o => o.UserId == u.Id).OrderByDescending(o => o.OrderDate).FirstOrDefault()?.OrderDate
        }).OrderByDescending(c => c.TotalSpent).ToList();

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> GetChartData(string type, int days = 30)
    {
        var startDate = DateTime.Now.AddDays(-days);

        switch (type.ToLower())
        {
            case "sales":
            case "orders":
            case "payments":
            case "topproducts":
            case "topcategories":
                break;
            default:
                return BadRequest("Invalid chart type");
        }

        if (type.ToLower() == "payments")
        {
            var orders = await _unitOfWork.Order.GetAllAsync(
                filter: o => o.OrderDate >= startDate && o.Status == OrderStatus.Delivered
            );
            var data = orders
                .GroupBy(o => o.PaymentMethod)
                .Select(g => new { Label = g.Key.ToString(), Value = g.Count() })
                .OrderByDescending(x => x.Value)
                .ToList();
            return Json(data);
        }
        else if (type.ToLower() == "topproducts")
        {
            var orders = await _unitOfWork.Order.GetAllAsync(
                filter: o => o.OrderDate >= startDate && o.Status == OrderStatus.Delivered,
                includeProperties: "OrderItems,OrderItems.Product"
            );
            var data = orders.SelectMany(o => o.OrderItems)
                .GroupBy(oi => oi.Product.Name)
                .Select(g => new { Label = g.Key, Value = g.Sum(x => x.TotalPrice) })
                .OrderByDescending(x => x.Value)
                .Take(10)
                .ToList();
            return Json(data);
        }
        else if (type.ToLower() == "topcategories")
        {
            var orders = await _unitOfWork.Order.GetAllAsync(
                filter: o => o.OrderDate >= startDate && o.Status == OrderStatus.Delivered,
                includeProperties: "OrderItems,OrderItems.Product,OrderItems.Product.Category"
            );
            var data = orders.SelectMany(o => o.OrderItems)
                .GroupBy(oi => oi.Product.Category.Name)
                .Select(g => new { Label = g.Key, Value = g.Sum(x => x.TotalPrice) })
                .OrderByDescending(x => x.Value)
                .Take(10)
                .ToList();
            return Json(data);
        }
        else if (type.ToLower() == "sales")
        {
            var orders = await _unitOfWork.Order.GetAllAsync(
                filter: o => o.OrderDate >= startDate && o.Status == OrderStatus.Delivered
            );
            var salesData = orders
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new { Date = g.Key.ToString("yyyy-MM-dd"), Revenue = g.Sum(o => o.TotalAmount) })
                .OrderBy(x => x.Date)
                .ToList();
            return Json(salesData);
        }
        else // orders
        {
            var orders = await _unitOfWork.Order.GetAllAsync(
                filter: o => o.OrderDate >= startDate && o.Status == OrderStatus.Delivered
            );
            var ordersData = orders
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new { Date = g.Key.ToString("yyyy-MM-dd"), Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToList();
            return Json(ordersData);
        }
    }

    [HttpPost]
    public async Task<IActionResult> ExportSales(DateTime? startDate, DateTime? endDate, string format = "excel", bool includeSummary = true, bool includeCharts = false, bool includeDetails = true)
    {
        try
        {
            startDate ??= DateTime.Now.AddMonths(-1);
            endDate ??= DateTime.Now;

            var orders = await _unitOfWork.Order.GetAllAsync(
                filter: o => o.OrderDate >= startDate && o.OrderDate <= endDate && o.Status == OrderStatus.Delivered,
                includeProperties: "User,OrderItems,OrderItems.Product"
            );

            var exportData = orders.Select(o => new
            {
                OrderNumber = o.OrderNumber,
                Date = o.OrderDate.ToString("yyyy-MM-dd"),
                Customer = $"{o.User.FirstName} {o.User.LastName}",
                Email = includeDetails ? o.User.Email : "",
                Phone = includeDetails ? o.User.PhoneNumber : "",
                PaymentMethod = o.PaymentMethod,
                ItemsCount = o.OrderItems.Count,
                TotalAmount = o.TotalAmount,
                Status = o.Status.ToString(),
                ShippingAddress = includeDetails ? $"{o.ShippingAddress}, {o.ShippingCity}" : ""
            }).ToList();

            if (format.ToLower() == "csv")
            {
                var csv = GenerateCSV(exportData, "Sales");
                var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
                Response.Headers["Content-Disposition"] = $"attachment; filename=Sales_Report_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.csv";
                return File(bytes, "text/csv");
            }
            else
            {
                return Json(new { success = true, data = exportData, message = "Excel export would be implemented with EPPlus library" });
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Export failed: {ex.Message}" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> ExportProducts(string format = "excel", bool includeImages = false, bool includeReviews = true, bool includeSales = true)
    {
        try
        {
            var products = await _unitOfWork.Product.GetAllAsync(includeProperties: "Category,Reviews,OrderItems");
            var orders = await _unitOfWork.Order.GetAllAsync(
                filter: o => o.Status == OrderStatus.Delivered,
                includeProperties: "OrderItems"
            );

            var exportData = products.Select(p => new
            {
                ProductName = p.Name,
                Category = p.Category.Name,
                Price = p.Price,
                DiscountPrice = p.DiscountPrice,
                Stock = p.StockQuantity,
                IsActive = p.IsActive ? "Active" : "Inactive",
                IsFeatured = p.IsFeatured ? "Yes" : "No",
                TotalSold = includeSales ? orders.SelectMany(o => o.OrderItems).Where(oi => oi.ProductId == p.Id).Sum(oi => oi.Quantity) : 0,
                Revenue = includeSales ? orders.SelectMany(o => o.OrderItems).Where(oi => oi.ProductId == p.Id).Sum(oi => oi.TotalPrice) : 0,
                AverageRating = includeReviews && p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0,
                ReviewCount = includeReviews ? p.Reviews.Count : 0,
                CreatedAt = p.CreatedAt.ToString("yyyy-MM-dd"),
                ImageUrl = includeImages ? p.ImageUrl : ""
            }).ToList();

            if (format.ToLower() == "csv")
            {
                var csv = GenerateCSV(exportData, "Products");
                var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
                Response.Headers["Content-Disposition"] = $"attachment; filename=Products_Report_{DateTime.Now:yyyyMMdd}.csv";
                return File(bytes, "text/csv");
            }
            else
            {
                // For Excel, return JSON for now (in real app, you'd use a library like EPPlus)
                return Json(new { success = true, data = exportData, message = "Excel export would be implemented with EPPlus library" });
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Export failed: {ex.Message}" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> ExportCustomers(string format = "excel", bool includePersonalInfo = true, bool includeOrderHistory = true, bool includeSegmentation = true)
    {
        try
        {
            var users = await _unitOfWork.ApplicationUser.GetAllAsync();
            var orders = await _unitOfWork.Order.GetAllAsync(includeProperties: "User");

            var exportData = users.Select(u => new
            {
                FirstName = includePersonalInfo ? u.FirstName : "",
                LastName = includePersonalInfo ? u.LastName : "",
                Email = includePersonalInfo ? u.Email : "",
                Phone = includePersonalInfo ? u.PhoneNumber : "",
                Address = includePersonalInfo ? u.Address : "",
                City = includePersonalInfo ? u.City : "",
                PostalCode = includePersonalInfo ? u.PostalCode : "",
                JoinedDate = u.CreatedAt.ToString("yyyy-MM-dd"),
                TotalOrders = includeOrderHistory ? orders.Count(o => o.UserId == u.Id) : 0,
                TotalSpent = includeOrderHistory ? orders.Where(o => o.UserId == u.Id && o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount) : 0,
                LastOrderDate = includeOrderHistory ? orders.Where(o => o.UserId == u.Id).OrderByDescending(o => o.OrderDate).FirstOrDefault()?.OrderDate.ToString("yyyy-MM-dd") : "",
                CustomerSegment = includeSegmentation ? GetCustomerSegment(orders.Where(o => o.UserId == u.Id && o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount)) : ""
            }).ToList();

            if (format.ToLower() == "csv")
            {
                var csv = GenerateCSV(exportData, "Customers");
                var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
                Response.Headers["Content-Disposition"] = $"attachment; filename=Customers_Report_{DateTime.Now:yyyyMMdd}.csv";
                return File(bytes, "text/csv");
            }
            else
            {
                return Json(new { success = true, data = exportData, message = "Excel export would be implemented with EPPlus library" });
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Export failed: {ex.Message}" });
        }
    }

    private string GetCustomerSegment(decimal totalSpent)
    {
        if (totalSpent >= 1000) return "VIP";
        if (totalSpent >= 100) return "Regular";
        if (totalSpent > 0) return "New";
        return "Inactive";
    }

    private string GenerateCSV<T>(IEnumerable<T> data, string reportType)
    {
        var csv = new System.Text.StringBuilder();

        if (!data.Any()) return csv.ToString();

        // Get properties
        var properties = typeof(T).GetProperties();

        // Add header
        csv.AppendLine(string.Join(",", properties.Select(p => $"\"{p.Name}\"")));

        // Add data rows
        foreach (var item in data)
        {
            var values = properties.Select(p =>
            {
                var value = p.GetValue(item)?.ToString() ?? "";
                return $"\"{value.Replace("\"", "\"\"")}\""; // Escape quotes
            });
            csv.AppendLine(string.Join(",", values));
        }

        return csv.ToString();
    }
}

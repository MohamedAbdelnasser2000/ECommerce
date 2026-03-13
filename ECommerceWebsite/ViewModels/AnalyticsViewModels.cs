using System.ComponentModel.DataAnnotations.Schema;
using ECommerceWebsite.Models;

namespace ECommerceWebsite.ViewModels
{
    // Advanced analytics dashboard view model
    public class AdvancedDashboardViewModel
    {
        // KPI cards
        public decimal RevenueToday { get; set; }
        public decimal Revenue7Days { get; set; }
        public decimal Revenue30Days { get; set; }
        public int OrdersToday { get; set; }
        public int Orders7Days { get; set; }
        public int Orders30Days { get; set; }
        public decimal AverageOrderValue30Days { get; set; }

        // Charts data (labels + values for Chart.js)
        public List<string> SalesDates { get; set; } = new();
        public List<decimal> SalesValues { get; set; } = new();

        public List<string> TopProductsLabels { get; set; } = new();
        public List<decimal> TopProductsRevenue { get; set; } = new();

        public List<string> TopCategoriesLabels { get; set; } = new();
        public List<decimal> TopCategoriesRevenue { get; set; } = new();

        public List<string> PaymentLabels { get; set; } = new();
        public List<int> PaymentCounts { get; set; } = new();
    }

    // Customer behavior analytics (RFM + Cohorts)
    public class CustomerBehaviorViewModel
    {
        public List<CustomerRfmItem> Rfm { get; set; } = new();
        public List<CohortRow> Cohorts { get; set; } = new();
    }

    public class CustomerRfmItem
    {
        public ApplicationUser User { get; set; } = null!;
        public int OrdersCount { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime? LastOrderDate { get; set; }
        public int RecencyDays => LastOrderDate.HasValue ? (int)(DateTime.UtcNow.Date - LastOrderDate.Value.Date).TotalDays : int.MaxValue;
        public string Segment { get; set; } = string.Empty; // VIP / Regular / New / Inactive
    }

    public class CohortRow
    {
        public string Cohort { get; set; } = string.Empty; // e.g., 2025-01
        public int Customers { get; set; }
        public int Orders { get; set; }
        public decimal Revenue { get; set; }
    }

    // Inventory report view model
    public class InventoryReportViewModel
    {
        public List<InventoryItemRow> LowStock { get; set; } = new();
        public List<InventoryItemRow> OutOfStock { get; set; } = new();
        public List<InventoryItemRow> SlowMovers { get; set; } = new();
    }

    public class InventoryItemRow
    {
        public Product Product { get; set; } = null!;
        public int Stock { get; set; }
        public int UnitsSoldLast30 { get; set; }
        public decimal RevenueLast30 { get; set; }
        public int DaysSinceLastSale { get; set; }
    }
}
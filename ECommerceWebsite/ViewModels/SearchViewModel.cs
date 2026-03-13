using System.ComponentModel.DataAnnotations;

namespace ECommerceWebsite.ViewModels
{
    public class SearchViewModel
    {
        [Display(Name = "كلمة البحث")]
        public string Query { get; set; } = string.Empty;

        [Display(Name = "الفئة")]
        public int? CategoryId { get; set; }

        [Display(Name = "السعر من")]
        [Range(0, double.MaxValue)]
        public decimal? MinPrice { get; set; }

        [Display(Name = "السعر إلى")]
        [Range(0, double.MaxValue)]
        public decimal? MaxPrice { get; set; }

        [Display(Name = "التقييم من")]
        [Range(1, 5)]
        public int? MinRating { get; set; }

        [Display(Name = "متوفر في المخزون فقط")]
        public bool InStockOnly { get; set; }

        [Display(Name = "المنتجات المميزة فقط")]
        public bool FeaturedOnly { get; set; }

        [Display(Name = "ترتيب حسب")]
        public SortBy SortBy { get; set; } = SortBy.Relevance;

        [Display(Name = "اتجاه الترتيب")]
        public SortDirection SortDirection { get; set; } = SortDirection.Descending;

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 12;

        // خصائص للعرض في الـ View
        public int TotalResults { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalResults / PageSize);
        public bool HasNextPage => PageNumber < TotalPages;
        public bool HasPreviousPage => PageNumber > 1;
    }

    public class SearchResultViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DescriptionAr { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryNameAr { get; set; } = string.Empty;
        public bool IsFeatured { get; set; }
        public DateTime CreatedAt { get; set; }

        // خصائص محسوبة للعرض
        public bool HasDiscount => DiscountPrice.HasValue && DiscountPrice < Price;
        public decimal DiscountPercentage => HasDiscount ? (decimal)Math.Round(((Price - DiscountPrice.Value) / Price) * 100, 0) : 0;
        public bool IsInStock => StockQuantity > 0;
    }

    public enum SortBy
    {
        [Display(Name = "الأهمية")]
        Relevance,
        [Display(Name = "السعر")]
        Price,
        [Display(Name = "التقييم")]
        Rating,
        [Display(Name = "الأحدث")]
        Newest,
        [Display(Name = "الأقدم")]
        Oldest,
        [Display(Name = "الأكثر مبيعاً")]
        MostSold,
        [Display(Name = "الاسم")]
        Name
    }

    public enum SortDirection
    {
        [Display(Name = "تصاعدي")]
        Ascending,
        [Display(Name = "تنازلي")]
        Descending
    }

    public class CategoryFilterViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public int ProductCount { get; set; }
    }
}

using System.Globalization;
using ECommerceWebsite.Models;

namespace ECommerceWebsite.Extensions
{
    public static class LocalizationExtensions
    {
        private static bool IsArabic()
        {
            return string.Equals(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, "ar", StringComparison.OrdinalIgnoreCase);
        }

        private static string Localized(string? en, string? ar)
        {
            if (IsArabic())
            {
                return string.IsNullOrWhiteSpace(ar) ? (en ?? string.Empty) : ar!;
            }
            return en ?? string.Empty;
        }

        // Product helpers
        public static string GetLocalizedName(this Product p) => Localized(p.Name, p.NameAr);
        public static string GetLocalizedShortDescription(this Product p) => Localized(p.ShortDescription, p.ShortDescriptionAr);
        public static string GetLocalizedDescription(this Product p) => Localized(p.Description, p.DescriptionAr);

        // Category helpers
        public static string GetLocalizedName(this Category c) => Localized(c.Name, c.NameAr);
        public static string GetLocalizedDescription(this Category c) => Localized(c.Description, c.DescriptionAr);
    }
}

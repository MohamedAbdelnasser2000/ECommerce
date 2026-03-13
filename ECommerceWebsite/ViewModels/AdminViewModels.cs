using System.ComponentModel.DataAnnotations;
using ECommerceWebsite.Models;

namespace ECommerceWebsite.ViewModels;

public class ShippingMethodViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "اسم طريقة الشحن مطلوب")]
    [StringLength(100, ErrorMessage = "يجب أن يكون الاسم أقل من 100 حرف")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "يجب أن يكون الوصف أقل من 500 حرف")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "تكلفة الشحن مطلوبة")]
    [Range(0, double.MaxValue, ErrorMessage = "يجب أن تكون التكلفة أكبر من أو تساوي صفر")]
    public decimal Cost { get; set; }

    [Required(ErrorMessage = "عدد أيام التوصيل مطلوب")]
    [Range(1, 30, ErrorMessage = "يجب أن يكون عدد الأيام بين 1 و 30 يوم")]
    public int EstimatedDays { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;
}

public class TaxSettingViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "اسم الدولة مطلوب")]
    [StringLength(100, ErrorMessage = "يجب أن يكون اسم الدولة أقل من 100 حرف")]
    public string Country { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "يجب أن يكون اسم المنطقة أقل من 100 حرف")]
    public string Region { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "يجب أن يكون اسم المدينة أقل من 100 حرف")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "معدل الضريبة مطلوب")]
    [Range(0, 100, ErrorMessage = "يجب أن يكون معدل الضريبة بين 0 و 100")]
    public decimal TaxRate { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    [StringLength(500, ErrorMessage = "يجب أن تكون الملاحظات أقل من 500 حرف")]
    public string Notes { get; set; } = string.Empty;
}

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ECommerceWebsite.Models;
using ECommerceWebsite.Data;

namespace ECommerceWebsite.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Create roles
        await CreateRolesAsync(roleManager);

        // Create admin user
        await CreateAdminUserAsync(userManager);

        // Create test customer user
        await CreateCustomerUserAsync(userManager);

        // Seed basic settings
        await SeedSettingsAsync(serviceProvider);
        await SeedShippingMethodsAsync(serviceProvider);
        await SeedTaxSettingsAsync(serviceProvider);

        // Seed Sample Data (Categories & Products)
        await CreateSampleDataAsync(serviceProvider);

        // Seed Coupons
        await SeedCouponsAsync(serviceProvider);

        // Create sample reviews
        await CreateSampleReviewsAsync(serviceProvider);

        // Create sample notifications for customers
        await CreateSampleCustomerNotificationsAsync(serviceProvider);

        // Seed Orders (Last because it depends on users, products, shipping, and tax)
        await SeedOrdersAsync(serviceProvider);
    }

    private static async Task RemoveExistingProductsAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

        var products = await context.Products
            .Include(p => p.ProductImages)
            .Include(p => p.CartItems)
            .Include(p => p.WishlistItems)
            .Include(p => p.Reviews)
            .Include(p => p.OrderItems)
            .ToListAsync();

        if (!products.Any())
        {
            return;
        }

        // Remove related entities first to avoid foreign key constraints
        var productImages = products.SelectMany(p => p.ProductImages).ToList();
        var cartItems = products.SelectMany(p => p.CartItems).ToList();
        var wishlistItems = products.SelectMany(p => p.WishlistItems).ToList();
        var reviews = products.SelectMany(p => p.Reviews).ToList();
        var orderItems = products.SelectMany(p => p.OrderItems).ToList();

        if (productImages.Any())
        {
            context.ProductImages.RemoveRange(productImages);
        }

        if (cartItems.Any())
        {
            context.CartItems.RemoveRange(cartItems);
        }

        if (wishlistItems.Any())
        {
            context.WishlistItems.RemoveRange(wishlistItems);
        }

        if (reviews.Any())
        {
            context.Reviews.RemoveRange(reviews);
        }

        if (orderItems.Any())
        {
            context.OrderItems.RemoveRange(orderItems);
        }

        context.Products.RemoveRange(products);
        await context.SaveChangesAsync();
    }

    private static async Task CreateRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        string[] roleNames = { "Admin", "Customer" };

        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }

    private static async Task CreateAdminUserAsync(UserManager<ApplicationUser> userManager)
    {
        var adminEmail = "admin@ecommerce.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Admin",
                LastName = "User",
                PhoneNumber = "1234567890",
                Address = "Admin Address",
                City = "Admin City",
                PostalCode = "12345",
                EmailConfirmed = true,
                CreatedAt = DateTime.Now
            };

            var result = await userManager.CreateAsync(adminUser, "Admin123!");

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
        else
        {
            // Ensure admin user has admin role
            if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
    }

    private static async Task CreateCustomerUserAsync(UserManager<ApplicationUser> userManager)
    {
        var customerEmail = "customer@ecommerce.com";
        var existingCustomer = await userManager.FindByEmailAsync(customerEmail);

        if (existingCustomer == null)
        {
            var customerUser = new ApplicationUser
            {
                UserName = customerEmail,
                Email = customerEmail,
                FirstName = "John",
                LastName = "Customer",
                EmailConfirmed = true,
                PhoneNumber = "+1-234-567-8901",
                Address = "456 Customer Street",
                City = "Customer City",
                PostalCode = "12346",
                CreatedAt = DateTime.Now
            };

            var result = await userManager.CreateAsync(customerUser, "Customer123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(customerUser, "Customer");
            }
        }
        else
        {
            // Ensure customer user has customer role
            if (!await userManager.IsInRoleAsync(existingCustomer, "Customer"))
            {
                await userManager.AddToRoleAsync(existingCustomer, "Customer");
            }
        }
    }

    private static async Task CreateSampleDataAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

        // Create categories if they don't exist
        if (!await context.Categories.AnyAsync())
        {
            var categories = new[]
            {
                new Category { Name = "Electronics", Description = "Electronic devices and technology", NameAr = "الإلكترونيات", DescriptionAr = "الأجهزة الإلكترونية والتقنية", IsActive = true },
                new Category { Name = "Fashion", Description = "Fashion and accessories", NameAr = "الأزياء", DescriptionAr = "الأزياء والإكسسوارات", IsActive = true },
                new Category { Name = "Books", Description = "Books and references", NameAr = "الكتب", DescriptionAr = "الكتب والمراجع", IsActive = true },
                new Category { Name = "Home & Garden", Description = "Home and garden essentials", NameAr = "المنزل والحديقة", DescriptionAr = "المنزل والحديقة", IsActive = true },
                new Category { Name = "Sports", Description = "Sports and fitness", NameAr = "الرياضة", DescriptionAr = "الرياضة واللياقة البدنية", IsActive = true }
            };

            await context.Categories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
        }

        // Ensure required categories exist, then fetch them
        var requiredCats = new[]
        {
            new Category { Name = "Electronics", Description = "Electronic devices and technology", NameAr = "الإلكترونيات", DescriptionAr = "الأجهزة الإلكترونية والتقنية", IsActive = true },
            new Category { Name = "Fashion", Description = "Fashion and accessories", NameAr = "الأزياء", DescriptionAr = "الأزياء والإكسسوارات", IsActive = true },
            new Category { Name = "Books", Description = "Books and references", NameAr = "الكتب", DescriptionAr = "الكتب والمراجع", IsActive = true }
        };

        foreach (var rc in requiredCats)
        {
            var existing = await context.Categories.FirstOrDefaultAsync(c => c.Name == rc.Name);
            if (existing == null)
            {
                await context.Categories.AddAsync(rc);
                await context.SaveChangesAsync();
            }
            else
            {
                // Ensure Arabic fields are populated for existing categories
                if (string.IsNullOrWhiteSpace(existing.NameAr) && !string.IsNullOrWhiteSpace(rc.NameAr))
                {
                    existing.NameAr = rc.NameAr;
                }

                if (string.IsNullOrWhiteSpace(existing.DescriptionAr) && !string.IsNullOrWhiteSpace(rc.DescriptionAr))
                {
                    existing.DescriptionAr = rc.DescriptionAr;
                }

                if (context.Entry(existing).State == EntityState.Modified)
                {
                    await context.SaveChangesAsync();
                }
            }
        }

        var fashionCategory = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Fashion");
        var electronicsCategory = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Electronics");
        var booksCategory = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Books");

        if (fashionCategory == null || electronicsCategory == null || booksCategory == null)
        {
            throw new InvalidOperationException("Required categories missing for seeding sample products.");
        }

        // Create sample products for testing
        Product[] products =
        [
            new Product
            {
                Name = "Advanced Smartphone",
                Description = "High-spec smartphone with modern technologies and advanced camera",
                NameAr = "هاتف ذكي متطور",
                DescriptionAr = "هاتف ذكي بمواصفات عالية وتقنيات حديثة مع كاميرا متطورة وأداء سريع",
                Price = 599.00m,
                StockQuantity = 50,
                ImageUrl = "/images/products/phone1.jpg",
                IsActive = true,
                CategoryId = electronicsCategory.Id,
                CreatedAt = DateTime.Now
            },
            new Product
            {
                Name = "Wireless Headphones",
                Description = "High-quality headphones with noise cancellation and long battery life",
                NameAr = "سماعات لاسلكية",
                DescriptionAr = "سماعات عالية الجودة مع تقنية إلغاء الضوضاء وبطارية طويلة المدى",
                Price = 249.00m,
                DiscountPrice = 199.00m,
                StockQuantity = 30,
                ImageUrl = "/images/products/headphones1.jpg",
                IsActive = true,
                CategoryId = electronicsCategory.Id,
                CreatedAt = DateTime.Now
            },
            new Product
            {
                Name = "Elegant Handbag",
                Description = "Stylish leather handbag with elegant and durable design",
                NameAr = "حقيبة يد أنيقة",
                DescriptionAr = "حقيبة يد عصرية مصنوعة من الجلد الطبيعي بتصميم أنيق ومتين",
                Price = 89.00m,
                StockQuantity = 0, // Not available
                ImageUrl = "/images/products/bag1.jpg",
                IsActive = true,
                CategoryId = fashionCategory.Id,
                CreatedAt = DateTime.Now
            },
            new Product
            {
                Name = "Smart Sports Watch",
                Description = "Smart watch for fitness and health tracking with water resistance",
                NameAr = "ساعة رياضية ذكية",
                DescriptionAr = "ساعة ذكية لتتبع اللياقة البدنية والصحة مع مقاومة للماء",
                Price = 399.00m,
                DiscountPrice = 299.00m,
                StockQuantity = 25,
                ImageUrl = "/images/products/watch1.jpg",
                IsActive = true,
                CategoryId = electronicsCategory.Id,
                CreatedAt = DateTime.Now
            },
            new Product
            {
                Name = "Self-Development Book",
                Description = "Inspiring book for personal and professional growth with practical tips",
                NameAr = "كتاب تطوير الذات",
                DescriptionAr = "كتاب ملهم لتطوير المهارات الشخصية والمهنية مع نصائح عملية",
                Price = 25.00m,
                StockQuantity = 100,
                ImageUrl = "/images/products/book1.jpg",
                IsActive = true,
                CategoryId = booksCategory.Id,
                CreatedAt = DateTime.Now
            },
            new Product
            {
                Name = "Digital Drawing Tablet",
                Description = "Advanced tablet for digital drawing and design with pressure-sensitive stylus",
                NameAr = "جهاز لوحي للرسم",
                DescriptionAr = "جهاز لوحي متطور للرسم الرقمي والتصميم مع قلم حساس للضغط",
                Price = 549.00m,
                DiscountPrice = 449.00m,
                StockQuantity = 15,
                ImageUrl = "/images/products/tablet1.jpg",
                IsActive = true,
                CategoryId = electronicsCategory.Id,
                CreatedAt = DateTime.Now
            },
            new Product
            {
                Name = "Leather Jacket",
                Description = "Premium leather jacket for a classic look",
                NameAr = "جاكيت جلد",
                DescriptionAr = "جاكيت جلد طبيعي لمظهر كلاسيكي",
                Price = 199.99m,
                StockQuantity = 20,
                ImageUrl = "/images/products/jacket1.jpg",
                IsActive = true,
                CategoryId = fashionCategory.Id,
                CreatedAt = DateTime.Now
            },
            new Product
            {
                Name = "Running Shoes",
                Description = "Lightweight running shoes for maximum comfort",
                NameAr = "حذاء جري",
                DescriptionAr = "حذاء جري خفيف الوزن لأقصى درجات الراحة",
                Price = 120.00m,
                DiscountPrice = 99.00m,
                StockQuantity = 40,
                ImageUrl = "/images/products/shoes1.jpg",
                IsActive = true,
                CategoryId = fashionCategory.Id,
                CreatedAt = DateTime.Now
            }
        ];

        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();

        // Update existing products with images if they don't have them
        var productsWithoutImages = await context.Products.Where(p => string.IsNullOrEmpty(p.ImageUrl)).ToListAsync();
        foreach (var product in productsWithoutImages)
        {
            if (product.Name.Contains("هاتف") || product.Name.ToLower().Contains("phone"))
            {
                product.ImageUrl = "/images/products/phone1.jpg";
            }
            else if (product.Name.Contains("سماعات") || product.Name.ToLower().Contains("headphone"))
            {
                product.ImageUrl = "/images/products/headphones1.jpg";
            }
            else if (product.Name.Contains("حقيبة") || product.Name.ToLower().Contains("bag"))
            {
                product.ImageUrl = "/images/products/bag1.jpg";
            }
            else if (product.Name.Contains("ساعة") || product.Name.ToLower().Contains("watch"))
            {
                product.ImageUrl = "/images/products/watch1.jpg";
            }
            else if (product.Name.Contains("كتاب") || product.Name.ToLower().Contains("book"))
            {
                product.ImageUrl = "/images/products/book1.jpg";
            }
            else if (product.Name.Contains("جهاز") || product.Name.ToLower().Contains("tablet"))
            {
                product.ImageUrl = "/images/products/tablet1.jpg";
            }
            else
            {
                product.ImageUrl = "/images/products/phone1.jpg"; // صورة افتراضية
            }
        }

        if (productsWithoutImages.Any())
        {
            await context.SaveChangesAsync();
        }
    }

    private static async Task RemoveSampleProductsAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

        var sampleNames = new[]
        {
            "iPhone 15 Pro",
            "Samsung Galaxy S24",
            "Nike Air Max",
            "Programming Book",
            "Cotton T-Shirt",
            "هاتف ذكي متطور",
            "سماعات لاسلكية",
            "حقيبة يد أنيقة",
            "ساعة رياضية ذكية",
            "كتاب تطوير الذات",
            "جهاز لوحي للرسم"
        };

        var sampleImages = new[]
        {
            "/images/products/iphone15pro.jpg",
            "/images/products/galaxys24.jpg",
            "/images/products/nikeairmax.jpg",
            "/images/products/csharpbook.jpg",
            "/images/products/cottontshirt.jpg",
            "/images/products/phone1.jpg",
            "/images/products/headphones1.jpg",
            "/images/products/bag1.jpg",
            "/images/products/watch1.jpg",
            "/images/products/book1.jpg",
            "/images/products/tablet1.jpg"
        };

        var baseSeedDate = new DateTime(2024, 1, 1);

        var toRemove = await context.Products
            .Where(p => sampleNames.Contains(p.Name) ||
                        (p.ImageUrl != null && sampleImages.Contains(p.ImageUrl)) ||
                        p.CreatedAt == baseSeedDate)
            .ToListAsync();

        if (toRemove.Any())
        {
            context.Products.RemoveRange(toRemove);
            await context.SaveChangesAsync();
        }
    }

    private static async Task CreateSampleReviewsAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Check if reviews already exist
        if (await context.Reviews.AnyAsync())
        {
            return; // Reviews already seeded
        }

        // Get customer user
        var customerUser = await userManager.FindByEmailAsync("customer@ecommerce.com");
        if (customerUser == null) return;

        // Create additional test users for reviews
        var testUser1 = await CreateTestUserAsync(userManager, "testuser1@ecommerce.com", "Test", "User1");
        var testUser2 = await CreateTestUserAsync(userManager, "testuser2@ecommerce.com", "Test", "User2");

        // Create sample reviews
        var reviews = new List<Review>
        {
            new Review
            {
                ProductId = 1, // iPhone 15 Pro
                UserId = customerUser.Id,
                Rating = 5,
                Comment = "Excellent phone! The camera quality is amazing and the performance is top-notch. Highly recommended!",
                CreatedAt = DateTime.Now.AddDays(-10),
                IsApproved = true,
                ApprovedAt = DateTime.Now.AddDays(-9)
            },
            new Review
            {
                ProductId = 1, // iPhone 15 Pro
                UserId = testUser1.Id,
                Rating = 4,
                Comment = "Great phone overall, but the battery life could be better. Still worth the price.",
                CreatedAt = DateTime.Now.AddDays(-5),
                IsApproved = true,
                ApprovedAt = DateTime.Now.AddDays(-4)
            },
            new Review
            {
                ProductId = 2, // Samsung Galaxy S24
                UserId = testUser2.Id,
                Rating = 5,
                Comment = "Love this phone! The display is gorgeous and the Android experience is smooth.",
                CreatedAt = DateTime.Now.AddDays(-7),
                IsApproved = true,
                ApprovedAt = DateTime.Now.AddDays(-6)
            },
            new Review
            {
                ProductId = 3, // Nike Air Max
                UserId = customerUser.Id,
                Rating = 4,
                Comment = "Very comfortable shoes for running. Good quality and stylish design.",
                CreatedAt = DateTime.Now.AddDays(-3),
                IsApproved = true,
                ApprovedAt = DateTime.Now.AddDays(-2)
            },
            new Review
            {
                ProductId = 1, // iPhone 15 Pro
                UserId = testUser2.Id,
                Rating = 3,
                Comment = "Good phone but overpriced. There are better alternatives in the market.",
                CreatedAt = DateTime.Now.AddDays(-2),
                IsApproved = true,
                ApprovedAt = DateTime.Now.AddDays(-1)
            }
        };

        await context.Reviews.AddRangeAsync(reviews);
        await context.SaveChangesAsync();
    }

    private static async Task<ApplicationUser> CreateTestUserAsync(UserManager<ApplicationUser> userManager, string email, string firstName, string lastName)
    {
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            return existingUser;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            EmailConfirmed = true,
            PhoneNumber = "+1-555-0123",
            Address = "123 Test Street",
            City = "Test City",
            PostalCode = "12345",
            CreatedAt = DateTime.Now
        };

        var result = await userManager.CreateAsync(user, "TestUser123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, "Customer");
        }

        return user;
    }

    private static async Task CreateSampleCustomerNotificationsAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Get customer user
        var customerUser = await userManager.FindByEmailAsync("customer@ecommerce.com");
        if (customerUser == null) return;

        // Check if customer already has any notifications (ignore admin/system entries)
        if (await context.Notifications.AnyAsync(n => n.UserId == customerUser.Id))
        {
            return; // Customer notifications already exist
        }

        // Get additional test users
        var testUser1 = await userManager.FindByEmailAsync("testuser1@ecommerce.com");
        var testUser2 = await userManager.FindByEmailAsync("testuser2@ecommerce.com");

        // Create sample notifications for customers
        var customerNotifications = new List<Notification>
        {
            new Notification
            {
                UserId = customerUser.Id,
                Type = NotificationType.Welcome,
                Title = "Welcome to our store!",
                Message = "Thank you for joining our community. Start exploring our amazing products!",
                Icon = "fas fa-heart",
                ActionUrl = "/Product",
                CreatedAt = DateTime.Now.AddDays(-7),
                IsRead = true
            },
            new Notification
            {
                UserId = customerUser.Id,
                Type = NotificationType.OrderUpdate,
                Title = "Order Confirmed",
                Message = "Your order #ORD-2025-001 has been confirmed and is being processed.",
                Icon = "fas fa-shopping-cart",
                ActionUrl = "/Customer/Orders",
                CreatedAt = DateTime.Now.AddDays(-3),
                IsRead = false
            },
            new Notification
            {
                UserId = customerUser.Id,
                Type = NotificationType.OrderUpdate,
                Title = "Order Shipped",
                Message = "Great news! Your order #ORD-2025-001 has been shipped and is on its way.",
                Icon = "fas fa-truck",
                ActionUrl = "/Customer/Orders",
                CreatedAt = DateTime.Now.AddDays(-1),
                IsRead = false
            },
            new Notification
            {
                UserId = customerUser.Id,
                Type = NotificationType.Success,
                Title = "Review Approved",
                Message = "Your review for 'هاتف ذكي متطور' has been approved and is now live!",
                Icon = "fas fa-star",
                ActionUrl = "/Product/Details/1",
                CreatedAt = DateTime.Now.AddHours(-12),
                IsRead = false
            },
            new Notification
            {
                UserId = customerUser.Id,
                Type = NotificationType.Info,
                Title = "Special Offer",
                Message = "Don't miss our special discount on electronics! Up to 30% off on selected items.",
                Icon = "fas fa-tags",
                ActionUrl = "/Product/Category/1",
                CreatedAt = DateTime.Now.AddHours(-6),
                IsRead = false
            }
        };

        // Add notifications for test users if they exist
        if (testUser1 != null)
        {
            customerNotifications.AddRange(new List<Notification>
            {
                new Notification
                {
                    UserId = testUser1.Id,
                    Type = NotificationType.Welcome,
                    Title = "Welcome to our store!",
                    Message = "Thank you for joining our community. Start exploring our amazing products!",
                    Icon = "fas fa-heart",
                    ActionUrl = "/Product",
                    CreatedAt = DateTime.Now.AddDays(-5),
                    IsRead = true
                },
                new Notification
                {
                    UserId = testUser1.Id,
                    Type = NotificationType.OrderUpdate,
                    Title = "Order Delivered",
                    Message = "Your order #ORD-2025-002 has been successfully delivered. Enjoy your purchase!",
                    Icon = "fas fa-box-open",
                    ActionUrl = "/Customer/Orders",
                    CreatedAt = DateTime.Now.AddHours(-2),
                    IsRead = false
                }
            });
        }

        if (testUser2 != null)
        {
            customerNotifications.AddRange(new List<Notification>
            {
                new Notification
                {
                    UserId = testUser2.Id,
                    Type = NotificationType.Welcome,
                    Title = "Welcome to our store!",
                    Message = "Thank you for joining our community. Start exploring our amazing products!",
                    Icon = "fas fa-heart",
                    ActionUrl = "/Product",
                    CreatedAt = DateTime.Now.AddDays(-4),
                    IsRead = true
                },
                new Notification
                {
                    UserId = testUser2.Id,
                    Type = NotificationType.Newsletter,
                    Title = "Newsletter Subscription",
                    Message = "You've successfully subscribed to our newsletter. Stay updated with latest offers!",
                    Icon = "fas fa-envelope",
                    ActionUrl = "/Home",
                    CreatedAt = DateTime.Now.AddHours(-8),
                    IsRead = false
                }
            });
        }

        foreach (var notification in customerNotifications)
        {
            await context.Notifications.AddAsync(notification);
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedShippingMethodsAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        if (await context.ShippingMethods.AnyAsync()) return;

        var methods = new List<ShippingMethod>
        {
            new ShippingMethod { Name = "Standard Shipping", Description = "Delivery within 5-7 business days", Cost = 10.00m, EstimatedDays = 7, IsActive = true },
            new ShippingMethod { Name = "Express Shipping", Description = "Delivery within 1-2 business days", Cost = 25.00m, EstimatedDays = 2, IsActive = true },
            new ShippingMethod { Name = "Free Shipping", Description = "Available for orders over $100", Cost = 0.00m, EstimatedDays = 10, IsActive = true }
        };

        await context.ShippingMethods.AddRangeAsync(methods);
        await context.SaveChangesAsync();
    }

    private static async Task SeedTaxSettingsAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        // TaxSetting model is in ErrorViewModel.cs but let's check if it exists in DB
        // Based on ApplicationDbContext, it's not there! Wait, let me check ApplicationDbContext again.
        // Actually, ShippingMethods is there, but TaxSetting is NOT a DbSet in ApplicationDbContext.
        // Wait, I saw it in ErrorViewModel.cs but it might not be in the DbContext.
    }

    private static async Task SeedSettingsAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        if (await context.Settings.AnyAsync()) return;

        var settings = new List<Setting>
        {
            new Setting { Key = "SiteName", Value = "My E-Commerce Store", Description = "The name of the website" },
            new Setting { Key = "ContactEmail", Value = "contact@mystore.com", Description = "Primary contact email" },
            new Setting { Key = "Currency", Value = "USD", Description = "Default store currency" }
        };

        await context.Settings.AddRangeAsync(settings);
        await context.SaveChangesAsync();
    }

    private static async Task SeedCouponsAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        if (await context.Coupons.AnyAsync()) return;

        var coupons = new List<Coupon>
        {
            new Coupon { Code = "DISCOUNT10", Name = "10% Off", Value = 10, Type = CouponType.Percentage, IsActive = true, StartDate = DateTime.Now, EndDate = DateTime.Now.AddMonths(1) },
            new Coupon { Code = "WELCOME20", Name = "Welcome $20", Value = 20, Type = CouponType.FixedAmount, IsActive = true, StartDate = DateTime.Now, EndDate = DateTime.Now.AddMonths(1) }
        };

        await context.Coupons.AddRangeAsync(coupons);
        await context.SaveChangesAsync();
    }

    private static async Task SeedOrdersAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        if (await context.Orders.AnyAsync()) return;

        var user = await userManager.FindByEmailAsync("customer@ecommerce.com");
        if (user == null) return;

        var product = await context.Products.FirstOrDefaultAsync();
        if (product == null) return;

        var order = new Order
        {
            OrderNumber = "ORD-" + DateTime.Now.Ticks.ToString().Substring(10),
            OrderDate = DateTime.Now.AddDays(-2),
            UserId = user.Id,
            ShippingFirstName = user.FirstName,
            ShippingLastName = user.LastName,
            ShippingAddress = user.Address ?? "123 Test St",
            ShippingCity = user.City ?? "Test City",
            ShippingPostalCode = user.PostalCode ?? "12345",
            ShippingPhone = user.PhoneNumber ?? "1234567890",
            Status = OrderStatus.Delivered,
            PaymentMethod = PaymentMethod.CreditCard,
            PaymentStatus = PaymentStatus.Paid,
            Subtotal = product.Price,
            TotalAmount = product.Price + 10, // Plus shipping
            ShippingCost = 10
        };

        order.OrderItems.Add(new OrderItem
        {
            ProductId = product.Id,
            Quantity = 1,
            UnitPrice = product.Price,
            TotalPrice = product.Price
        });

        await context.Orders.AddAsync(order);
        await context.SaveChangesAsync();
    }
}

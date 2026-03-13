-- بيانات تجريبية شاملة للتجارة الإلكترونية
-- تشغيل هذا الملف بعد إنشاء قاعدة البيانات

USE ECommerceDB;
GO

-- إدراج فئات تجريبية
IF NOT EXISTS (SELECT * FROM Categories WHERE Name = 'Electronics')
BEGIN
    INSERT INTO Categories (Name, NameAr, Description, DescriptionAr, ImageUrl, IsActive, CreatedAt) VALUES
    ('Electronics', 'الإلكترونيات', 'Latest electronic gadgets and devices', 'أحدث الأدوات والأجهزة الإلكترونية', '/images/categories/electronics.jpg', 1, GETDATE()),
    ('Clothing', 'الملابس', 'Fashion and clothing items', 'أزياء وعناصر ملابس', '/images/categories/clothing.jpg', 1, GETDATE()),
    ('Home & Garden', 'المنزل والحديقة', 'Home improvement and garden supplies', 'أدوات تحسين المنزل ولوازم الحديقة', '/images/categories/home.jpg', 1, GETDATE()),
    ('Sports', 'الرياضة', 'Sports equipment and accessories', 'معدات رياضية وإكسسوارات', '/images/categories/sports.jpg', 1, GETDATE()),
    ('Books', 'الكتب', 'Books and educational materials', 'كتب ومواد تعليمية', '/images/categories/books.jpg', 1, GETDATE());
END

-- إدراج منتجات تجريبية
DECLARE @ElectronicsCategoryId INT, @ClothingCategoryId INT, @HomeCategoryId INT, @SportsCategoryId INT, @BooksCategoryId INT;

SELECT @ElectronicsCategoryId = Id FROM Categories WHERE Name = 'Electronics';
SELECT @ClothingCategoryId = Id FROM Categories WHERE Name = 'Clothing';
SELECT @HomeCategoryId = Id FROM Categories WHERE Name = 'Home & Garden';
SELECT @SportsCategoryId = Id FROM Categories WHERE Name = 'Sports';
SELECT @BooksCategoryId = Id FROM Categories WHERE Name = 'Books';

-- منتجات الإلكترونيات
IF NOT EXISTS (SELECT * FROM Products WHERE Name = 'iPhone 15 Pro')
BEGIN
    INSERT INTO Products (Name, NameAr, Description, DescriptionAr, Price, DiscountPrice, StockQuantity, ImageUrl, CategoryId, IsActive, CreatedAt, AverageRating) VALUES
    ('iPhone 15 Pro', 'آيفون 15 برو', 'Latest iPhone with advanced features', 'أحدث آيفون مع ميزات متقدمة', 999.99, 899.99, 50, '/images/products/phone1.jpg', @ElectronicsCategoryId, 1, GETDATE(), 4.5),

    ('Samsung Galaxy S24', 'سامسونج جالاكسي S24', 'Premium Android smartphone', 'هاتف أندرويد متميز', 899.99, NULL, 30, '/images/products/phone2.jpg', @ElectronicsCategoryId, 1, GETDATE(), 4.3),

    ('MacBook Air M3', 'ماك بوك إير M3', 'Lightweight laptop with M3 chip', 'لابتوب خفيف الوزن مع شريحة M3', 1199.99, 1099.99, 20, '/images/products/laptop1.jpg', @ElectronicsCategoryId, 1, GETDATE(), 4.7),

    ('Dell Gaming Monitor', 'شاشة ديل للألعاب', '27-inch 144Hz gaming monitor', 'شاشة ألعاب 27 إنش 144 هرتز', 399.99, NULL, 15, '/images/products/monitor1.jpg', @ElectronicsCategoryId, 1, GETDATE(), 4.2),

    ('Sony WH-1000XM5', 'سوني WH-1000XM5', 'Premium noise-cancelling headphones', 'سماعات عازلة للضوضاء متميزة', 349.99, 299.99, 40, '/images/products/headphones1.jpg', @ElectronicsCategoryId, 1, GETDATE(), 4.6);
END

-- منتجات الملابس
IF NOT EXISTS (SELECT * FROM Products WHERE Name = 'Nike Air Max 270')
BEGIN
    INSERT INTO Products (Name, NameAr, Description, DescriptionAr, Price, DiscountPrice, StockQuantity, ImageUrl, CategoryId, IsActive, CreatedAt, AverageRating) VALUES
    ('Nike Air Max 270', 'نايك إير ماكس 270', 'Comfortable running shoes', 'أحذية جري مريحة', 150.00, 129.99, 100, '/images/products/shoes1.jpg', @ClothingCategoryId, 1, GETDATE(), 4.4),

    ('Adidas Hoodie', 'هودي أديداس', 'Cotton blend hoodie for casual wear', 'هودي من مزيج القطن للارتداء الكاجوال', 79.99, NULL, 80, '/images/products/hoodie1.jpg', @ClothingCategoryId, 1, GETDATE(), 4.1),

    ('Levi''s Jeans', 'جينز ليفايز', 'Classic fit denim jeans', 'جينز دينم كلاسيك فيت', 89.99, 69.99, 60, '/images/products/jeans1.jpg', @ClothingCategoryId, 1, GETDATE(), 4.3);
END

-- منتجات المنزل والحديقة
IF NOT EXISTS (SELECT * FROM Products WHERE Name = 'Modern Desk Lamp')
BEGIN
    INSERT INTO Products (Name, NameAr, Description, DescriptionAr, Price, DiscountPrice, StockQuantity, ImageUrl, CategoryId, IsActive, CreatedAt, AverageRating) VALUES
    ('Modern Desk Lamp', 'مصباح مكتب عصري', 'LED desk lamp with wireless charging', 'مصباح مكتب LED مع شحن لاسلكي', 59.99, 49.99, 35, '/images/products/lamp1.jpg', @HomeCategoryId, 1, GETDATE(), 4.0),

    ('Ceramic Plant Pot', 'إصيص نباتات سيراميك', 'Handcrafted ceramic pot for indoor plants', 'إصيص سيراميك مصنوع يدوياً للنباتات الداخلية', 29.99, NULL, 45, '/images/products/pot1.jpg', @HomeCategoryId, 1, GETDATE(), 4.2);
END

-- منتجات رياضية
IF NOT EXISTS (SELECT * FROM Products WHERE Name = 'Yoga Mat Premium')
BEGIN
    INSERT INTO Products (Name, NameAr, Description, DescriptionAr, Price, DiscountPrice, StockQuantity, ImageUrl, CategoryId, IsActive, CreatedAt, AverageRating) VALUES
    ('Yoga Mat Premium', 'حصيرة يوغا متميزة', 'Non-slip yoga mat for all levels', 'حصيرة يوغا غير قابلة للانزلاق لجميع المستويات', 39.99, 34.99, 70, '/images/products/yogamat1.jpg', @SportsCategoryId, 1, GETDATE(), 4.4),

    ('Resistance Bands Set', 'مجموعة أشرطة مقاومة', '5-piece resistance bands for strength training', 'مجموعة 5 قطع أشرطة مقاومة للتدريب على القوة', 24.99, NULL, 55, '/images/products/bands1.jpg', @SportsCategoryId, 1, GETDATE(), 4.1);
END

-- منتجات الكتب
IF NOT EXISTS (SELECT * FROM Products WHERE Name = 'Programming Book')
BEGIN
    INSERT INTO Products (Name, NameAr, Description, DescriptionAr, Price, DiscountPrice, StockQuantity, ImageUrl, CategoryId, IsActive, CreatedAt, AverageRating) VALUES
    ('Programming Book', 'كتاب البرمجة', 'Learn C# programming from basics', 'تعلم برمجة C# من الأساسيات', 49.99, 39.99, 25, '/images/products/book1.jpg', @BooksCategoryId, 1, GETDATE(), 4.5),

    ('Business Strategy Guide', 'دليل استراتيجية الأعمال', 'Essential business strategy concepts', 'مفاهيم استراتيجية الأعمال الأساسية', 34.99, NULL, 30, '/images/products/book2.jpg', @BooksCategoryId, 1, GETDATE(), 4.0);
END

-- إدراج مستخدم تجريبي للاختبار
IF NOT EXISTS (SELECT * FROM AspNetUsers WHERE Email = 'test@example.com')
BEGIN
    INSERT INTO AspNetUsers (
        Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
        PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed,
        TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount,
        FirstName, LastName, CreatedAt
    ) VALUES (
        NEWID(), 'testuser', 'TESTUSER', 'test@example.com', 'TEST@EXAMPLE.COM', 1,
        'AQAAAAEAACcQAAAAEOfKsYaJKsKP8vO8KfKsYaJKsKP8vO8KfKsYaJKsKP8vO8K==', -- Password: Test123!
        NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0,
        'Test', 'User', GETDATE()
    );
END

-- إدراج تقييمات تجريبية
DECLARE @ProductId INT, @UserId UNIQUEIDENTIFIER;
SELECT TOP 1 @ProductId = Id FROM Products WHERE Name = 'iPhone 15 Pro';
SELECT TOP 1 @UserId = Id FROM AspNetUsers WHERE Email = 'test@example.com';

IF NOT EXISTS (SELECT * FROM Reviews WHERE ProductId = @ProductId AND UserId = @UserId)
BEGIN
    INSERT INTO Reviews (ProductId, UserId, Rating, Comment, CreatedAt, IsApproved) VALUES
    (@ProductId, @UserId, 5, 'Excellent product! Highly recommended.', GETDATE(), 1);
END

PRINT 'تم إدراج البيانات التجريبية بنجاح!';
GO

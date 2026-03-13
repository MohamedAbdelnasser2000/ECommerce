-- سكريبت قاعدة البيانات المبسط للنشر السريع
-- انسخ والصق هذا في أداة إدارة قاعدة البيانات في لوحة الاستضافة

USE db28996;
GO

-- إنشاء فئات أساسية إذا لم تكن موجودة
IF NOT EXISTS (SELECT * FROM Categories WHERE Name = 'Electronics')
BEGIN
    INSERT INTO Categories (Name, NameAr, IsActive, CreatedAt) VALUES
    ('Electronics', 'الإلكترونيات', 1, GETDATE()),
    ('Clothing', 'الملابس', 1, GETDATE()),
    ('Home', 'المنزل', 1, GETDATE());
END

-- إضافة منتج تجريبي واحد على الأقل
DECLARE @CategoryId INT;
SELECT @CategoryId = Id FROM Categories WHERE Name = 'Electronics';

IF NOT EXISTS (SELECT * FROM Products WHERE Name = 'iPhone 15 Pro')
BEGIN
    INSERT INTO Products (Name, NameAr, Description, Price, StockQuantity, ImageUrl, CategoryId, IsActive, CreatedAt)
    VALUES ('iPhone 15 Pro', 'آيفون 15 برو', 'أحدث هواتف آيفون', 999.99, 10, '/images/products/phone1.jpg', @CategoryId, 1, GETDATE());
END

-- إضافة منتج آخر إذا أردت المزيد من الاختبار
IF NOT EXISTS (SELECT * FROM Products WHERE Name = 'Samsung Galaxy')
BEGIN
    INSERT INTO Products (Name, NameAr, Description, Price, StockQuantity, ImageUrl, CategoryId, IsActive, CreatedAt)
    VALUES ('Samsung Galaxy', 'سامسونج جالاكسي', 'هاتف سامسونج متميز', 899.99, 5, '/images/products/phone1.jpg', @CategoryId, 1, GETDATE());
END

SELECT 'تم إضافة البيانات بنجاح!' as Status;
GO

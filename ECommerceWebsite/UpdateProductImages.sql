-- تحديث صور المنتجات الموجودة
-- هذا الملف يحدث مسارات الصور للمنتجات الموجودة في قاعدة البيانات

-- تحديث المنتجات بصور تجريبية
UPDATE Products SET ImageUrl = '/images/products/phone1.jpg' WHERE Name LIKE '%هاتف%' OR Name LIKE '%phone%';
UPDATE Products SET ImageUrl = '/images/products/headphones1.jpg' WHERE Name LIKE '%سماعات%' OR Name LIKE '%headphone%';
UPDATE Products SET ImageUrl = '/images/products/bag1.jpg' WHERE Name LIKE '%حقيبة%' OR Name LIKE '%bag%';
UPDATE Products SET ImageUrl = '/images/products/watch1.jpg' WHERE Name LIKE '%ساعة%' OR Name LIKE '%watch%';
UPDATE Products SET ImageUrl = '/images/products/book1.jpg' WHERE Name LIKE '%كتاب%' OR Name LIKE '%book%';
UPDATE Products SET ImageUrl = '/images/products/tablet1.jpg' WHERE Name LIKE '%جهاز%' OR Name LIKE '%tablet%';

-- تحديث المنتجات المتبقية بصور عشوائية
UPDATE Products SET ImageUrl = '/images/products/phone1.jpg' WHERE ImageUrl IS NULL OR ImageUrl = '';

-- إضافة بعض المنتجات التجريبية إذا لم تكن موجودة
INSERT INTO Products (Name, Description, Price, StockQuantity, CategoryId, ImageUrl, IsActive, CreatedAt, UpdatedAt)
SELECT 'هاتف ذكي متطور', 'هاتف ذكي بمواصفات عالية وتقنيات حديثة', 599.00, 50, 1, '/images/products/phone1.jpg', 1, GETDATE(), GETDATE()
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'هاتف ذكي متطور');

INSERT INTO Products (Name, Description, Price, StockQuantity, CategoryId, ImageUrl, IsActive, CreatedAt, UpdatedAt, DiscountPrice)
SELECT 'سماعات لاسلكية', 'سماعات عالية الجودة مع تقنية إلغاء الضوضاء', 249.00, 30, 1, '/images/products/headphones1.jpg', 1, GETDATE(), GETDATE(), 199.00
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'سماعات لاسلكية');

INSERT INTO Products (Name, Description, Price, StockQuantity, CategoryId, ImageUrl, IsActive, CreatedAt, UpdatedAt)
SELECT 'حقيبة يد أنيقة', 'حقيبة يد عصرية مصنوعة من الجلد الطبيعي', 89.00, 0, 2, '/images/products/bag1.jpg', 1, GETDATE(), GETDATE()
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'حقيبة يد أنيقة');

INSERT INTO Products (Name, Description, Price, StockQuantity, CategoryId, ImageUrl, IsActive, CreatedAt, UpdatedAt, DiscountPrice)
SELECT 'ساعة رياضية ذكية', 'ساعة ذكية لتتبع اللياقة البدنية والصحة', 399.00, 25, 1, '/images/products/watch1.jpg', 1, GETDATE(), GETDATE(), 299.00
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'ساعة رياضية ذكية');

INSERT INTO Products (Name, Description, Price, StockQuantity, CategoryId, ImageUrl, IsActive, CreatedAt, UpdatedAt)
SELECT 'كتاب تطوير الذات', 'كتاب ملهم لتطوير المهارات الشخصية والمهنية', 25.00, 100, 3, '/images/products/book1.jpg', 1, GETDATE(), GETDATE()
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'كتاب تطوير الذات');

INSERT INTO Products (Name, Description, Price, StockQuantity, CategoryId, ImageUrl, IsActive, CreatedAt, UpdatedAt, DiscountPrice)
SELECT 'جهاز لوحي للرسم', 'جهاز لوحي متطور للرسم الرقمي والتصميم', 549.00, 15, 1, '/images/products/tablet1.jpg', 1, GETDATE(), GETDATE(), 449.00
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'جهاز لوحي للرسم');

-- التأكد من وجود الفئات
INSERT INTO Categories (Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT 'Electronics', 'الأجهزة الإلكترونية والتقنية', 1, GETDATE(), GETDATE()
WHERE NOT EXISTS (SELECT 1 FROM Categories WHERE Name = 'Electronics');

INSERT INTO Categories (Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT 'Fashion', 'الأزياء والإكسسوارات', 1, GETDATE(), GETDATE()
WHERE NOT EXISTS (SELECT 1 FROM Categories WHERE Name = 'Fashion');

INSERT INTO Categories (Name, Description, IsActive, CreatedAt, UpdatedAt)
SELECT 'Books', 'الكتب والمراجع', 1, GETDATE(), GETDATE()
WHERE NOT EXISTS (SELECT 1 FROM Categories WHERE Name = 'Books');

-- تحديث CategoryId للمنتجات
UPDATE Products SET CategoryId = (SELECT TOP 1 Id FROM Categories WHERE Name = 'Electronics') 
WHERE Name IN ('هاتف ذكي متطور', 'سماعات لاسلكية', 'ساعة رياضية ذكية', 'جهاز لوحي للرسم');

UPDATE Products SET CategoryId = (SELECT TOP 1 Id FROM Categories WHERE Name = 'Fashion') 
WHERE Name IN ('حقيبة يد أنيقة');

UPDATE Products SET CategoryId = (SELECT TOP 1 Id FROM Categories WHERE Name = 'Books') 
WHERE Name IN ('كتاب تطوير الذات');

-- إنشاء قاعدة بيانات SQL Server
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'ECommerceDB')
BEGIN
    CREATE DATABASE ECommerceDB;
END
GO

# ECommerce

This repository contains a .NET 9 Razor Pages e-commerce website (ECommerceWebsite project).

Quick start:

1. Configure your SQL Server connection string in ECommerceWebsite/appsettings.json (DefaultConnection).
2. Restore packages: `dotnet restore`
3. Apply migrations and run the app: `dotnet run --project ECommerceWebsite`

Notes:
- This project targets .NET 9 and uses Razor Pages.
- Add any secrets (like SMTP credentials) to user secrets or `appsettings.Development.json` (excluded from repo).

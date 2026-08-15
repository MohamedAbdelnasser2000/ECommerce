# ECommerce — .NET 9 Razor Pages Platform

A database-driven e-commerce application built with **ASP.NET Core Razor Pages on .NET 9**. The project demonstrates product and order workflows, authentication, validation, SQL Server persistence, caching, and production-oriented observability.

## Technology Stack

- C# and ASP.NET Core Razor Pages
- .NET 9
- Entity Framework Core and SQL Server
- ASP.NET Core Identity
- Redis distributed caching
- Application Insights
- jQuery and unobtrusive validation

## Project Structure

The main application is located in `ECommerceWebsite/`. The repository also contains the solution file, front-end assets, and supporting documentation.

## Getting Started

### Prerequisites

- .NET 9 SDK
- SQL Server or a compatible SQL Server instance
- Redis, if distributed caching is enabled in the selected configuration

### Run locally

```bash
git clone https://github.com/MohamedAbdelnasser2000/ECommerce.git
cd ECommerce
dotnet restore
dotnet ef database update --project ECommerceWebsite
dotnet run --project ECommerceWebsite
```

Open the URL printed by the application in the terminal.

## Configuration and Security

Set the database connection string through User Secrets or environment variables. Do not commit passwords, API keys, SMTP credentials, browser cookies, or production connection strings. Development-only configuration should remain local and should not be used as a source of real credentials.

Before publishing this repository, review `cookies.txt` and remove it if it contains session data or any information copied from a browser.

## Engineering Highlights

The project is designed to demonstrate a complete .NET web application workflow, including server-side rendering with Razor Pages, persistence with Entity Framework Core, authentication with Identity, validation, and caching integration.

## Contact

- LinkedIn: [Mohamed Abdelnasser](https://www.linkedin.com/in/mohamed-abdelnasser-krsoon-8b448134b)
- Email: [mnkq2000@gmail.com](mailto:mnkq2000@gmail.com)

## License

No license has been declared yet. Add an explicit license before accepting external contributions or presenting the project as reusable open-source software.

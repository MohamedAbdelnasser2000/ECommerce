using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ECommerceWebsite.Migrations
{
    /// <inheritdoc />
    public partial class AddContactMessagesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_TaxSettings_TaxSettingId",
                table: "Orders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TaxSettings",
                table: "TaxSettings");

            migrationBuilder.RenameTable(
                name: "TaxSettings",
                newName: "TaxSetting");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TaxSetting",
                table: "TaxSetting",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "ContactMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Consent = table.Column<bool>(type: "bit", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactMessages", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ShippingMethods",
                columns: new[] { "Id", "Cost", "CreatedAt", "Description", "EstimatedDays", "IsActive", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, 5.99m, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Standard delivery (3-5 business days)", 5, true, "Standard Shipping", new DateTime(2025, 10, 6, 0, 1, 29, 98, DateTimeKind.Local).AddTicks(9597) },
                    { 2, 12.99m, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Express delivery (1-2 business days)", 2, true, "Express Shipping", new DateTime(2025, 10, 6, 0, 1, 29, 99, DateTimeKind.Local).AddTicks(1786) },
                    { 3, 24.99m, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Next business day delivery", 1, true, "Overnight Shipping", new DateTime(2025, 10, 6, 0, 1, 29, 99, DateTimeKind.Local).AddTicks(1799) },
                    { 4, 0.00m, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Free shipping on orders over $50", 7, true, "Free Shipping", new DateTime(2025, 10, 6, 0, 1, 29, 99, DateTimeKind.Local).AddTicks(1815) }
                });

            migrationBuilder.InsertData(
                table: "TaxSetting",
                columns: new[] { "Id", "City", "Country", "CreatedAt", "IsActive", "Notes", "Region", "TaxRate", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "Cairo", "Egypt", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Standard VAT rate for Cairo", "Cairo", 14.00m, new DateTime(2025, 10, 6, 0, 1, 29, 99, DateTimeKind.Local).AddTicks(3354) },
                    { 2, "Alexandria", "Egypt", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Standard VAT rate for Alexandria", "Alexandria", 14.00m, new DateTime(2025, 10, 6, 0, 1, 29, 99, DateTimeKind.Local).AddTicks(6221) },
                    { 3, "Riyadh", "Saudi Arabia", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Standard VAT rate for Saudi Arabia", "Riyadh", 15.00m, new DateTime(2025, 10, 6, 0, 1, 29, 99, DateTimeKind.Local).AddTicks(6229) },
                    { 4, "Dubai", "UAE", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Standard VAT rate for UAE", "Dubai", 5.00m, new DateTime(2025, 10, 6, 0, 1, 29, 99, DateTimeKind.Local).AddTicks(6238) }
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_TaxSetting_TaxSettingId",
                table: "Orders",
                column: "TaxSettingId",
                principalTable: "TaxSetting",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_TaxSetting_TaxSettingId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "ContactMessages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TaxSetting",
                table: "TaxSetting");

            migrationBuilder.DeleteData(
                table: "ShippingMethods",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ShippingMethods",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "ShippingMethods",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "ShippingMethods",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "TaxSetting",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "TaxSetting",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "TaxSetting",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "TaxSetting",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.RenameTable(
                name: "TaxSetting",
                newName: "TaxSettings");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TaxSettings",
                table: "TaxSettings",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_TaxSettings_TaxSettingId",
                table: "Orders",
                column: "TaxSettingId",
                principalTable: "TaxSettings",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}

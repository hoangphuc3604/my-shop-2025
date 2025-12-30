using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MyShop.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderStatusAndSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.InsertData(
                table: "Orders",
                columns: new[] { "OrderId", "CreatedTime", "FinalPrice", "Status" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 12, 19, 16, 41, 46, 160, DateTimeKind.Utc).AddTicks(441), 580000, "Created" },
                    { 2, new DateTime(2025, 12, 20, 16, 41, 46, 160, DateTimeKind.Utc).AddTicks(459), 1250000, "Paid" },
                    { 3, new DateTime(2025, 12, 21, 16, 41, 46, 160, DateTimeKind.Utc).AddTicks(463), 340000, "Cancelled" },
                    { 4, new DateTime(2025, 12, 22, 16, 41, 46, 160, DateTimeKind.Utc).AddTicks(465), 850000, "Paid" },
                    { 5, new DateTime(2025, 12, 23, 16, 41, 46, 160, DateTimeKind.Utc).AddTicks(466), 1700000, "Created" },
                    { 6, new DateTime(2025, 12, 24, 16, 41, 46, 160, DateTimeKind.Utc).AddTicks(469), 475000, "Paid" },
                    { 7, new DateTime(2025, 12, 25, 16, 41, 46, 160, DateTimeKind.Utc).AddTicks(471), 1120000, "Cancelled" },
                    { 8, new DateTime(2025, 12, 26, 16, 41, 46, 160, DateTimeKind.Utc).AddTicks(472), 2150000, "Paid" },
                    { 9, new DateTime(2025, 12, 27, 16, 41, 46, 160, DateTimeKind.Utc).AddTicks(474), 610000, "Created" },
                    { 10, new DateTime(2025, 12, 28, 16, 41, 46, 160, DateTimeKind.Utc).AddTicks(506), 1390000, "Paid" }
                });

            migrationBuilder.InsertData(
                table: "OrderItems",
                columns: new[] { "OrderItemId", "OrderId", "ProductId", "Quantity", "TotalPrice", "UnitSalePrice" },
                values: new object[,]
                {
                    { 1, 1, 2, 1, 70000, 70000f },
                    { 2, 1, 3, 2, 180000, 90000f },
                    { 3, 1, 4, 3, 330000, 110000f },
                    { 4, 2, 3, 1, 85000, 85000f },
                    { 5, 2, 4, 2, 210000, 105000f },
                    { 6, 2, 5, 3, 375000, 125000f },
                    { 7, 2, 6, 4, 580000, 145000f },
                    { 8, 3, 4, 1, 100000, 100000f },
                    { 9, 3, 5, 2, 240000, 120000f },
                    { 10, 4, 5, 1, 115000, 115000f },
                    { 11, 4, 6, 2, 270000, 135000f },
                    { 12, 4, 7, 3, 465000, 155000f },
                    { 13, 5, 6, 1, 130000, 130000f },
                    { 14, 5, 7, 2, 300000, 150000f },
                    { 15, 5, 8, 3, 510000, 170000f },
                    { 16, 5, 9, 4, 760000, 190000f },
                    { 17, 6, 7, 1, 145000, 145000f },
                    { 18, 6, 8, 2, 330000, 165000f },
                    { 19, 7, 8, 1, 160000, 160000f },
                    { 20, 7, 9, 2, 360000, 180000f },
                    { 21, 7, 10, 3, 600000, 200000f },
                    { 22, 8, 9, 1, 175000, 175000f },
                    { 23, 8, 10, 2, 390000, 195000f },
                    { 24, 8, 11, 3, 645000, 215000f },
                    { 25, 8, 12, 4, 940000, 235000f },
                    { 26, 9, 10, 1, 190000, 190000f },
                    { 27, 9, 11, 2, 420000, 210000f },
                    { 28, 10, 11, 1, 205000, 205000f },
                    { 29, 10, 12, 2, 450000, 225000f },
                    { 30, 10, 13, 3, 735000, 245000f }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "OrderItems",
                keyColumn: "OrderItemId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "OrderItems",
                keyColumn: "OrderItemId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "OrderItems",
                keyColumn: "OrderItemId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "OrderItems",
                keyColumn: "OrderItemId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "OrderItems",
                keyColumn: "OrderItemId",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "OrderItems",
                keyColumn: "OrderItemId",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "OrderItems",
                keyColumn: "OrderItemId",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "OrderItems",
                keyColumn: "OrderItemId",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "OrderItems",
                keyColumn: "OrderItemId",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "OrderItems",
                keyColumn: "OrderItemId",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "OrderItems",
                keyColumn: "OrderItemId",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "OrderItems",
                keyColumn: "OrderItemId",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "OrderItems",
                keyColumn: "OrderItemId",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "OrderItems",
                keyColumn: "OrderItemId",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "OrderItems",
                keyColumn: "OrderItemId",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "OrderItems",
                keyColumn: "OrderItemId",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "OrderItems",
                keyColumn: "OrderItemId",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "OrderItems",
                keyColumn: "OrderItemId",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "OrderItems",
                keyColumn: "OrderItemId",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "OrderItems",
                keyColumn: "OrderItemId",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "OrderItems",
                keyColumn: "OrderItemId",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "OrderItems",
                keyColumn: "OrderItemId",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "OrderItems",
                keyColumn: "OrderItemId",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "OrderItems",
                keyColumn: "OrderItemId",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "OrderItems",
                keyColumn: "OrderItemId",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "OrderItems",
                keyColumn: "OrderItemId",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "OrderItems",
                keyColumn: "OrderItemId",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "OrderItems",
                keyColumn: "OrderItemId",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "OrderItems",
                keyColumn: "OrderItemId",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "OrderItems",
                keyColumn: "OrderItemId",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "Orders",
                keyColumn: "OrderId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Orders",
                keyColumn: "OrderId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Orders",
                keyColumn: "OrderId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Orders",
                keyColumn: "OrderId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Orders",
                keyColumn: "OrderId",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Orders",
                keyColumn: "OrderId",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Orders",
                keyColumn: "OrderId",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Orders",
                keyColumn: "OrderId",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Orders",
                keyColumn: "OrderId",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Orders",
                keyColumn: "OrderId",
                keyValue: 10);

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Orders");
        }
    }
}

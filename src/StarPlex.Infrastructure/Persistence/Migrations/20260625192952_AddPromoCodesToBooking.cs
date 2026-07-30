using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarPlex.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPromoCodesToBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UsageCount",
                table: "Discounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UsageLimit",
                table: "Discounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "DiscountId",
                table: "Bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_DiscountId",
                table: "Bookings",
                column: "DiscountId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Discounts_DiscountId",
                table: "Bookings",
                column: "DiscountId",
                principalTable: "Discounts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Discounts_DiscountId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_DiscountId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "UsageCount",
                table: "Discounts");

            migrationBuilder.DropColumn(
                name: "UsageLimit",
                table: "Discounts");

            migrationBuilder.DropColumn(
                name: "DiscountId",
                table: "Bookings");
        }
    }
}

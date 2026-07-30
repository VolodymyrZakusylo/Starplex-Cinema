using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarPlex.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixMovieDeletionCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Sessions_SessionId",
                table: "Bookings");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Sessions_SessionId",
                table: "Bookings",
                column: "SessionId",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Sessions_SessionId",
                table: "Bookings");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Sessions_SessionId",
                table: "Bookings",
                column: "SessionId",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

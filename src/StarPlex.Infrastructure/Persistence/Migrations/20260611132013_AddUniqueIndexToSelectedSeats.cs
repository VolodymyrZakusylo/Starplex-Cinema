using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarPlex.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexToSelectedSeats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SelectedSeats_SessionId_SeatId",
                table: "SelectedSeats");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Halls",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.CreateIndex(
                name: "IX_SelectedSeats_SessionId_SeatId",
                table: "SelectedSeats",
                columns: new[] { "SessionId", "SeatId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Halls_Name_CinemaId",
                table: "Halls",
                columns: new[] { "Name", "CinemaId" },
                unique: true)
                .Annotation("Npgsql:IndexMethod", "btree")
                .Annotation("Npgsql:IndexOperators", new[] { "text_ops", "uuid_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SelectedSeats_SessionId_SeatId",
                table: "SelectedSeats");

            migrationBuilder.DropIndex(
                name: "IX_Halls_Name_CinemaId",
                table: "Halls");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Halls",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_SelectedSeats_SessionId_SeatId",
                table: "SelectedSeats",
                columns: new[] { "SessionId", "SeatId" });
        }
    }
}

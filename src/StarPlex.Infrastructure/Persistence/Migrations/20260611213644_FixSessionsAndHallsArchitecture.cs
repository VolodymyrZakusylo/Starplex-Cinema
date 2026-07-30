using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarPlex.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixSessionsAndHallsArchitecture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sessions_HallId",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Halls_CinemaId",
                table: "Halls");

            migrationBuilder.DropIndex(
                name: "IX_Halls_Name_CinemaId",
                table: "Halls");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_HallId_StartTime",
                table: "Sessions",
                columns: new[] { "HallId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_Status_StartTime",
                table: "Sessions",
                columns: new[] { "Status", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Halls_CinemaId_Name",
                table: "Halls",
                columns: new[] { "CinemaId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sessions_HallId_StartTime",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_Status_StartTime",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Halls_CinemaId_Name",
                table: "Halls");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_HallId",
                table: "Sessions",
                column: "HallId");

            migrationBuilder.CreateIndex(
                name: "IX_Halls_CinemaId",
                table: "Halls",
                column: "CinemaId");

            migrationBuilder.CreateIndex(
                name: "IX_Halls_Name_CinemaId",
                table: "Halls",
                columns: new[] { "Name", "CinemaId" },
                unique: true)
                .Annotation("Npgsql:IndexMethod", "btree")
                .Annotation("Npgsql:IndexOperators", new[] { "text_ops", "uuid_ops" });
        }
    }
}

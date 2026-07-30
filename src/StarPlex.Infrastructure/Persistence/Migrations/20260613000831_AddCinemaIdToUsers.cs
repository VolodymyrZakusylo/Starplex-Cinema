using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarPlex.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCinemaIdToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CinemaId",
                table: "Users",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CinemaId",
                table: "Users");
        }
    }
}

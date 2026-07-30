using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarPlex.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMovieSlugAndBackdrop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BackdropUrl",
                table: "Movies",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Movies",
                type: "character varying(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BackdropUrl",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Movies");
        }
    }
}

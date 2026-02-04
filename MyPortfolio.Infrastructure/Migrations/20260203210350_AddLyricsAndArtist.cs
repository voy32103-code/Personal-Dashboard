using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyPortfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLyricsAndArtist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Artist",
                table: "PortfolioItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Lyrics",
                table: "PortfolioItems",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Artist",
                table: "PortfolioItems");

            migrationBuilder.DropColumn(
                name: "Lyrics",
                table: "PortfolioItems");
        }
    }
}

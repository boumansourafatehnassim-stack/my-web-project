using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bibliotheque.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLangueAndAdresseBibliogrToLivres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdresseBibliogr",
                table: "Livres",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Langue",
                table: "Livres",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdresseBibliogr",
                table: "Livres");

            migrationBuilder.DropColumn(
                name: "Langue",
                table: "Livres");
        }
    }
}

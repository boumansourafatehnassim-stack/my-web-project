using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bibliotheque.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddStatutToEmprunt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Statut",
                table: "Emprunts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Statut",
                table: "Emprunts");
        }
    }
}

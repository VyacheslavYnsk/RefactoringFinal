using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Refactoring.Migrations
{
    /// <inheritdoc />
    public partial class RecreateHallsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Rows",
                table: "Halls",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rows",
                table: "Halls");
        }
    }
}

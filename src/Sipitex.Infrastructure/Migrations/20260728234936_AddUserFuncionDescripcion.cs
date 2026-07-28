using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sipitex.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserFuncionDescripcion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FuncionDescripcion",
                table: "Users",
                type: "TEXT",
                maxLength: 800,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FuncionDescripcion",
                table: "Users");
        }
    }
}

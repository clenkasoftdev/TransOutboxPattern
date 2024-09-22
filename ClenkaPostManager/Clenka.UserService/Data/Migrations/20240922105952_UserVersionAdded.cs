using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clenka.UserService.Data.MIgrations
{
    public partial class UserVersionAdded : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "Users");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

namespace PasswordsAPI.Migrations
{
    public partial class AddedInfos : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Infos",
                table: "UserLocations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Infos",
                table: "UserLocations");
        }
    }
}

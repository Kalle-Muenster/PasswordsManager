using Microsoft.EntityFrameworkCore.Migrations;

namespace PasswordsAPI.Migrations
{
    public partial class ChangePasswordName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Password",
                table: "PasswordUsers",
                newName: "Pass");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Pass",
                table: "PasswordUsers",
                newName: "Password");
        }
    }
}

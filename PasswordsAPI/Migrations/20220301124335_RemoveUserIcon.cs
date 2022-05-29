using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Passwords.API.Migrations
{
    public partial class RemoveUserIcon : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Icon",
                table: "PasswordUsers");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "Icon",
                table: "PasswordUsers",
                type: "varbinary(max)",
                nullable: true);
        }
    }
}

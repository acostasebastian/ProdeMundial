using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProdeMundial.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ModificarTablaUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccessPin",
                table: "AppUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "AppUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAdmin",
                table: "AppUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "AppUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessPin",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "IsAdmin",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "AppUsers");
        }
    }
}

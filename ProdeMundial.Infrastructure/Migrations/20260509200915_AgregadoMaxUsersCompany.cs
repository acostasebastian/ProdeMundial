using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProdeMundial.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregadoMaxUsersCompany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxUsers",
                table: "Companies",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxUsers",
                table: "Companies");
        }
    }
}

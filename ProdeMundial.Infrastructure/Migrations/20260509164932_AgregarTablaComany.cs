using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProdeMundial.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTablaComany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Predictions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InvitationCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrizeDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_CompanyId",
                table: "Predictions",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Predictions_Companies_CompanyId",
                table: "Predictions",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Predictions_Companies_CompanyId",
                table: "Predictions");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Predictions_CompanyId",
                table: "Predictions");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Predictions");
        }
    }
}

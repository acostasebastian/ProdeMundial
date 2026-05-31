using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProdeMundial.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregadoIsActiveAppUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Predictions_AppUsers_UserId",
                table: "Predictions");

            migrationBuilder.DropForeignKey(
                name: "FK_Predictions_Companies_CompanyId",
                table: "Predictions");

            migrationBuilder.AddColumn<int>(
                name: "CompanyId1",
                table: "Predictions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId1",
                table: "Predictions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "AppUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId1",
                table: "AppUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "AppUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_CompanyId1",
                table: "Predictions",
                column: "CompanyId1");

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_UserId1",
                table: "Predictions",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_CompanyId",
                table: "AppUsers",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_CompanyId1",
                table: "AppUsers",
                column: "CompanyId1");

            migrationBuilder.AddForeignKey(
                name: "FK_AppUsers_Companies_CompanyId",
                table: "AppUsers",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AppUsers_Companies_CompanyId1",
                table: "AppUsers",
                column: "CompanyId1",
                principalTable: "Companies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Predictions_AppUsers_UserId",
                table: "Predictions",
                column: "UserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Predictions_AppUsers_UserId1",
                table: "Predictions",
                column: "UserId1",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Predictions_Companies_CompanyId",
                table: "Predictions",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Predictions_Companies_CompanyId1",
                table: "Predictions",
                column: "CompanyId1",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppUsers_Companies_CompanyId",
                table: "AppUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AppUsers_Companies_CompanyId1",
                table: "AppUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Predictions_AppUsers_UserId",
                table: "Predictions");

            migrationBuilder.DropForeignKey(
                name: "FK_Predictions_AppUsers_UserId1",
                table: "Predictions");

            migrationBuilder.DropForeignKey(
                name: "FK_Predictions_Companies_CompanyId",
                table: "Predictions");

            migrationBuilder.DropForeignKey(
                name: "FK_Predictions_Companies_CompanyId1",
                table: "Predictions");

            migrationBuilder.DropIndex(
                name: "IX_Predictions_CompanyId1",
                table: "Predictions");

            migrationBuilder.DropIndex(
                name: "IX_Predictions_UserId1",
                table: "Predictions");

            migrationBuilder.DropIndex(
                name: "IX_AppUsers_CompanyId",
                table: "AppUsers");

            migrationBuilder.DropIndex(
                name: "IX_AppUsers_CompanyId1",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "CompanyId1",
                table: "Predictions");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "Predictions");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "CompanyId1",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "AppUsers");

            migrationBuilder.AddForeignKey(
                name: "FK_Predictions_AppUsers_UserId",
                table: "Predictions",
                column: "UserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Predictions_Companies_CompanyId",
                table: "Predictions",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

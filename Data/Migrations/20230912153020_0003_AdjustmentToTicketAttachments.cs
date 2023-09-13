using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chrysalis.Data.Migrations
{
    /// <inheritdoc />
    public partial class _0003_AdjustmentToTicketAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketAttachments_AspNetUsers_BTUserId",
                table: "TicketAttachments");

            migrationBuilder.RenameColumn(
                name: "BTUserId",
                table: "TicketAttachments",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_TicketAttachments_BTUserId",
                table: "TicketAttachments",
                newName: "IX_TicketAttachments_UserId");

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "TicketAttachments",
                type: "text",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TicketAttachments_AspNetUsers_UserId",
                table: "TicketAttachments",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketAttachments_AspNetUsers_UserId",
                table: "TicketAttachments");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "TicketAttachments");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "TicketAttachments",
                newName: "BTUserId");

            migrationBuilder.RenameIndex(
                name: "IX_TicketAttachments_UserId",
                table: "TicketAttachments",
                newName: "IX_TicketAttachments_BTUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketAttachments_AspNetUsers_BTUserId",
                table: "TicketAttachments",
                column: "BTUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
